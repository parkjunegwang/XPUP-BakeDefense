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
                target.TakeDamage(dmg, true);
                StartCoroutine(JackpotEffect(target.transform.position));
            }
            // 미스든 크리든 배트 휘두르는 이펙트
            StartCoroutine(SwingEffect(isCrit));
        }

        private IEnumerator SwingEffect(bool isJackpot)
        {
            if (barrelRenderer == null) yield break;

            float dur     = isJackpot ? 0.22f : 0.08f;
            float swingAmt = isJackpot ? 80f : 25f;
            float t       = 0f;
            float startZ  = barrelRenderer.transform.localEulerAngles.z;

            // 크리 시 노란색으로 번쩍
            Color origColor = barrelRenderer.color;
            if (isJackpot) barrelRenderer.color = new Color(1f, 0.95f, 0.1f);

            while (t < dur)
            {
                t += Time.deltaTime;
                float ratio = t / dur;
                float angle = Mathf.Sin(ratio * Mathf.PI) * swingAmt;
                barrelRenderer.transform.localEulerAngles = new Vector3(0f, 0f, startZ + angle);
                yield return null;
            }
            barrelRenderer.transform.localEulerAngles = new Vector3(0f, 0f, startZ);
            barrelRenderer.color = origColor;
        }

        private IEnumerator JackpotEffect(Vector3 pos)
        {
            // 대박 크리 - 황금빛 팽창 원
            var go = new GameObject("JackpotBurst");
            go.transform.position = pos;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop          = true;
            lr.positionCount = 32;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = SLayer.Effect;

            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float ratio  = t / 0.45f;
                float radius = Mathf.Lerp(0.05f, 2.0f, ratio);
                float alpha  = Mathf.Lerp(1f, 0f, ratio);
                float width  = Mathf.Lerp(0.12f, 0.02f, ratio);

                Color col     = new Color(1f, 0.9f, 0.1f, alpha);
                lr.startColor = col;
                lr.endColor   = col;
                lr.startWidth = width;
                lr.endWidth   = width;

                for (int i = 0; i < 32; i++)
                {
                    float angle = i * Mathf.PI * 2f / 32;
                    lr.SetPosition(i, new Vector3(
                        pos.x + Mathf.Cos(angle) * radius,
                        pos.y + Mathf.Sin(angle) * radius, -0.1f));
                }
                yield return null;
            }
            Destroy(go);
        }
    }
}
