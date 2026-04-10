using System.Collections;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 정밀 타격 타워
    /// - 기본 데미지 0, 크리티컬 확률 100%
    /// - 항상 크리티컬 팝업 뜨는 작은 데미지
    /// - 빠른 공속으로 꾸준히 크리 팝업 스팸
    /// </summary>
    public class PrecisionStrikeTurret : TurretBase
    {
        public GameObject projectilePrefab;


        [Header("Precision Settings")]
        [Tooltip("항상 크리티컬로 입히는 고정 데미지")]
        public float fixedCritDamage = 5f;

        protected override void Awake()
        {
            turretType = TurretType.PrecisionStrike;
            if (statData == null)
            {
                damage         = 0f;
                range          = 3.0f;
                fireRate       = 2.5f;  // 빠른 공속
                hp             = 60f;
                critChance     = 1.0f;  // 100% 크리
                critMultiplier = 1.0f;
            }
            base.Awake();
        }

        protected override void OnTick()
        {
            var target = FindClosestInRange();
            if (target == null) return;

            AimBarrel(target.transform.position);

            // fixedCritDamage를 StatData 있으면 거기서, 없으면 기본값
            float dmg = (statData != null && statData.GetLevel(level).damage > 0f)
                ? statData.GetLevel(level).damage
                : fixedCritDamage;

            //// 항상 크리티컬로 입힘
            //target.TakeDamage(dmg, true);

            Vector3 spawnPos = GetFirePosition();

            if (projectilePrefab != null)
            {
                bool was = projectilePrefab.activeSelf;
                projectilePrefab.SetActive(true);
                var go =
                    Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                projectilePrefab.SetActive(was);
                go.GetComponent<Projectile>()?.Init(target, dmg, true);
            }


            StartCoroutine(PrecisionFlash());
        }

        private IEnumerator PrecisionFlash()
        {
            var go = new GameObject("PrecisionFlash");
            go.transform.position = GetFirePosition();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameSetup.WhiteSquareStatic(4);
            sr.color = new Color(1f, 0.95f, 0.2f, 0.9f);
            sr.sortingOrder = SLayer.Effect;
            go.transform.localScale = Vector3.one * 0.12f;

            float t = 0f;
            while (t < 0.08f)
            {
                t += Time.deltaTime;
                float ratio = t / 0.08f;
                sr.color = new Color(1f, 0.95f, 0.2f, Mathf.Lerp(0.9f, 0f, ratio));
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.28f, ratio);
                yield return null;
            }
            Destroy(go);
        }
    }
}
