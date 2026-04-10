using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 부메랑을 던져 앞으로 날아갔다 되돌아옴.
    /// 관통하며 맞는 순서대로 데미지 감쇄 (첫 타 100% → 매 타 damageDecay씩 감소).
    /// 왕복 2번 때리며, 복귀 시에도 독립적으로 다시 감쇄 적용.
    /// </summary>
    public class BoomerangTurret : TurretBase
    {
        [Header("Boomerang Settings")]
        public float throwRange     = 4f;
        public float boomerangSpeed = 7f;
        public float damageDecay    = 0.1f;
        public float minDamageMult  = 0.3f;

        protected override void OnTick()
        {
            var target = FindClosestInRange();
            if (target == null) return;
            Throw(target);
        }

private void Throw(Monster target)
        {
            Vector2 dir  = (target.transform.position - transform.position).normalized;
            Vector3 dest = transform.position + (Vector3)(dir * throwRange);

            var go = new GameObject("Boomerang");
            go.transform.position = transform.position;
            go.transform.rotation = Quaternion.Euler(0, 0, 45f); // 마름모 모양
            var sr = go.AddComponent<SpriteRenderer>();

            // 투사체 스프라이트 - 1x1 이미지 로드
            var sprite = Resources.Load<Sprite>("Image/BoomerangTurret_Ball");
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (sprite == null) sprite = GameSetup.WhiteSquareStatic();

            sr.sprite       = sprite;
            //sr.color        = new Color(0.4f, 0.9f, 1f); // 하늘색 부메랑
            sr.sortingOrder = SLayer.Projectile;
            go.transform.localScale = Vector3.one;

            bool isCrit;
            float baseDmg = RollDamage(out isCrit);
            go.AddComponent<BoomerangProjectile>().Init(
                transform.position, dest, boomerangSpeed,
                baseDmg, isCrit, damageDecay, minDamageMult);
        }
    }

    public class BoomerangProjectile : MonoBehaviour
    {
        private Vector3 _origin;
        private Vector3 _dest;
        private float   _speed;
        private float   _baseDmg;
        private bool    _isCrit;
        private float   _decay;
        private float   _minMult;

        private bool  _returning;
        private float _damageMult = 1f;
        private HashSet<Monster> _hitOut  = new HashSet<Monster>();
        private HashSet<Monster> _hitBack = new HashSet<Monster>();

        public void Init(Vector3 origin, Vector3 dest, float speed,
                         float baseDmg, bool isCrit, float decay, float minMult)
        {
            _origin  = origin;
            _dest    = dest;
            _speed   = speed;
            _baseDmg = baseDmg;
            _isCrit  = isCrit;
            _decay   = decay;
            _minMult = minMult;
            Destroy(gameObject, 5f);
        }

private void Update()
        {
            transform.Rotate(0, 0, 600f * Time.deltaTime * (_returning ? -1f : 1f));

            Vector3 tgt = _returning ? _origin : _dest;
            transform.position = Vector3.MoveTowards(transform.position, tgt, _speed * Time.deltaTime);

            if (!_returning && Vector3.Distance(transform.position, _dest) < 0.1f)
            {
                _returning  = true;
                _damageMult = 1f;
            }
            if (_returning && Vector3.Distance(transform.position, _origin) < 0.15f)
            {
                Destroy(gameObject);
                return;
            }

            var monsters = MonsterManager.Instance?.ActiveMonsters;
            if (monsters == null) return;

            // 스냅샷으로 콜렉션 수정 방지
            var snap   = new System.Collections.Generic.List<Monster>(monsters);
            var hitSet = _returning ? _hitBack : _hitOut;
            foreach (var m in snap)
            {
                if (m == null || !m.IsAlive || hitSet.Contains(m)) continue;
                if (Vector3.Distance(transform.position, m.transform.position) > 0.45f) continue;

                hitSet.Add(m);
                float dmg = _baseDmg * _damageMult;
                m.TakeDamage(dmg, _isCrit);
                _damageMult = Mathf.Max(_minMult, _damageMult - _decay);
            }
        }
    }
}
