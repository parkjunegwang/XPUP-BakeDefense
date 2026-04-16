using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 스테이지 클리어 기록, 씬 전환 데이터, 터렛 소유 정보를 PlayerPrefs에 저장/로드.
    /// </summary>
    public static class SaveData
    {
        private const string KEY_PREFIX        = "stage_clear_";
        private const string KEY_SELECTED      = "selected_stage";
        private const string KEY_TURRET_POOL   = "selected_turrets";
        private const string KEY_OWNED_PREFIX  = "owned_turret_";

        // ── 스테이지 클리어 ──────────────────────────────────────────

        public static void SetCleared(int stageIndex, bool cleared)
        {
            PlayerPrefs.SetInt(KEY_PREFIX + stageIndex, cleared ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool IsCleared(int stageIndex)
            => PlayerPrefs.GetInt(KEY_PREFIX + stageIndex, 0) == 1;

        public static bool IsUnlocked(int stageIndex)
            => stageIndex == 0 || IsCleared(stageIndex - 1);

        // ── 씬 전환 데이터 ───────────────────────────────────────────

        public static int SelectedStageIndex
        {
            get => PlayerPrefs.GetInt(KEY_SELECTED, 0);
            set { PlayerPrefs.SetInt(KEY_SELECTED, value); PlayerPrefs.Save(); }
        }

        public static TurretType[] SelectedTurrets
        {
            get
            {
                string raw = PlayerPrefs.GetString(KEY_TURRET_POOL, "");
                if (string.IsNullOrEmpty(raw)) return new TurretType[0];
                var parts  = raw.Split(',');
                var result = new List<TurretType>();
                foreach (var p in parts)
                    if (System.Enum.TryParse<TurretType>(p.Trim(), out var t))
                        result.Add(t);
                return result.ToArray();
            }
            set
            {
                if (value == null || value.Length == 0)
                {
                    PlayerPrefs.DeleteKey(KEY_TURRET_POOL);
                }
                else
                {
                    var parts = new string[value.Length];
                    for (int i = 0; i < value.Length; i++)
                        parts[i] = value[i].ToString();
                    PlayerPrefs.SetString(KEY_TURRET_POOL, string.Join(",", parts));
                }
                PlayerPrefs.Save();
            }
        }

        // ── 터렛 소유 ────────────────────────────────────────────────

        /// <summary>특정 터렛을 소유하고 있는지 확인</summary>
        public static bool IsOwned(TurretType type)
            => PlayerPrefs.GetInt(KEY_OWNED_PREFIX + type.ToString(), 0) == 1;

        /// <summary>특정 터렛의 소유 상태 설정</summary>
        public static void SetOwned(TurretType type, bool owned)
        {
            PlayerPrefs.SetInt(KEY_OWNED_PREFIX + type.ToString(), owned ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// TurretRegistry의 isDefault=true 터렛들을 기본 소유로 초기화.
        /// 아직 한 번도 설정된 적 없는 터렛만 초기화 (기존 데이터 보존).
        /// </summary>
        public static void InitDefaultOwnership(TurretRegistry registry)
        {
            if (registry == null) return;
            foreach (var entry in registry.All)
            {
                if (entry == null || entry.type == TurretType.None) continue;
                string key = KEY_OWNED_PREFIX + entry.type.ToString();
                if (!PlayerPrefs.HasKey(key) && entry.isDefault)
                    PlayerPrefs.SetInt(key, 1);
            }
            PlayerPrefs.Save();
        }

        // ── 전체 초기화 (디버그용) ───────────────────────────────────

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
