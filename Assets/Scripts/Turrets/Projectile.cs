using UnityEngine;

namespace Underdark
{
    public class Projectile : MonoBehaviour
    {
        private Monster        _target;
        private float          _damage;
        private float          _speed = 9f;
        private SpriteRenderer _sr;

private void Awake() { _sr = GetComponent<SpriteRenderer>(); if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>(); if (_sr.sprite == null) { _sr.sprite = GameSetup.WhiteSquareStatic(8); _sr.color = new Color(1f, 1f, 0.2f); transform.localScale = Vector3.one * 0.18f; } _sr.sortingOrder = SLayer.Projectile; }

        public void Init(Monster target, float damage)
        {
            _target = target;
            _damage = damage;
        }

private void Update() { if (_target == null || !_target.IsAlive) { Destroy(gameObject); return; } Vector3 dir = (_target.transform.position - transform.position).normalized; transform.position += dir * _speed * Time.deltaTime; if (_sr != null && Mathf.Abs(dir.x) > 0.01f) _sr.flipX = dir.x < 0; if (Vector2.Distance(transform.position, _target.transform.position) < 0.15f) { _target.TakeDamage(_damage); Destroy(gameObject); } }
    }
}
