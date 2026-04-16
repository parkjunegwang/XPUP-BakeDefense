using UnityEngine;
using UnityEditor;

namespace Underdark
{
    public static class FixAllTurretPrefabs
    {
        [MenuItem("Underdark/Fix All Turret Prefabs")]
        public static void FixAll()
        {
            // (prefab path, damage, range, fireRate) - 0 = don't change
            var configs = new (string path, System.Type script, float dmg, float rng, float fr)[]
            {
                ("Assets/Prefabs/Turrets/PF_RangedTurret.prefab",    typeof(RangedTurret),         20f, 2.5f, 1.2f),
                ("Assets/Prefabs/Turrets/PF_MeleeTurret.prefab",     typeof(MeleeTurret),          18f, 1.2f, 1.5f),
                ("Assets/Prefabs/Turrets/PF_SpikeTrap.prefab",       typeof(SpikeTrap),            12f, 1.5f, 2.0f),
                ("Assets/Prefabs/Turrets/PF_AreaDamage.prefab",      typeof(AreaDamageTurret),     15f, 2.0f, 1.0f),
                ("Assets/Prefabs/Turrets/PF_ExplosiveCannon.prefab", typeof(ExplosiveCannon),      30f, 2.5f, 0.7f),
                ("Assets/Prefabs/Turrets/PF_SlowShooter.prefab",     typeof(SlowShooterTurret),    12f, 2.5f, 1.0f),
                ("Assets/Prefabs/Turrets/PF_RapidFire.prefab",       typeof(RapidFireTurret),      10f, 2.0f, 2.5f),
                ("Assets/Prefabs/Turrets/PF_Tornado.prefab",         typeof(TornadoTurret),        20f, 2.0f, 0.8f),
                ("Assets/Prefabs/Turrets/PF_LavaRain.prefab",        typeof(LavaRainTurret),       25f, 2.5f, 0.6f),
                ("Assets/Prefabs/Turrets/PF_ChainLightning.prefab",  typeof(ChainLightningTurret), 22f, 2.5f, 1.0f),
                ("Assets/Prefabs/Turrets/PF_BlackHole.prefab",       typeof(BlackHoleTurret),      10f, 3.0f, 0.5f),
                ("Assets/Prefabs/Turrets/PF_PrecisionStrike.prefab", typeof(PrecisionStrikeTurret),30f, 3.5f, 0.5f),
                ("Assets/Prefabs/Turrets/PF_GambleBat.prefab",       typeof(GambleBatTurret),      25f, 2.5f, 1.0f),
                ("Assets/Prefabs/Turrets/PF_PulseSlower.prefab",     typeof(PulseSlower),          5f,  2.5f, 0.4f),
                ("Assets/Prefabs/Turrets/PF_DragonStatue.prefab",    typeof(DragonStatue),         0f,  2.2f, 1.0f),
                ("Assets/Prefabs/Turrets/PF_HasteTower.prefab",      typeof(HasteTower),           0f,  2.5f, 1.0f),
                ("Assets/Prefabs/Turrets/PF_PinballCannon.prefab",   typeof(PinballCannon),        20f, 3.0f, 1.0f),
                ("Assets/Prefabs/Turrets/PF_BoomerangTurret.prefab", typeof(BoomerangTurret),      18f, 3.5f, 0.8f),
            };

            int fixed_count = 0;
            foreach (var cfg in configs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(cfg.path);
                if (prefab == null) { Debug.LogError($"[Fix] Prefab not found: {cfg.path}"); continue; }

                // 프리팹 인스턴스로 편집
                var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

                // SpriteRenderer가 루트에 없으면 추가
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = GenerateWhiteSprite();
                    sr.color = Color.grey;
                    sr.sortingOrder = SLayer.Turret;
                }

                // TurretBase 컴포넌트 가져오기
                var tb = go.GetComponent<TurretBase>();
                if (tb == null)
                {
                    Debug.LogError($"[Fix] No TurretBase on {cfg.path}");
                    Object.DestroyImmediate(go);
                    continue;
                }

                // 값 강제 설정
                if (cfg.dmg > 0) tb.damage   = cfg.dmg;
                if (cfg.rng > 0) tb.range    = cfg.rng;
                if (cfg.fr  > 0) tb.fireRate = cfg.fr;

                // Body/Barrel 중복 제거
                CleanDuplicateChildren(go, "Body");
                CleanDuplicateChildren(go, "Barrel");

                // Body/Barrel 없으면 추가
                EnsureChild(go, "Body");
                EnsureChild(go, "Barrel");

                PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
                Object.DestroyImmediate(go);
                fixed_count++;
                Debug.Log($"[Fix] {System.IO.Path.GetFileNameWithoutExtension(cfg.path)}: dmg={cfg.dmg} rng={cfg.rng} fr={cfg.fr}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fix] Done! Fixed {fixed_count} prefabs.");
        }

        static void CleanDuplicateChildren(GameObject go, string childName)
        {
            var found = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in go.transform)
                if (c.name == childName) found.Add(c);
            // 첫 번째만 남기고 나머지 삭제
            for (int i = 1; i < found.Count; i++)
                Object.DestroyImmediate(found[i].gameObject);
        }

        static void EnsureChild(GameObject go, string childName)
        {
            if (go.transform.Find(childName) != null) return;
            var child = new GameObject(childName);
            child.transform.SetParent(go.transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateWhiteSprite();
            sr.color = childName == "Body" ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.4f, 0.4f, 0.4f);
            sr.sortingOrder = SLayer.Turret + 1;
        }

        static Sprite GenerateWhiteSprite()
        {
            var tex = new Texture2D(32, 32);
            var pix = new Color32[32 * 32];
            for (int i = 0; i < pix.Length; i++) pix[i] = Color.white;
            tex.SetPixels32(pix);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }
    }
}
