using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    // ── 폭발 이펙트 컴포넌트 ─────────────────────────────────────────
    public class ExplosionEffect : MonoBehaviour
    {
        [Tooltip("폭발 색상 (일반)")]
        public Color normalColor = new Color(1f, 0.5f, 0.1f, 0.85f);
        [Tooltip("폭발 색상 (크리티컬)")]
        public Color critColor   = new Color(1f, 0.95f, 0.1f, 0.95f);
        [Tooltip("확장 시간")]
        public float expandTime  = 0.15f;
        [Tooltip("페이드 시간")]
        public float fadeTime    = 0.2f;

        private SpriteRenderer _sr;

        

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = SLayer.Effect;
            if (_sr.sprite == null)
                _sr.sprite = GameSetup.WhiteSquareStatic();
        }

        public void Play(float blastRadius, bool isCrit)
        {
            _sr.color = isCrit ? critColor : normalColor;
            StartCoroutine(PlayRoutine(blastRadius, isCrit));
        }

        private IEnumerator PlayRoutine(float blastRadius, bool isCrit)
        {
            float targetScale = blastRadius * 2f;
            float t = 0f;

            // 확장
            while (t < expandTime)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.one * Mathf.Lerp(0f, targetScale, t / expandTime);
                yield return null;
            }
            transform.localScale = Vector3.one * targetScale;

            // 페이드아웃
            Color col = _sr.color;
            t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                _sr.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0f, t / fadeTime));
                yield return null;
            }

            Destroy(gameObject);
        }
    }

    // ── 포탄 프로젝타일 ───────────────────────────────────────────────
    public class ExplosiveProjectile : MonoBehaviour
    {
        [Tooltip("폭발 이펙트 프리팹")]
        public GameObject explosionPrefab;

        private Monster _target;
        private float   _damage;
        private float   _blastRadius;
        private bool    _isCrit;
        private float   _speed = 8f;
        private bool    _exploded;
        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = SLayer.Projectile;
            if (_sr.sprite == null)
            {
                _sr.sprite = GameSetup.WhiteSquareStatic();
                _sr.color  = new Color(1f, 0.55f, 0.1f);
                transform.localScale = Vector3.one * 0.32f;
            }
        }

        public void Init(Monster target, float damage, float blastRadius, bool isCrit, GameObject expPrefab = null)
        {
            _target      = target;
            _damage      = damage;
            _blastRadius = blastRadius;
            _isCrit      = isCrit;
            if (expPrefab != null) explosionPrefab = expPrefab;
            if (isCrit && _sr != null) _sr.color = new Color(1f, 0.95f, 0.1f);
        }

        private void Update()
        {
            if (_exploded) return;
            if (_target == null || !_target.IsAlive)
            {
                StartCoroutine(Explode(transform.position));
                return;
            }

            Vector3 dir = (_target.transform.position - transform.position).normalized;
            transform.position += dir * _speed * Time.deltaTime;

            // 이동 방향으로 회전
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (Vector2.Distance(transform.position, _target.transform.position) < 0.25f)
                StartCoroutine(Explode(transform.position));
        }

        private IEnumerator Explode(Vector3 pos)
        {
            _exploded = true;
            enabled   = false;
            if (_sr != null) _sr.enabled = false;

            // 범위 데미지
            var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                if (Vector2.Distance(pos, m.transform.position) <= _blastRadius)
                    m.TakeDamage(_damage, _isCrit);
            }

            // 폭발 이펙트 생성
            GameObject fxGo;
            if (explosionPrefab != null)
                fxGo = Instantiate(explosionPrefab, pos, Quaternion.identity);
            else
            {
                fxGo = new GameObject("Explosion");
                fxGo.transform.position = pos;
            }

            var effect = fxGo.GetComponent<ExplosionEffect>();
            if (effect == null) effect = fxGo.AddComponent<ExplosionEffect>();
            effect.Play(_blastRadius, _isCrit);

            yield return new WaitForSeconds(0.05f);
            Destroy(gameObject);
        }
    }

    // ── 폭발 포탑 ─────────────────────────────────────────────────────
    public class ExplosiveCannon : TurretBase
    {
        [Header("Explosion")]
        [Tooltip("폭발 반경")]
        public float blastRadius = 1.5f;

        [Header("프리팹 (Inspector에서 연결)")]
        [Tooltip("포탄 프리팹 (PF_ExplosiveShell) - 없으면 기본 주황 사각형")]
        public GameObject shellPrefab;
        [Tooltip("폭발 이펙트 프리팹 (PF_Explosion) - 없으면 기본 원형 이펙트")]
        public GameObject explosionPrefab;
        public Animator _ani;
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

            _ani.Rebind();

            AimBarrel(target.transform.position);
            float dmg = RollDamage(out bool isCrit);

            // 포탄 생성
            GameObject shellGo = shellPrefab != null
                ? Instantiate(shellPrefab, GetFirePosition(), Quaternion.identity)
                : new GameObject("ExplosiveShell") { transform = { position = GetFirePosition() } };

            var proj = shellGo.GetComponent<ExplosiveProjectile>()
                    ?? shellGo.AddComponent<ExplosiveProjectile>();

            proj.Init(target, dmg, blastRadius, isCrit, explosionPrefab);
        }
    }
}
