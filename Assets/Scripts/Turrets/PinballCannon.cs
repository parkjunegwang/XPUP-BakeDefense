using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 발사한 포탄이 배치된 터렛(벽)에 닿으면 튕겨서 계속 이동. 핀볼 스타일.
    /// </summary>
    public class PinballCannon : TurretBase
    {
        [Header("Pinball Settings")]
        public int   maxBounces  = 5;
        public float bulletSpeed = 6f;
        public float bulletSize  = 0.22f;

        protected override void OnTick()
        {
            var target = FindClosestInRange();
            if (target == null) return;

            Vector2 dir = (target.transform.position - transform.position).normalized;
            bool isCrit;
            float dmg = RollDamage(out isCrit);

            var go = new GameObject("PinballBullet");
            go.transform.position = transform.position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = GetComponent<SpriteRenderer>()?.sprite;
            sr.color        = new Color(1f, 0.85f, 0.1f);
            sr.sortingOrder = SLayer.Projectile;
            go.transform.localScale = Vector3.one * bulletSize;
            go.AddComponent<PinballBullet>().Init(dir, bulletSpeed, dmg, isCrit, maxBounces);
        }
    }

    public class PinballBullet : MonoBehaviour
    {
        private Vector2           _dir;
        private float             _speed;
        private float             _damage;
        private bool              _isCrit;
        private int               _bouncesLeft;
        private HashSet<Monster>  _hit = new HashSet<Monster>();

        public void Init(Vector2 dir, float speed, float damage, bool isCrit, int bounces)
        {
            _dir         = dir.normalized;
            _speed       = speed;
            _damage      = damage;
            _isCrit      = isCrit;
            _bouncesLeft = bounces;
            Destroy(gameObject, 6f);
        }

private void Update()
        {
            Vector2 pos  = transform.position;
            Vector2 next = pos + _dir * _speed * Time.deltaTime;

            var map = MapManager.Instance;
            if (map != null)
            {
                // MapManager의 실제 그리드 좌표 역산
                float step    = map.tileSize + map.tileGap;
                float offsetX = -(map.columns - 1) * step * 0.5f;
                float offsetY = -(map.rows    - 1) * step * 0.5f;
                int gx = Mathf.RoundToInt((next.x - offsetX) / step);
                int gy = Mathf.RoundToInt((next.y - offsetY) / step);
                var tile = map.GetTile(gx, gy);

                bool blocked = tile != null && tile.placedTurret != null && !tile.passableOverride;
                if (blocked)
                {
                    if (_bouncesLeft <= 0) { Destroy(gameObject); return; }
                    _bouncesLeft--;

                    // 충돌 면 추정 (가로/세로 중 어느 쪽 벽인지)
                    Vector3 tileWorld = map.GridToWorld(gx, gy);
                    float dx = Mathf.Abs(next.x - tileWorld.x);
                    float dy = Mathf.Abs(next.y - tileWorld.y);
                    if (dx < dy)
                        _dir = new Vector2(-_dir.x,  _dir.y);  // 좌우 벽 반사
                    else
                        _dir = new Vector2( _dir.x, -_dir.y);  // 상하 벽 반사

                    next = pos + _dir * _speed * Time.deltaTime;
                }
            }

            transform.position = next;

            // 몬스터 히트 (스냅샷으로 컬렉션 수정 방지)
            var monsters = MonsterManager.Instance?.ActiveMonsters;
            if (monsters == null) return;
            var snap = new System.Collections.Generic.List<Monster>(monsters);
            foreach (var m in snap)
            {
                if (m == null || !m.IsAlive || _hit.Contains(m)) continue;
                if (Vector2.Distance(transform.position, m.transform.position) < 0.4f)
                {
                    _hit.Add(m);
                    m.TakeDamage(_damage, _isCrit);
                }
            }
        }
    }
}
