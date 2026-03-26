using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 체인 라이트닝 터렛 - 첫 타겟에서 최대 chainCount명에게 번개가 튕김
    /// 타겟이 부족하면 같은 몬스터에게 중복 적중
    /// </summary>
    public class ChainLightningTurret : TurretBase
    {
        [Header("Chain Settings")]
        [Tooltip("최대 튕기는 횟수 (3 = 최대 3명에게 데미지)")]
        public int   chainCount    = 3;
        [Tooltip("체인 튕기는 최대 거리")]
        public float chainRadius   = 2.5f;
        [Tooltip("튕길수록 데미지 감소 배율 (1.0 = 감소없음, 0.7 = 30% 감소)")]
        public float chainFalloff  = 0.75f;

        protected override void Awake()
        {
            turretType = TurretType.ChainLightning;
            if (statData == null) { damage = 18f; range = 3.0f; fireRate = 1.2f; hp = 70f; }
            base.Awake();
        }

protected override void OnTick() { var first = FindClosestInRange(); if (first == null) return; AimBarrel(first.transform.position); StartCoroutine(ChainRoutine(first)); }

        private IEnumerator ChainRoutine(Monster firstTarget)
        {
            var allMonsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            // 체인 대상 결정 (중복 허용)
            var chain = BuildChain(firstTarget, allMonsters);

            for (int i = 0; i < chain.Count; i++)
            {
                var from = i == 0 ? GetFirePosition() : chain[i - 1].transform.position;
                var to   = chain[i].transform.position;

                float dmg = damage * Mathf.Pow(chainFalloff, i);
                chain[i].TakeDamage(dmg);

                // 번개 이펙트
                SpawnLightningArc(from, to, i == 0);

                yield return new WaitForSeconds(0.07f);
            }
        }

        private List<Monster> BuildChain(Monster first, List<Monster> pool)
        {
            var chain   = new List<Monster> { first };
            var prev    = first;

            for (int i = 1; i < chainCount; i++)
            {
                // 주변에서 가장 가까운 살아있는 몬스터 탐색 (이미 체인된 것 우선 제외)
                Monster next = FindNearestExcluding(prev.transform.position, pool, chain);
                if (next == null)
                {
                    // 다음 타겟 없으면 체인 중 랜덤하게 재적중
                    next = chain[Random.Range(0, chain.Count)];
                }
                chain.Add(next);
                prev = next;
            }
            return chain;
        }

        private Monster FindNearestExcluding(Vector3 from, List<Monster> pool, List<Monster> exclude)
        {
            Monster best  = null;
            float   minD  = float.MaxValue;
            foreach (var m in pool)
            {
                if (m == null || !m.IsAlive) continue;
                if (exclude.Contains(m)) continue;
                float d = Vector2.Distance(from, m.transform.position);
                if (d <= chainRadius && d < minD) { minD = d; best = m; }
            }
            return best;
        }

        private void SpawnLightningArc(Vector3 from, Vector3 to, bool isFirst)
        {
            // 지그재그 번개 라인
            var go = new GameObject("LightningArc");
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace    = true;
            lr.loop             = false;
            lr.material         = new Material(Shader.Find("Sprites/Default"));
            lr.startWidth       = isFirst ? 0.08f : 0.05f;
            lr.endWidth         = 0.02f;
            lr.sortingOrder     = SLayer.Effect;

            Color c = isFirst
                ? new Color(0.5f, 0.8f, 1f, 1f)
                : new Color(0.7f, 0.9f, 1f, 0.8f);
            lr.startColor = c;
            lr.endColor   = new Color(c.r, c.g, c.b, 0f);

            // 지그재그 포인트 생성
            int   segs   = 6;
            float jitter = 0.15f;
            lr.positionCount = segs;
            for (int i = 0; i < segs; i++)
            {
                float t   = (float)i / (segs - 1);
                Vector3 p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < segs - 1)
                    p += new Vector3(
                        Random.Range(-jitter, jitter),
                        Random.Range(-jitter, jitter), 0f);
                lr.SetPosition(i, p);
            }

            Destroy(go, 0.12f);
        }
    }
}
