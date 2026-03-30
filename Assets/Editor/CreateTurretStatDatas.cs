using UnityEngine;
using UnityEditor;

namespace Underdark
{
    /// <summary>
    /// 모든 터렛에 TurretStatData를 생성하고 프리팹에 연결
    /// </summary>
    public static class CreateTurretStatDatas
    {
        // (name, prefabPath, damage, range, fireRate, hp, critChance, critMult)
        static readonly (string n, string pf, float dmg, float rng, float fr, float hp, float cc, float cm)[] Defs =
        {
            ("RangedTurret",     "Assets/Prefabs/Turrets/PF_RangedTurret.prefab",     20f, 2.5f, 1.2f, 0f, 0.10f, 2f),
            ("MeleeTurret",      "Assets/Prefabs/Turrets/PF_MeleeTurret.prefab",      18f, 1.2f, 1.5f, 0f, 0.10f, 2f),
            ("SpikeTrap",        "Assets/Prefabs/Turrets/PF_SpikeTrap.prefab",        12f, 1.5f, 2.0f, 0f, 0.05f, 2f),
            ("ElectricGate",     "Assets/Prefabs/Turrets/PF_ElectricGate.prefab",     10f, 1.5f, 1.5f, 0f, 0.05f, 2f),
            ("AreaDamage",       "Assets/Prefabs/Turrets/PF_AreaDamage.prefab",       15f, 2.0f, 1.0f, 0f, 0.10f, 2f),
            ("ExplosiveCannon",  "Assets/Prefabs/Turrets/PF_ExplosiveCannon.prefab",  30f, 2.5f, 0.7f, 0f, 0.15f, 2f),
            ("SlowShooter",      "Assets/Prefabs/Turrets/PF_SlowShooter.prefab",      12f, 2.5f, 1.0f, 0f, 0.10f, 2f),
            ("RapidFire",        "Assets/Prefabs/Turrets/PF_RapidFire.prefab",        10f, 2.0f, 2.5f, 0f, 0.10f, 2f),
            ("Tornado",          "Assets/Prefabs/Turrets/PF_Tornado.prefab",          20f, 2.0f, 0.8f, 0f, 0.10f, 2f),
            ("LavaRain",         "Assets/Prefabs/Turrets/PF_LavaRain.prefab",         25f, 2.5f, 0.6f, 0f, 0.10f, 2f),
            ("ChainLightning",   "Assets/Prefabs/Turrets/PF_ChainLightning.prefab",   22f, 2.5f, 1.0f, 0f, 0.10f, 2f),
            ("BlackHole",        "Assets/Prefabs/Turrets/PF_BlackHole.prefab",        10f, 3.0f, 0.5f, 0f, 0.10f, 2f),
            ("PrecisionStrike",  "Assets/Prefabs/Turrets/PF_PrecisionStrike.prefab",  30f, 3.5f, 0.5f, 0f, 1.00f, 2f), // 항상 크리
            ("GambleBat",        "Assets/Prefabs/Turrets/PF_GambleBat.prefab",        25f, 2.5f, 1.0f, 0f, 0.30f, 3f),
            ("PulseSlower",      "Assets/Prefabs/Turrets/PF_PulseSlower.prefab",       5f, 2.5f, 0.4f, 0f, 0.05f, 2f),
            ("DragonStatue",     "Assets/Prefabs/Turrets/PF_DragonStatue.prefab",      4f, 2.2f, 1.0f, 0f, 0.05f, 2f),
            ("HasteTower",       "Assets/Prefabs/Turrets/PF_HasteTower.prefab",        0f, 2.5f, 1.0f, 0f, 0.00f, 1f),
            ("PinballCannon",    "Assets/Prefabs/Turrets/PF_PinballCannon.prefab",    20f, 3.0f, 1.0f, 0f, 0.15f, 2f),
            ("BoomerangTurret",  "Assets/Prefabs/Turrets/PF_BoomerangTurret.prefab",  18f, 3.5f, 0.8f, 0f, 0.10f, 2f),
        };

        [MenuItem("Underdark/Create & Link All TurretStatDatas")]
public static void CreateAndLink()
        {
            const string folder = "Assets/Data/Turrets";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Data", "Turrets");

            int count = 0;
            foreach (var d in Defs)
            {
                string assetPath = $"{folder}/SD_{d.n}.asset";

                var sd = AssetDatabase.LoadAssetAtPath<TurretStatData>(assetPath);
                if (sd == null)
                {
                    sd = ScriptableObject.CreateInstance<TurretStatData>();
                    AssetDatabase.CreateAsset(sd, assetPath);
                }

                // LevelStat (올바른 클래스명)
                var level = new TurretStatData.LevelStat
                {
                    level          = 1,
                    damage         = d.dmg,
                    range          = d.rng,
                    fireRate       = d.fr,
                    hp             = d.hp,
                    critChance     = d.cc,
                    critMultiplier = d.cm,
                };
                sd.levels = new TurretStatData.LevelStat[] { level };
                EditorUtility.SetDirty(sd);

                // 프리팹에 연결
                var prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(d.pf);
                if (prefabGO == null) { Debug.LogError($"[StatData] Prefab not found: {d.pf}"); continue; }

                var tb = prefabGO.GetComponent<TurretBase>();
                if (tb == null) { Debug.LogError($"[StatData] No TurretBase on: {d.pf}"); continue; }

                // SerializedObject로 statData + 기본값 저장
                var so = new SerializedObject(tb);
                so.FindProperty("statData").objectReferenceValue = sd;
                so.FindProperty("damage").floatValue   = d.dmg;
                so.FindProperty("range").floatValue    = d.rng;
                so.FindProperty("fireRate").floatValue = d.fr;
                so.FindProperty("hp").floatValue       = d.hp;
                so.FindProperty("critChance").floatValue     = d.cc;
                so.FindProperty("critMultiplier").floatValue = d.cm;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefabGO);

                Debug.Log($"[StatData] {d.n} OK: dmg={d.dmg} rng={d.rng} fr={d.fr}");
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatData] Complete! {count}/{Defs.Length} turrets.");
        }
    }
}
