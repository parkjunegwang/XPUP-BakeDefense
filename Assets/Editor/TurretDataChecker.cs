using UnityEngine;
using UnityEditor;

namespace Underdark
{
    public static class TurretDataChecker
    {
        [MenuItem("Underdark/Check All Turret Data")]
public static void CheckAll()
        {
            var entries = new System.Text.StringBuilder();
            entries.AppendLine("=== TURRET DATA CHECK ===");

            string[] prefabPaths = {
                "Assets/Prefabs/Turrets/PF_RangedTurret.prefab",
                "Assets/Prefabs/Turrets/PF_MeleeTurret.prefab",
                "Assets/Prefabs/Turrets/PF_SpikeTrap.prefab",
                "Assets/Prefabs/Turrets/PF_ElectricGate.prefab",
                "Assets/Prefabs/Turrets/PF_AreaDamage.prefab",
                "Assets/Prefabs/Turrets/PF_ExplosiveCannon.prefab",
                "Assets/Prefabs/Turrets/PF_SlowShooter.prefab",
                "Assets/Prefabs/Turrets/PF_RapidFire.prefab",
                "Assets/Prefabs/Turrets/PF_Tornado.prefab",
                "Assets/Prefabs/Turrets/PF_LavaRain.prefab",
                "Assets/Prefabs/Turrets/PF_ChainLightning.prefab",
                "Assets/Prefabs/Turrets/PF_BlackHole.prefab",
                "Assets/Prefabs/Turrets/PF_PrecisionStrike.prefab",
                "Assets/Prefabs/Turrets/PF_GambleBat.prefab",
                "Assets/Prefabs/Turrets/PF_PulseSlower.prefab",
                "Assets/Prefabs/Turrets/PF_DragonStatue.prefab",
                "Assets/Prefabs/Turrets/PF_HasteTower.prefab",
                "Assets/Prefabs/Turrets/PF_PinballCannon.prefab",
                "Assets/Prefabs/Turrets/PF_BoomerangTurret.prefab",
            };

            // 없어도 되는 damage 터렛 (range/aoe 기반)
            var noDmgOk = new System.Collections.Generic.HashSet<string> {
                "DragonStatue", "HasteTower", "SpikeTrap", "ElectricGate",
                "Tornado", "AreaDamage"
            };

            int issues = 0;
            foreach (var path in prefabPaths)
            {
                string tName = System.IO.Path.GetFileNameWithoutExtension(path).Replace("PF_","");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { entries.AppendLine($"  MISSING_PREFAB: {tName}"); issues++; continue; }

                var tb = prefab.GetComponent<TurretBase>();
                if (tb == null) { entries.AppendLine($"  NO_COMPONENT: {tName}"); issues++; continue; }

                bool hasBody   = prefab.transform.Find("Body")   != null;
                bool hasBarrel = prefab.transform.Find("Barrel") != null;
                bool dmgOk  = tb.damage   > 0 || noDmgOk.Contains(tName);
                bool rngOk  = tb.range    > 0;
                bool frOk   = tb.fireRate > 0;

                string status = (dmgOk && rngOk && frOk && hasBody && hasBarrel) ? "OK" : "ISSUE";
                string row = $"  [{status}] {tName,-20} dmg={tb.damage,5:F1} rng={tb.range,4:F1} fr={tb.fireRate,4:F2}";
                if (!hasBody)   row += " [noBody]";
                if (!hasBarrel) row += " [noBarrel]";
                if (!dmgOk)     row += " [noDmg]";
                if (!rngOk)     row += " [noRange]";
                if (!frOk)      row += " [noFireRate]";
                entries.AppendLine(row);
                if (status == "ISSUE") issues++;
            }
            entries.AppendLine($"=== {issues} ISSUES ===");
            // 이슈가 있으면 Warning, 없으면 Log
            if (issues > 0)
                Debug.LogWarning(entries.ToString());
            else
                Debug.Log(entries.ToString());
        }
    }
}
