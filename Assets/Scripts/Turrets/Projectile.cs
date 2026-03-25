using UnityEngine;

namespace Underdark
{
    public class Projectile : MonoBehaviour
    {
        private Monster _target;
        private float   _damage;
        private float   _speed = 9f;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color        = new Color(1f, 1f, 0.2f);
                sr.sortingOrder = SLayer.Projectile;
            }
        }

        public void Init(Monster target, float damage)
        {
            _target = target;
            _damage = damage;
        }

        private void Update()
        {
            if (_target == null || !_target.IsAlive) { Destroy(gameObject); return; }
            Vector3 dir = (_target.transform.position - transform.position).normalized;
            transform.position += dir * _speed * Time.deltaTime;
            if (Vector2.Distance(transform.position, _target.transform.position) < 0.15f)
            {
                _target.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
