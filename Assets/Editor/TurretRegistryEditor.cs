using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Underdark
{
    /// <summary>
    /// TurretRegistry 커스텀 인스펙터 + 마이그레이션 툴
    /// - 기존 TurretManager의 prefab 슬롯 데이터를 Registry로 자동 마이그레이션
    /// - Registry 항목을 보기 좋게 표시
    /// </summary>
    [CustomEditor(typeof(TurretRegistry))]
    public class TurretRegistryEditor : Editor
    {
        private bool _showMigration = false;

        public override void OnInspectorGUI()
        {
            var reg = (TurretRegistry)target;

            // ── 헤더 ──────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "터렛 추가 방법:\n" +
                "1. GameEnums.cs 의 TurretType enum에 항목 추가\n" +
                "2. 아래 Entries 리스트에 항목 추가 (+)\n" +
                "3. Type 선택, Prefab 드래그, 수치 입력\n" +
                "4. 끝! 코드 수정 불필요.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            // ── 기본 인스펙터 ─────────────────────────────────────────
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            // ── 마이그레이션 섹션 ─────────────────────────────────────
            _showMigration = EditorGUILayout.Foldout(_showMigration, "기존 TurretManager에서 마이그레이션", true);
            if (_showMigration)
            {
                EditorGUILayout.HelpBox(
                    "씬에 있는 TurretManager의 prefab 슬롯 데이터를 이 Registry로 자동으로 옮깁니다.\n" +
                    "이미 Registry에 있는 타입은 덮어쓰지 않습니다.",
                    MessageType.Warning);

                if (GUILayout.Button("▶  씬에서 TurretManager 찾아 자동 마이그레이션", GUILayout.Height(32)))
                    MigrateFromScene(reg);

                if (GUILayout.Button("▶  Resources/Prefabs 폴더에서 프리팹 자동 탐색 & 등록", GUILayout.Height(32)))
                    AutoDetectPrefabs(reg);
            }

            EditorGUILayout.Space(4);

            // ── 검증 버튼 ─────────────────────────────────────────────
            if (GUILayout.Button("누락된 Prefab 확인", GUILayout.Height(26)))
                ValidatePrefabs(reg);

            EditorGUILayout.Space(2);
            if (GUILayout.Button("캐시 재빌드", GUILayout.Height(22)))
            {
                reg.RebuildCache();
                Debug.Log("[TurretRegistry] 캐시 재빌드 완료");
            }
        }

        // ── 씬에서 TurretManager를 찾아 마이그레이션 ─────────────────

        private void MigrateFromScene(TurretRegistry reg)
        {
            var mgr = FindObjectOfType<TurretManager>();
            if (mgr == null)
            {
                EditorUtility.DisplayDialog("마이그레이션 실패",
                    "씬에서 TurretManager를 찾을 수 없습니다.\n인게임 씬을 열고 다시 시도하세요.", "확인");
                return;
            }

            // 리플렉션으로 구 버전 필드 읽기
            var type = mgr.GetType();
            var added = 0;

            // (구 코드의 prefab 필드명 → TurretType) 매핑
            var fieldMap = new Dictionary<string, TurretType>
            {
                { "rangedTurretPrefab",    TurretType.RangedTurret    },
                { "meleeTurretPrefab",     TurretType.MeleeTurret     },
                { "CrossTurretPrefab",     TurretType.CrossMeleeTurret},
                { "spikeTrapPrefab",       TurretType.SpikeTrap       },
                { "electricGatePrefab",    TurretType.ElectricGate    },
                { "wallPrefab",            TurretType.Wall            },
                { "wall2x1Prefab",         TurretType.Wall2x1         },
                { "wall1x2Prefab",         TurretType.Wall1x2         },
                { "wall2x2Prefab",         TurretType.Wall2x2         },
                { "areaDamagePrefab",      TurretType.AreaDamage      },
                { "explosiveCannonPrefab", TurretType.ExplosiveCannon },
                { "slowShooterPrefab",     TurretType.SlowShooter     },
                { "rapidFirePrefab",       TurretType.RapidFire       },
                { "tornadoPrefab",         TurretType.Tornado         },
                { "lavaRainPrefab",        TurretType.LavaRain        },
                { "chainLightningPrefab",  TurretType.ChainLightning  },
                { "blackHolePrefab",       TurretType.BlackHole       },
                { "precisionStrikePrefab", TurretType.PrecisionStrike },
                { "gambleBatPrefab",       TurretType.GambleBat       },
                { "pulseSlowerPrefab",     TurretType.PulseSlower     },
                { "dragonStatuePrefab",    TurretType.DragonStatue    },
                { "hasteTowerPrefab",      TurretType.HasteTower      },
                { "pinballCannonPrefab",   TurretType.PinballCannon   },
                { "boomerangTurretPrefab", TurretType.BoomerangTurret },
            };

            // 기존 TurretDef 기본값
            var defaults = GetDefaultDefs();

            Undo.RecordObject(reg, "Migrate TurretRegistry");

            foreach (var kv in fieldMap)
            {
                var field = type.GetField(kv.Key,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field == null) continue;

                var prefab = field.GetValue(mgr) as GameObject;
                if (prefab == null) continue;

                // 이미 등록돼 있으면 스킵
                if (reg.Get(kv.Value) != null)
                {
                    Debug.Log($"[Migration] {kv.Value} 이미 등록됨 — 스킵");
                    continue;
                }

                defaults.TryGetValue(kv.Value, out var def);
                var entry = def ?? new TurretRegistry.TurretEntry
                {
                    type  = kv.Value,
                    sizeX = 1, sizeY = 1, cost = 10,
                    color = Color.white, label = kv.Value.ToString(), emoji = "🔫"
                };
                entry.prefab = prefab;

                reg.entries.Add(entry);
                added++;
                Debug.Log($"[Migration] {kv.Value} → {prefab.name} 추가됨");
            }

            EditorUtility.SetDirty(reg);
            reg.RebuildCache();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("마이그레이션 완료",
                $"{added}개 터렛이 Registry에 추가되었습니다!\n\n이제 TurretManager의 Inspector에서\n'Registry' 슬롯에 이 asset을 연결하면 됩니다.", "확인");
        }

        // ── 프리팹 폴더 자동 탐색 ────────────────────────────────────

        private void AutoDetectPrefabs(TurretRegistry reg)
        {
            // Assets 전체에서 TurretBase를 가진 프리팹 탐색
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            var found = 0;
            var defMap = GetDefaultDefs();

            Undo.RecordObject(reg, "AutoDetect TurretRegistry");

            foreach (var guid in guids)
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var tb = prefab.GetComponent<TurretBase>();
                if (tb == null) continue;

                var ttype = tb.turretType;
                if (ttype == TurretType.None) continue;

                // 이미 등록된 타입이면 스킵
                if (reg.Get(ttype) != null) continue;

                defMap.TryGetValue(ttype, out var def);
                var entry = def ?? new TurretRegistry.TurretEntry
                {
                    type  = ttype,
                    sizeX = 1, sizeY = 1, cost = 10,
                    color = Color.white, label = ttype.ToString(), emoji = "🔫"
                };
                entry.prefab = prefab;

                // statData에서 sizeX/sizeY 우선 적용
                var sd = tb.statData;

                reg.entries.Add(entry);
                found++;
                Debug.Log($"[AutoDetect] {ttype} → {path}");
            }

            EditorUtility.SetDirty(reg);
            reg.RebuildCache();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("자동 탐색 완료",
                $"{found}개 터렛 프리팹을 찾아 Registry에 추가했습니다!", "확인");
        }

        // ── 검증 ─────────────────────────────────────────────────────

        private void ValidatePrefabs(TurretRegistry reg)
        {
            var missing = new List<string>();
            foreach (var e in reg.entries)
            {
                if (e.prefab == null)
                    missing.Add(e.type.ToString());
            }

            if (missing.Count == 0)
                EditorUtility.DisplayDialog("검증 완료", "모든 터렛에 Prefab이 연결되어 있습니다!", "확인");
            else
                EditorUtility.DisplayDialog("누락된 Prefab",
                    "다음 터렛에 Prefab이 없습니다:\n\n" + string.Join("\n", missing), "확인");
        }

        // ── 기본 TurretDef 값 (색상, 크기, 비용 등) ──────────────────

        private Dictionary<TurretType, TurretRegistry.TurretEntry> GetDefaultDefs()
        {
            return new Dictionary<TurretType, TurretRegistry.TurretEntry>
            {
                { TurretType.RangedTurret,     new TurretRegistry.TurretEntry { type=TurretType.RangedTurret,     sizeX=1,sizeY=1, cost=15, color=new Color(0.3f,0.6f,1f),    label="Ranged",      emoji="Gun",   isWall=false } },
                { TurretType.MeleeTurret,      new TurretRegistry.TurretEntry { type=TurretType.MeleeTurret,      sizeX=1,sizeY=1, cost=12, color=new Color(0.9f,0.7f,0.2f),  label="Melee",       emoji="Sword", isWall=false } },
                { TurretType.CrossMeleeTurret, new TurretRegistry.TurretEntry { type=TurretType.CrossMeleeTurret, sizeX=1,sizeY=1, cost=20, color=new Color(0.5f,1f,0.3f),    label="Cross",       emoji="Cross", isWall=false } },
                { TurretType.SpikeTrap,        new TurretRegistry.TurretEntry { type=TurretType.SpikeTrap,        sizeX=2,sizeY=1, cost=10, color=new Color(0.4f,0.35f,0.3f), label="Spikes",      emoji="Trap",  isPassable=true,  isWall=false } },
                { TurretType.ElectricGate,     new TurretRegistry.TurretEntry { type=TurretType.ElectricGate,     sizeX=3,sizeY=1, cost=20, color=new Color(0.9f,0.8f,0.1f),  label="Elec Gate",   emoji="Elec",  isWall=false } },
                { TurretType.Wall,             new TurretRegistry.TurretEntry { type=TurretType.Wall,             sizeX=1,sizeY=1, cost=5,  color=new Color(0.55f,0.45f,0.35f),label="Wall 1x1",   emoji="Wall",  isWall=true  } },
                { TurretType.Wall2x1,          new TurretRegistry.TurretEntry { type=TurretType.Wall2x1,          sizeX=2,sizeY=1, cost=8,  color=new Color(0.50f,0.40f,0.30f),label="Wall 2x1",   emoji="Wall",  isWall=true  } },
                { TurretType.Wall1x2,          new TurretRegistry.TurretEntry { type=TurretType.Wall1x2,          sizeX=1,sizeY=2, cost=8,  color=new Color(0.50f,0.40f,0.30f),label="Wall 1x2",   emoji="Wall",  isWall=true  } },
                { TurretType.Wall2x2,          new TurretRegistry.TurretEntry { type=TurretType.Wall2x2,          sizeX=2,sizeY=2, cost=12, color=new Color(0.45f,0.35f,0.25f),label="Wall 2x2",   emoji="Wall",  isWall=true  } },
                { TurretType.AreaDamage,       new TurretRegistry.TurretEntry { type=TurretType.AreaDamage,       sizeX=1,sizeY=1, cost=18, color=new Color(0.7f,0.2f,0.85f),  label="Area Dmg",    emoji="Area",  isWall=false } },
                { TurretType.ExplosiveCannon,  new TurretRegistry.TurretEntry { type=TurretType.ExplosiveCannon,  sizeX=1,sizeY=1, cost=22, color=new Color(1f,0.4f,0.1f),     label="Cannon",      emoji="Bomb",  isWall=false } },
                { TurretType.SlowShooter,      new TurretRegistry.TurretEntry { type=TurretType.SlowShooter,      sizeX=1,sizeY=1, cost=16, color=new Color(0.3f,0.6f,1f),     label="Slow",        emoji="Ice",   isWall=false } },
                { TurretType.RapidFire,        new TurretRegistry.TurretEntry { type=TurretType.RapidFire,        sizeX=1,sizeY=1, cost=14, color=new Color(1f,0.85f,0.2f),    label="Rapid",       emoji="Fast",  isWall=false } },
                { TurretType.Tornado,          new TurretRegistry.TurretEntry { type=TurretType.Tornado,          sizeX=1,sizeY=1, cost=25, color=new Color(0.5f,0.85f,1f),    label="Tornado",     emoji="Wind",  isWall=false } },
                { TurretType.LavaRain,         new TurretRegistry.TurretEntry { type=TurretType.LavaRain,         sizeX=1,sizeY=1, cost=20, color=new Color(1f,0.3f,0f),       label="Lava Rain",   emoji="Lava",  isWall=false } },
                { TurretType.ChainLightning,   new TurretRegistry.TurretEntry { type=TurretType.ChainLightning,   sizeX=1,sizeY=1, cost=24, color=new Color(0.4f,0.8f,1f),     label="Chain Bolt",  emoji="Bolt",  isWall=false } },
                { TurretType.BlackHole,        new TurretRegistry.TurretEntry { type=TurretType.BlackHole,        sizeX=1,sizeY=1, cost=30, color=new Color(0.5f,0f,0.8f),      label="Black Hole",  emoji="Hole",  isWall=false } },
                { TurretType.PrecisionStrike,  new TurretRegistry.TurretEntry { type=TurretType.PrecisionStrike,  sizeX=1,sizeY=1, cost=20, color=new Color(1f,0.95f,0.2f),    label="Precision",   emoji="Aim",   isWall=false } },
                { TurretType.GambleBat,        new TurretRegistry.TurretEntry { type=TurretType.GambleBat,        sizeX=1,sizeY=1, cost=18, color=new Color(0.9f,0.3f,0.9f),   label="Gamble Bat",  emoji="Bat",   isWall=false } },
                { TurretType.PulseSlower,      new TurretRegistry.TurretEntry { type=TurretType.PulseSlower,      sizeX=1,sizeY=1, cost=20, color=new Color(0.4f,0.8f,1f),     label="Pulse Slow",  emoji="Pulse", isWall=false } },
                { TurretType.DragonStatue,     new TurretRegistry.TurretEntry { type=TurretType.DragonStatue,     sizeX=1,sizeY=1, cost=28, color=new Color(1f,0.45f,0.1f),    label="Dragon",      emoji="Fire",  isWall=false } },
                { TurretType.HasteTower,       new TurretRegistry.TurretEntry { type=TurretType.HasteTower,       sizeX=1,sizeY=1, cost=22, color=new Color(0.9f,1f,0.3f),     label="Haste",       emoji="Haste", isWall=false } },
                { TurretType.PinballCannon,    new TurretRegistry.TurretEntry { type=TurretType.PinballCannon,    sizeX=1,sizeY=1, cost=24, color=new Color(1f,0.85f,0.1f),    label="Pinball",     emoji="Ball",  isWall=false } },
                { TurretType.BoomerangTurret,  new TurretRegistry.TurretEntry { type=TurretType.BoomerangTurret,  sizeX=1,sizeY=1, cost=20, color=new Color(0.5f,1f,0.3f),     label="Boomerang",   emoji="Boom",  isWall=false } },
            };
        }
    }
}
