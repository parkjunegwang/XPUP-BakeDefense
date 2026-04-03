using UnityEngine;
using UnityEditor;

namespace Underdark
{
    public class LinkExplosiveCannonPrefabs
    {
        [MenuItem("Underdark/Link Explosive Cannon Prefabs")]
        public static void Link()
        {
            var cannonPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Turrets/PF_ExplosiveCannon.prefab");
            var shellPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/PF_ExplosiveShell.prefab");
            var explodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/PF_Explosion.prefab");

            if (cannonPrefab == null) { Debug.LogError("PF_ExplosiveCannon not found!"); return; }

            var instance = PrefabUtility.InstantiatePrefab(cannonPrefab) as GameObject;
            var cannon   = instance.GetComponent<ExplosiveCannon>();
            if (cannon == null) { Debug.LogError("ExplosiveCannon component not found!"); Object.DestroyImmediate(instance); return; }

            cannon.shellPrefab     = shellPrefab;
            cannon.explosionPrefab = explodePrefab;

            var so = new SerializedObject(cannon);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();

            Debug.Log("[LinkExplosiveCannonPrefabs] shellPrefab + explosionPrefab 연결 완료!");
        }
    }
}
