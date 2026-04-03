using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class TornadoProjectile : MonoBehaviour
    {
        [Tooltip("토네이도 스프라이트 (없으면 기본 하늘색 사각형)")]
        public Sprite tornadoSprite;

        private float         _damage;
        private float         _radius;
        private float         _speed;
        private List<Vector3> _waypoints = new List<Vector3>();
        private int           _wpIdx     = 0;
        private SpriteRenderer _sr;
        private float         _rotAngle;

        // 이미 맞은 몬스터 쿨다운 (같은 몬스터를 너무 자주 치지 않도록)
        private Dictionary<Monster, float> _hitCooldown = new Dictionary<Monster, float>();
        private const float HitInterval = 0.25f;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = SLayer.Effect;

            if (tornadoSprite != null)
                _sr.sprite = tornadoSprite;
            else
            {
                _sr.sprite = GameSetup.WhiteSquareStatic();
                _sr.color  = new Color(0.6f, 0.9f, 1f, 0.75f);
            }
        }

        public void Init(float damage, float radius, float speed,
                         List<Vector3> waypoints, Sprite sprite = null)
        {
            _damage    = damage;
            _radius    = radius;
            _speed     = speed;
            _waypoints = waypoints;
            _wpIdx     = 0;

            if (sprite != null)
            {
                _sr.sprite = sprite;
                _sr.color  = Color.white;
            }

            transform.localScale = Vector3.one * radius * 2f;
        }

        private void Update()
        {
            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentState != GameState.WaveInProgress)
            {
                Destroy(gameObject);
                return;
            }

            if (_waypoints == null || _wpIdx >= _waypoints.Count)
            {
                Destroy(gameObject);
                return;
            }

            // ── 경로 이동 ────────────────────────────────────────────
            Vector3 target = _waypoints[_wpIdx];
            Vector3 dir    = (target - transform.position).normalized;
            transform.position += dir * _speed * Time.deltaTime;

            // 다음 웨이포인트로
            if (Vector2.Distance(transform.position, target) < 0.2f)
            {
                _wpIdx++;
                if (_wpIdx >= _waypoints.Count)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            // ── 회전 비주얼 ──────────────────────────────────────────
            _rotAngle += 400f * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, _rotAngle);

            // ── 범위 내 몬스터 데미지 ────────────────────────────────
            float now      = Time.time;
            var monsters   = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                if (Vector2.Distance(transform.position, m.transform.position) > _radius) continue;

                // 쿨다운 체크
                if (_hitCooldown.TryGetValue(m, out float lastHit) &&
                    now - lastHit < HitInterval)
                    continue;

                m.TakeDamage(_damage);
                _hitCooldown[m] = now;
            }
        }
    }

    /// <summary>
    /// 회오리 터렛 - 경로를 따라 스폰 지점으로 날아가며 데미지
    /// </summary>
    public class TornadoTurret : TurretBase
    {
        [Header("Tornado Settings")]
        public float tornadoRadius = 0.8f;
        public float tornadoSpeed  = 4.0f;

        [Header("Sprite (Inspector에서 연결)")]
        [Tooltip("토네이도 발사체 스프라이트")]
        public Sprite tornadoSprite;

        protected override void Awake()
        {
            turretType = TurretType.Tornado;
            if (statData == null) { damage = 15f; range = 99f; fireRate = 0.4f; hp = 80f; }
            base.Awake();
        }

protected override void OnTick()
        {
            var map = MapManager.Instance;
            if (map == null || map.spawnPositions.Count == 0) return;

            // 가장 가까운 스폰 포인트 선택
            Vector2Int bestSpawn = map.spawnPositions[0];
            float minDist = float.MaxValue;
            foreach (var sp in map.spawnPositions)
            {
                float d = Vector2.Distance(transform.position, map.GridToWorld(sp.x, sp.y));
                if (d < minDist) { minDist = d; bestSpawn = sp; }
            }

            // 엔드포인트 가져오기
            if (map.endPositions == null || map.endPositions.Count == 0) return;
            var endPos = map.endPositions[0];

            // 스폰 → 엔드 경로를 역방향으로 사용 (spawn은 먹혀있지 않으므로)
            var gridPath = Pathfinder.FindPath(bestSpawn, endPos, map);
            if (gridPath == null || gridPath.Count == 0) return;

            // 역방향: 엔드에서 스폰 쪽으로 (= 타워 방향에서 스폰으로)
            gridPath.Reverse();

            // 그리드 경로 → 월드 좌표
            var waypoints = new List<Vector3>();
            foreach (var gp in gridPath)
                waypoints.Add(map.GridToWorld(gp.x, gp.y));

            // 타워 위치에서 가장 가까운 waypoint를 시작점으로
            // (waypoints[0] = spawn, 타워 위치에 가장 가까운 인덱스를 찾아서 거기서부터 시작)
            int startIdx = 0;
            float minWpDist = float.MaxValue;
            for (int i = 0; i < waypoints.Count; i++)
            {
                float d = Vector2.Distance(transform.position, waypoints[i]);
                if (d < minWpDist) { minWpDist = d; startIdx = i; }
            }
            // startIdx에서 spawn까지 진행 (spawn 방향)
            waypoints = waypoints.GetRange(startIdx, waypoints.Count - startIdx);

            if (waypoints.Count == 0) return;

            var go = new GameObject("Tornado");
            go.transform.position = transform.position;
            var proj = go.AddComponent<TornadoProjectile>();
            proj.Init(damage, tornadoRadius, tornadoSpeed, waypoints, tornadoSprite);
        }
    }
}
