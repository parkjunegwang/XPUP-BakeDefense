using System.Collections;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 갬블 배트 타워
    /// - 기본 데미지 0, 매우 빠른 공속
    /// - 낮은 확률(기본 8%)로 크리티컬 → 엄청난 데미지
    /// - 미스 시 아무 데미지 없음 (배트 휘두르는 이펙트만)
    /// </summary>
    public class GambleBatTurret : TurretBase
    {

        public GameObject projectilePrefab_Normal;
        public GameObject projectilePrefab_Cri;

        [Header("Gamble Settings")]
        [Tooltip("크리티컬 시 입히는 고정 대박 데미지 (StatData 없을 때)")]
        public float jackpotDamage = 200f;

        protected override void Awake()
        {
            turretType = TurretType.GambleBat;
            if (statData == null)
            {
                damage         = 0f;
                range          = 2.8f;
                fireRate       = 4.0f;  // 매우 빠른 공속
                hp             = 50f;
                critChance     = 0.08f; // 8% 확률
                critMultiplier = 1f;
            }
            base.Awake();
        }

        protected override void OnTick()
        {
            var target = FindClosestInRange();
            if (target == null) return;

            AimBarrel(target.transform.position);

            bool isCrit = Random.value < critChance;

            if (isCrit)
            {
                // 대박! StatData에 설정된 critMultiplier 있으면 사용, 없으면 jackpotDamage
                float dmg = (statData != null && statData.GetLevel(level).critMultiplier > 1f)
                    ? statData.GetLevel(level).damage * statData.GetLevel(level).critMultiplier
                    : jackpotDamage;

                Vector3 spawnPos = GetFirePosition();

                if (projectilePrefab_Cri != null)
                {
                    bool was = projectilePrefab_Cri.activeSelf;
                    projectilePrefab_Cri.SetActive(true);
                    var go =
                        Instantiate(projectilePrefab_Cri, spawnPos, Quaternion.identity);
                    projectilePrefab_Cri.SetActive(was);
                    go.GetComponent<Projectile>()?.Init(target, dmg, isCrit);
                }
                else
                {
                    target.TakeDamage(dmg, true);

                }

                //    StartCoroutine(JackpotEffect(target.transform.position));
            }
            else
            {
                Vector3 spawnPos = GetFirePosition();

                if (projectilePrefab_Normal != null)
                {
                    bool was = projectilePrefab_Normal.activeSelf;
                    projectilePrefab_Normal.SetActive(true);
                    var go =
                        Instantiate(projectilePrefab_Normal, spawnPos, Quaternion.identity);
                    projectilePrefab_Normal.SetActive(was);
                    go.GetComponent<Projectile>()?.Init(target, 0, isCrit);
                }
                else
                {
                    target.TakeDamage(0, true);

                }
            }
            // 미스든 크리든 배트 휘두르는 이펙트
           // StartCoroutine(SwingEffect(isCrit));
        }
              
    }
}
