using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class TornadoProjectile : MonoBehaviour
    {
        private float _damage;
        private float _radius;
        private float _speed;
        private Vector3 _target;
        private SpriteRenderer _sr;
        private float _rotAngle;
        private HashSet<Monster> _hitThisFrame = new HashSet<Monster>();

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = GameSetup.WhiteSquareStatic();
            _sr.color = new Color(0.6f, 0.9f, 1f, 0.75f);
            _sr.sortingOrder = SLayer.Effect;
        }

        public void Init(float damage, float radius, float speed, Vector3 target)
        {
            _damage = damage;
            _radius = radius;
            _speed = speed;
            _target = target;
            transform.localScale = Vector3.one * radius * 2f;
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return;

            // 스폰 방향으로 이동
            Vector3 dir = (_target - transform.position).normalized;
            transform.position += dir * _speed * Time.deltaTime;

            // 회전 비주얼
            _rotAngle += 360f * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, _rotAngle);

            // 경로상 몬스터 데미지
            _hitThisFrame.Clear();
            var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                if (Vector2.Distance(transform.position, m.transform.position) <= _radius)
                {
                    if (!_hitThisFrame.Contains(m))
                    {
                        m.TakeDamage(_damage * Time.deltaTime);
                        _hitThisFrame.Add(m);
                    }
                }
            }

            // 스폰에 도달하면 소멸
            if (Vector2.Distance(transform.position, _target) < 0.3f)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// 5. 회오리 터렛 - 회오리를 만들어 스폰 지역으로 날려보냄
    /// </summary>
    public class TornadoTurret : TurretBase
    {
        [Header("Tornado Settings")]
        public float tornadoRadius = 0.8f;
        public float tornadoSpeed  = 3.5f;

        protected override void Awake()
        {
            turretType = TurretType.Tornado;
            if (statData == null) { damage = 15f; range = 99f; fireRate = 0.4f; hp = 80f; }
            base.Awake();
        }

        protected override void OnTick()
        {
            // 가장 가까운 스폰 포인트로 회오리 발사
            var map = MapManager.Instance;
            if (map == null || map.spawnPositions.Count == 0) return;

            Vector3 spawnWorld = map.GridToWorld(
                map.spawnPositions[0].x, map.spawnPositions[0].y);

            var go = new GameObject("Tornado");
            go.transform.position = transform.position;
            var proj = go.AddComponent<TornadoProjectile>();
            proj.Init(damage, tornadoRadius, tornadoSpeed, spawnWorld);
        }
    }
}
