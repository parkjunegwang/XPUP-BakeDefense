using System.Collections;
using UnityEngine;

namespace Underdark
{
    public class SlowProjectile : MonoBehaviour
    {
        private Monster _target;
        private float   _damage;
        private float   _slowFactor;   // 0.4 = 60% 감속
        private float   _slowDuration;
        private float   _speed = 8f;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = GameSetup.WhiteSquareStatic();
            sr.color = new Color(0.3f, 0.7f, 1f);
            sr.sortingOrder = SLayer.Projectile;
            transform.localScale = Vector3.one * 0.25f;
        }

        public void Init(Monster target, float damage, float slowFactor, float slowDuration)
        {
            _target = target;
            _damage = damage;
            _slowFactor = slowFactor;
            _slowDuration = slowDuration;
        }

        private void Update()
        {
            if (_target == null || !_target.IsAlive) { Destroy(gameObject); return; }
            Vector3 dir = (_target.transform.position - transform.position).normalized;
            transform.position += dir * _speed * Time.deltaTime;
            if (Vector2.Distance(transform.position, _target.transform.position) < 0.15f)
            {
                _target.TakeDamage(_damage);
                _target.ApplySlow(_slowFactor, _slowDuration);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 3. 슬로우형 터렛 - 발사체가 날아가 데미지 + 슬로우
    /// </summary>
    public class SlowShooterTurret : TurretBase
    {
        [Header("Slow Settings")]
        [Tooltip("슬로우 배율 (0.4 = 속도 40%로 감소)")]
        public float slowFactor   = 0.4f;
        [Tooltip("슬로우 지속시간 (초)")]
        public float slowDuration = 2.5f;

        protected override void Awake()
        {
            turretType = TurretType.SlowShooter;
            if (statData == null) { damage = 8f; range = 3.0f; fireRate = 1.2f; hp = 70f; }
            base.Awake();
        }

protected override void OnTick() { var target = FindClosestInRange(); if (target == null) return; AimBarrel(target.transform.position); var go = new GameObject("SlowBolt"); go.transform.position = GetFirePosition(); var proj = go.AddComponent<SlowProjectile>(); proj.Init(target, damage, slowFactor, slowDuration); }
    }
}
