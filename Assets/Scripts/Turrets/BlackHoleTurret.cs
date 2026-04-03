using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 블랙홀 터렛
    /// - 느린 공격속도로 가까운 타일에 블랙홀 소환
    /// - 블랙홀은 일정 시간 동안 주변 적을 끌어당기며 약한 지속 데미지
    /// - 레벨업 → 크기 증가 + 끌어당기는 힘 증가
    /// </summary>
    public class BlackHoleTurret : TurretBase
    {
        [Header("Sprite")]
        [Tooltip("블랙홀 코어 스프라이트 (없으면 기본 어두운 사각형)")]
        public Sprite coreSprite;

        [Header("Black Hole Settings")]
        [Tooltip("블랙홀 지속 시간")]
        public float holeDuration   = 4.0f;
        [Tooltip("블랙홀 반경 (레벨1 기준)")]
        public float holeRadius     = 1.5f;
        [Tooltip("끌어당기는 힘 (레벨1 기준)")]
        public float suctionForce   = 2.0f;
        [Tooltip("틱 데미지 간격 (초)")]
        public float tickInterval   = 0.3f;
        [Tooltip("동시에 존재할 수 있는 블랙홀 최대 수")]
        public int   maxHoles       = 1;

        // 현재 활성 블랙홀 수
        private int _activeHoles = 0;

protected override void OnTick()
        {
            if (_activeHoles >= maxHoles) return;

            var target = FindClosestInRange();
            if (target == null) return;

            Vector3 spawnPos = SnapToTile(target.transform.position);
            StartCoroutine(BlackHoleRoutine(spawnPos));
        }

        private Vector3 SnapToTile(Vector3 worldPos)
        {
            var map = MapManager.Instance;
            if (map == null) return worldPos;
            float step    = map.tileSize + map.tileGap;
            float offsetX = -(map.columns - 1) * step * 0.5f;
            float offsetY = -(map.rows    - 1) * step * 0.5f;
            int gx = Mathf.RoundToInt((worldPos.x - offsetX) / step);
            int gy = Mathf.RoundToInt((worldPos.y - offsetY) / step);
            gx = Mathf.Clamp(gx, 0, map.columns - 1);
            gy = Mathf.Clamp(gy, 0, map.rows    - 1);
            return map.GridToWorld(gx, gy);
        }

private IEnumerator BlackHoleRoutine(Vector3 center)
        {
            _activeHoles++;

            // 블랙홀 크기는 holeRadius를 따로 사용 (타워 range와 스스로 다름)
            float levelMult  = 1f + (level - 1) * 0.35f;
            float curRadius  = holeRadius  * levelMult; // 블랙홀 자체 반경
            float curSuction = suctionForce * levelMult;

            var holeGo = new GameObject("BlackHole");
            holeGo.transform.position = center;

            var lr = holeGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop          = true;
            lr.positionCount = 48;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = SLayer.Effect;

            var coreGo = new GameObject("Core");
            coreGo.transform.SetParent(holeGo.transform, false);
            var coreSr = coreGo.AddComponent<SpriteRenderer>();
            if (coreSprite != null)
            {
                coreSr.sprite = coreSprite;
                coreSr.color  = Color.white;
            }
            else
            {
                coreSr.sprite = GameSetup.WhiteSquareStatic();
                coreSr.color  = new Color(0.05f, 0f, 0.1f, 0.92f);
            }
            coreSr.sortingOrder = SLayer.Effect - 1;
            coreGo.transform.localScale = Vector3.one * curRadius * 2f * 0.82f;

            float elapsed   = 0f;
            float tickTimer = 0f;

            while (elapsed < holeDuration)
            {
                if (GameManager.Instance == null ||
                    GameManager.Instance.CurrentState != GameState.WaveInProgress)
                    break;

                elapsed   += Time.deltaTime;
                tickTimer += Time.deltaTime;
                float t    = elapsed / holeDuration;

                DrawRing(lr, center, curRadius, t);
                coreGo.transform.Rotate(0f, 0f, 120f * Time.deltaTime);

                var monsters = new List<Monster>(MonsterManager.Instance?.ActiveMonsters ?? new List<Monster>());
                foreach (var m in monsters)
                {
                    if (m == null || !m.IsAlive) continue;
                    float dist = Vector2.Distance(center, m.transform.position);
                    if (dist > curRadius) continue; // 블랙홀 반경으로만 끝어당기기

                    Vector3 dir   = (center - m.transform.position).normalized;
                    float falloff = 1f - (dist / curRadius);
                    m.transform.position += dir * curSuction * falloff * Time.deltaTime;

                    if (tickTimer >= tickInterval)
                        m.TakeDamage(damage, false);
                }

                if (tickTimer >= tickInterval)
                    tickTimer = 0f;

                yield return null;
            }

            if (GameManager.Instance?.CurrentState == GameState.WaveInProgress)
                yield return StartCoroutine(CollapseEffect(holeGo, lr, center, curRadius));

            Destroy(holeGo);
            _activeHoles--;
        }

        private IEnumerator CollapseEffect(GameObject go, LineRenderer lr, Vector3 center, float radius)
        {
            float t = 0f;
            float dur = 0.35f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float r = radius * (1f - t / dur);
                float a = 1f - t / dur;
                DrawRing(lr, center, r, 1f, a);
                yield return null;
            }
        }

        private void DrawRing(LineRenderer lr, Vector3 center, float radius, float t, float alpha = -1f)
        {
            // t=0: 어두운 보라, t=1: 밝은 보라
            float a = alpha >= 0f ? alpha : Mathf.Lerp(0.5f, 0.9f, t);
            Color col = Color.Lerp(
                new Color(0.3f, 0f, 0.8f, a),
                new Color(0.7f, 0.2f, 1f,  a), t);
            lr.startColor  = col;
            lr.endColor    = col;
            lr.startWidth  = 0.045f + t * 0.04f;
            lr.endWidth    = lr.startWidth;

            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48f;
                lr.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius, -0.1f));
            }
        }

        // 레벨업 시 최대 블랙홀 수 증가 (3레벨부터 2개)
        protected override void OnLevelUp()
        {
            maxHoles = level >= 3 ? 2 : 1;
        }
    }
}
