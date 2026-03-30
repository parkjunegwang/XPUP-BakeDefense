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
        public float bulletSize  = 1f;

protected override void OnTick()
        {
            var target = FindClosestInRange();
            if (target == null) return;

            Vector2 dir = (target.transform.position - transform.position).normalized;
            bool isCrit;
            float dmg = RollDamage(out isCrit);

            var go = new GameObject("PinballBullet");
            // 발사 위치를 터렛 중심에서 약간 앞으로 오프셋 (자신 타일에 즉시 바로 충돌나지 않도록)
            go.transform.position = (Vector3)((Vector2)transform.position + dir * 0.6f);

            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>("Image/Ball");
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (sprite == null) sprite = GameSetup.WhiteSquareStatic();

            sr.sprite       = sprite;
            sr.color        = new Color(1f, 0.85f, 0.1f);
            sr.sortingOrder = SLayer.Projectile;
            go.transform.localScale = Vector3.one * bulletSize;
            // 소유자 터렛 정보 전달 (자기 타일 충돌 제외용)
            go.AddComponent<PinballBullet>().Init(dir, bulletSpeed, dmg, isCrit, maxBounces, currentTile);
        }
    }

    public class PinballBullet : MonoBehaviour
    {
        private Vector2           _dir;
        private float             _speed;
        private float             _damage;
        private bool              _isCrit;
        private int               _bouncesLeft;
        private Tile              _ownerTile;   // 자기 타일 충돌 제외
        private HashSet<Monster>  _hit = new HashSet<Monster>();

public void Init(Vector2 dir, float speed, float damage, bool isCrit, int bounces, Tile ownerTile = null)
        {
            _dir         = dir.normalized;
            _speed       = speed;
            _damage      = damage;
            _isCrit      = isCrit;
            _bouncesLeft = bounces;
            _ownerTile   = ownerTile;
            Destroy(gameObject, 6f);
        }

private void Update()
        {
            Vector2 pos  = transform.position;
            Vector2 next = pos + _dir * _speed * Time.deltaTime;

            var map = MapManager.Instance;
            if (map != null)
            {
                float step    = map.tileSize + map.tileGap;
                float offsetX = -(map.columns - 1) * step * 0.5f;
                float offsetY = -(map.rows    - 1) * step * 0.5f;
                int gx = Mathf.RoundToInt((next.x - offsetX) / step);
                int gy = Mathf.RoundToInt((next.y - offsetY) / step);
                var tile = map.GetTile(gx, gy);

                // 자기 터렛이 놓인 타일은 충돌 제외
                bool blocked = tile != null && tile.placedTurret != null
                    && !tile.passableOverride
                    && tile != _ownerTile;
                if (blocked)
                {
                    if (_bouncesLeft <= 0) { Destroy(gameObject); return; }
                    _bouncesLeft--;
                    Vector3 tileWorld = map.GridToWorld(gx, gy);
                    float dx = Mathf.Abs(next.x - tileWorld.x);
                    float dy = Mathf.Abs(next.y - tileWorld.y);
                    if (dx < dy) _dir = new Vector2(-_dir.x,  _dir.y);
                    else         _dir = new Vector2( _dir.x, -_dir.y);
                    next = pos + _dir * _speed * Time.deltaTime;
                }
            }

            transform.position = next;

            // 몬스터 히트 체크 - 히트 반경 키움
            var monsters = MonsterManager.Instance?.ActiveMonsters;
            if (monsters == null) return;
            var snap = new System.Collections.Generic.List<Monster>(monsters);
            foreach (var m in snap)
            {
                if (m == null || !m.IsAlive || _hit.Contains(m)) continue;
                float dist = Vector2.Distance(transform.position, m.transform.position);
                if (dist < 0.55f)  // 히트 반경 0.4 → 0.55으로 확대
                {
                    _hit.Add(m);
                    m.TakeDamage(_damage, _isCrit);
                    Debug.Log($"[Pinball] HIT {m.name} dist={dist:F2} dmg={_damage}");
                }
            }
        }
    }
}
