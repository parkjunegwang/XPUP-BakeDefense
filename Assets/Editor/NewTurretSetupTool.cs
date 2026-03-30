using UnityEngine;
using UnityEditor;

namespace Underdark
{
    public static class NewTurretSetupTool
    {
        [MenuItem("Underdark/Setup New Turret Defaults")]
        public static void SetupDefaults()
        {
            SetPrefab<PulseSlower>("Assets/Prefabs/Turrets/PF_PulseSlower.prefab",
                damage: 5f, range: 2.5f, fireRate: 0.4f);

            SetPrefab<DragonStatue>("Assets/Prefabs/Turrets/PF_DragonStatue.prefab",
                damage: 0f, range: 2.2f, fireRate: 1f);

            SetPrefab<HasteTower>("Assets/Prefabs/Turrets/PF_HasteTower.prefab",
                damage: 0f, range: 2.5f, fireRate: 1f);

            SetPrefab<PinballCannon>("Assets/Prefabs/Turrets/PF_PinballCannon.prefab",
                damage: 20f, range: 3.0f, fireRate: 1.0f);

            SetPrefab<BoomerangTurret>("Assets/Prefabs/Turrets/PF_BoomerangTurret.prefab",
                damage: 18f, range: 3.5f, fireRate: 0.8f);

            AssetDatabase.SaveAssets();
            Debug.Log("[NewTurretSetup] All 5 new turret prefabs configured!");
        }

        static void SetPrefab<T>(string path, float damage, float range, float fireRate)
            where T : TurretBase
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError($"Not found: {path}"); return; }

            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            var t  = go.GetComponent<T>();
            if (t == null) { Debug.LogError($"No {typeof(T).Name} on {path}"); Object.DestroyImmediate(go); return; }

            if (damage   > 0) t.damage   = damage;
            if (range    > 0) t.range    = range;
            if (fireRate > 0) t.fireRate = fireRate;

            PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(go);
            Debug.Log($"[NewTurretSetup] {typeof(T).Name}: damage={damage} range={range} fireRate={fireRate}");
        }
    }
}
