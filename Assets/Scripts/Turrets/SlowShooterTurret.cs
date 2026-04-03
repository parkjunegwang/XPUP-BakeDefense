using System.Collections;
using UnityEngine;

namespace Underdark
{
    public class SlowProjectile : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Monster _target;
        private float   _damage;
        private float   _slowFactor;   // 0.4 = 60% 감속
        private float   _slowDuration;
        private bool    _isCrit;
        private float   _speed = 8f;

private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = SLayer.Projectile;

            // 스프라이트가 없으면 기본 주황 사각형
            if (_sr.sprite == null)
            {
                _sr.sprite = GameSetup.WhiteSquareStatic();
                _sr.color  = new Color(0.3f, 0.7f, 1f);
                transform.localScale = Vector3.one * 0.25f;
            }
        }

public void Init(Monster target, float damage, float slowFactor, float slowDuration,
                         bool isCrit = false, Sprite sprite = null, float size = 0.25f)
        {
            _target      = target;
            _damage      = damage;
            _slowFactor  = slowFactor;
            _slowDuration = slowDuration;
            _isCrit      = isCrit;

            if (sprite != null)
            {
                _sr.sprite = sprite;
                _sr.color  = isCrit ? new Color(1f, 0.95f, 0.3f) : Color.white;
            }
            else if (isCrit)
            {
                _sr.color = new Color(0.5f, 0.9f, 1f); // 얼음 하는 스카이블루 크릿
            }

            transform.localScale = Vector3.one * size;
        }

private void Update() { if (_target == null || !_target.IsAlive) { Destroy(gameObject); return; } Vector3 dir = (_target.transform.position - transform.position).normalized; transform.position += dir * _speed * Time.deltaTime; if (Vector2.Distance(transform.position, _target.transform.position) < 0.15f) { _target.TakeDamage(_damage, _isCrit); _target.ApplySlow(_slowFactor, _slowDuration); Destroy(gameObject); } }
    }

    /// <summary>
    /// 3. 슬로우형 터렛 - 발사체가 날아가 데미지 + 슬로우
    /// </summary>
    public class SlowShooterTurret : TurretBase
    {
        [Header("Projectile")]
        [Tooltip("발사체 프리팩 (없으면 빈 GameObject)")]
        public GameObject projectilePrefab;
        [Tooltip("발사체 스프라이트 (없으면 기본 파란 사각형)")]
        public Sprite     projectileSprite;
        [Tooltip("발사체 크기")]
        public float      projectileSize = 0.25f;

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

protected override void OnTick()
        {
            var target = FindClosestInRange();
            if (target == null) return;
            AimBarrel(target.transform.position);
            float dmg = RollDamage(out bool isCrit);

            // 프리팩 우선, 없으면 빈 GameObject
            GameObject go = projectilePrefab != null
                ? Instantiate(projectilePrefab, GetFirePosition(), Quaternion.identity)
                : new GameObject("SlowBolt") { transform = { position = GetFirePosition() } };

            var proj = go.GetComponent<SlowProjectile>() ?? go.AddComponent<SlowProjectile>();
            proj.Init(target, dmg, slowFactor, slowDuration, isCrit, projectileSprite, projectileSize);
        }
    }
}
