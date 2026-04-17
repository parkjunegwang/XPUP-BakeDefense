using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class PinballCannon : TurretBase
    {
        [Header("Pinball Settings")]
        public int   maxBounces    = 5;
        public float bulletSpeed   = 7f;
        public float bulletSize    = 0.22f;
        public float hitRadius     = 0.4f;
        public float maxTravelDist = 22f;

        protected override void OnTick()
        {
            var target = FindClosestInRange();
            if (target == null) return;
            
            Vector2 startPos = GetFirePosition();
            Vector2 initDir  = ((Vector2)target.transform.position - startPos).normalized;

            var path = SimulatePath(startPos, initDir);
            if (path == null || path.Count < 2) return;

            bool  isCrit;
            float dmg = RollDamage(out isCrit);

            var go = new GameObject("PinballBullet");
            go.transform.position = startPos;

            var sr     = go.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>("Image/PinballCannon_Ball");
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (sprite == null) sprite = GameSetup.WhiteSquareStatic();
            sr.sprite       = sprite;
            //sr.color        = new Color(1f, 0.85f, 0.1f);
            sr.sortingOrder = SLayer.Projectile;
          //  go.transform.localScale = Vector3.one * bulletSize;

            go.AddComponent<PinballBullet>().Init(path, bulletSpeed, dmg, isCrit, hitRadius);
        }

        // ───────────────────────────────────────────────────────────────
        // 경로 시뮬레이션 - 그리드 기반이지만 "연속 레이" 방식으로 정확하게
        // ───────────────────────────────────────────────────────────────
        private List<Vector2> SimulatePath(Vector2 start, Vector2 dir)
        {
            var map = MapManager.Instance;
            if (map == null) return null;

            float step    = map.tileSize + map.tileGap; // 타일 1칸 크기 (예: 1.05)
            float offsetX = -(map.columns - 1) * step * 0.5f;
            float offsetY = -(map.rows    - 1) * step * 0.5f;

            var  waypoints = new List<Vector2> { start };
            Vector2 pos    = start;
            Vector2 curDir = dir.normalized;
            float   traveled = 0f;
            int     bounces  = 0;

            // 시작 그리드 위치 기억 (자기 타일 무시)
            int prevGx = WorldToGrid(pos.x, offsetX, step);
            int prevGy = WorldToGrid(pos.y, offsetY, step);

            // 충분히 작은 스텝으로 이동하면서 벽 감지
            // 스텝 = 타일 크기의 1/10 (정밀도와 성능 균형)
            float simStep = step * 0.1f;

            while (traveled < maxTravelDist)
            {
                Vector2 next  = pos + curDir * simStep;
                int     newGx = WorldToGrid(next.x, offsetX, step);
                int     newGy = WorldToGrid(next.y, offsetY, step);

                // 그리드 밖
                if (newGx < 0 || newGx >= map.columns || newGy < 0 || newGy >= map.rows)
                {
                    waypoints.Add(next);
                    break;
                }

                bool changedCell = (newGx != prevGx || newGy != prevGy);

                if (changedCell)
                {
                    var tile = map.GetTile(newGx, newGy);
                    bool isWall = tile != null && tile.placedTurret != null && !tile.passableOverride;

                    if (isWall)
                    {
                        if (bounces >= maxBounces)
                        {
                            waypoints.Add(pos);
                            break;
                        }

                        // ── 반사 면 판별 ──────────────────────────────
                        // X축으로만 칸이 바뀌었으면 → 좌/우 벽 → X반사
                        // Y축으로만 칸이 바뀌었으면 → 위/아래 벽 → Y반사
                        // 대각 이동이면 두 방향 모두 체크해서 결정
                        bool xChanged = (newGx != prevGx);
                        bool yChanged = (newGy != prevGy);

                        if (xChanged && yChanged)
                        {
                            // 대각: X방향 타일과 Y방향 타일 중 어느 쪽이 막혔는지 확인
                            var tileX = map.GetTile(newGx, prevGy);
                            var tileY = map.GetTile(prevGx, newGy);
                            bool xBlocked = tileX != null && tileX.placedTurret != null && !tileX.passableOverride;
                            bool yBlocked = tileY != null && tileY.placedTurret != null && !tileY.passableOverride;

                            if (xBlocked && !yBlocked)
                                curDir = new Vector2(-curDir.x,  curDir.y);
                            else if (!xBlocked && yBlocked)
                                curDir = new Vector2( curDir.x, -curDir.y);
                            else
                                curDir = new Vector2(-curDir.x, -curDir.y); // 코너
                        }
                        else if (xChanged)
                            curDir = new Vector2(-curDir.x,  curDir.y);
                        else
                            curDir = new Vector2( curDir.x, -curDir.y);

                        bounces++;
                        waypoints.Add(pos); // 반사 지점 기록
                        // 반사 후에도 prevGx/Gy는 현재 위치 유지 (pos는 안 바뀜)
                        traveled += simStep;
                        continue; // pos는 그대로, 다음 스텝부터 새 방향으로
                    }
                }

                // 벽 없음 → 이동
                pos     = next;
                prevGx  = newGx;
                prevGy  = newGy;
                traveled += simStep;
            }

            // 마지막 지점
            if (waypoints[waypoints.Count - 1] != pos)
                waypoints.Add(pos);

            return waypoints;
        }

        private int WorldToGrid(float world, float offset, float step)
            => Mathf.RoundToInt((world - offset) / step);

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            var target = FindClosestInRange();
            if (target == null) return;
            Vector2 startPos = transform.position;
            Vector2 initDir  = ((Vector2)target.transform.position - startPos).normalized;
            var path = SimulatePath(startPos, initDir);
            if (path == null) return;
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(path[i], path[i + 1]);
            Gizmos.color = Color.red;
            for (int i = 1; i < path.Count - 1; i++)
                Gizmos.DrawWireSphere(path[i], 0.12f);
        }
#endif
    }

    // ─────────────────────────────────────────────────────────────────
    // 발사체: waypoint 경로를 따라 이동, 몬스터 히트 판정
    // ─────────────────────────────────────────────────────────────────
    public class PinballBullet : MonoBehaviour
    {
        private List<Vector2>    _path;
        private int              _pathIdx;
        private float            _speed;
        private float            _damage;
        private bool             _isCrit;
        private float            _hitRadius;
        private HashSet<Monster> _hit = new HashSet<Monster>();

        public void Init(List<Vector2> path, float speed, float damage, bool isCrit, float hitRadius)
        {
            _path      = path;
            _pathIdx   = 1;
            _speed     = speed;
            _damage    = damage;
            _isCrit    = isCrit;
            _hitRadius = hitRadius;

            // 수명 = 전체 경로 길이 / 속도 + 여유
            float totalDist = 0f;
            for (int i = 0; i < path.Count - 1; i++)
                totalDist += Vector2.Distance(path[i], path[i + 1]);
            Destroy(gameObject, totalDist / Mathf.Max(speed, 0.1f) + 1f);
        }

        private void Update()
        {
            if (_path == null || _pathIdx >= _path.Count)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 cur  = transform.position;
            Vector2 dest = _path[_pathIdx];
            float   dist = Vector2.Distance(cur, dest);
            float   move = _speed * Time.deltaTime;

            if (move >= dist)
            {
                transform.position = (Vector3)(Vector2)dest;
                _pathIdx++;
            }
            else
            {
                transform.position = (Vector3)(cur + (dest - cur).normalized * move);
            }

            CheckHit();
        }

        private void CheckHit()
        {
            var monsters = MonsterManager.Instance?.ActiveMonsters;
            if (monsters == null) return;
            var snap = new List<Monster>(monsters);
            foreach (var m in snap)
            {
                if (m == null || !m.IsAlive || _hit.Contains(m)) continue;
                if (Vector2.Distance(transform.position, m.transform.position) < _hitRadius)
                {
                    _hit.Add(m);
                    m.TakeDamage(_damage, _isCrit);
                }
            }
        }
    }
}
