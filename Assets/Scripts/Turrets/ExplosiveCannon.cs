using System.Collections;
using UnityEngine;

namespace Underdark
{
    public class ExplosiveProjectile : MonoBehaviour
    {
        private Monster _target;
        private float   _damage;
        private float   _blastRadius;
        private float   _speed = 6f;

        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = GameSetup.WhiteSquareStatic();
            _sr.color = new Color(1f, 0.5f, 0.1f);
            _sr.sortingOrder = SLayer.Projectile;
            transform.localScale = Vector3.one * 0.35f;
        }

        public void Init(Monster target, float damage, float blastRadius)
        {
            _target = target;
            _damage = damage;
            _blastRadius = blastRadius;
        }

        private void Update()
        {
            if (_target == null || !_target.IsAlive)
            {
                // 타겟 죽었어도 현재 위치에서 폭발
                StartCoroutine(Explode(transform.position));
                return;
            }

            Vector3 dir = (_target.transform.position - transform.position).normalized;
            transform.position += dir * _speed * Time.deltaTime;

            if (Vector2.Distance(transform.position, _target.transform.position) < 0.2f)
                StartCoroutine(Explode(transform.position));
        }

        private IEnumerator Explode(Vector3 pos)
        {
            enabled = false;

            // 범위 내 모든 몬스터 데미지
            var monsters = new System.Collections.Generic.List<Monster>(
                MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                if (Vector2.Distance(pos, m.transform.position) <= _blastRadius)
                    m.TakeDamage(_damage);
            }

            // 폭발 이펙트
            _sr.color = new Color(1f, 0.7f, 0.1f);
            transform.localScale = Vector3.one * _blastRadius * 2f;
            yield return new WaitForSeconds(0.12f);
            _sr.color = new Color(1f, 0.3f, 0f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 2. 폭발형 터렛 - 발사체가 날아가 터져서 광역 1회 데미지
    /// </summary>
    public class ExplosiveCannon : TurretBase
    {
        [Header("Explosion")]
        public float blastRadius = 1.5f;

        protected override void Awake()
        {
            turretType = TurretType.ExplosiveCannon;
            if (statData == null) { damage = 35f; range = 3.5f; fireRate = 0.5f; hp = 100f; }
            base.Awake();
        }

        protected override void OnTick()
        {
            var target = FindClosestInRange();
            if (target == null) return;

            var go = new GameObject("ExplosiveShell");
            go.transform.position = transform.position;
            var proj = go.AddComponent<ExplosiveProjectile>();
            proj.Init(target, damage, blastRadius);
        }
    }
}
