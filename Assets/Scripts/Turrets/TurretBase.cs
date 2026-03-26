using UnityEngine;
using System.Collections.Generic;

namespace Underdark
{
    public abstract class TurretBase : MonoBehaviour
    {
        // ── 기본 필드 ──────────────────────────────────────────────────
        public TurretType turretType;
        public int        level = 1;

        [Header("Stat Data (ScriptableObject)")]
        public TurretStatData statData;

        public float damage;
        public float range;
        public float fireRate;
        public float hp;

        [Header("Visual - Body (방향 고정)")]
        public SpriteRenderer bodyRenderer;

        [Header("Fire Point")]
        [Tooltip("발사 기준점. Barrel 자식에 FirePoint 오브젝트 두면 자동 탐색.")]
        public Transform firePoint;

        [Header("Visual - Barrel (좌우 방향전환)")]
        [Tooltip("포신 SpriteRenderer. 공격 방향에 따라 flipX 적용.")]
        public SpriteRenderer barrelRenderer;
        [Tooltip("포신이 기본으로 오른쪽을 바라보면 false, 왼쪽이면 true")]
        public bool barrelDefaultFacingLeft = false;

        [Header("Runtime")]
        [Tooltip("MapData에서 불러온 벽이면 true - 이동/삭제 불가")]
        public bool isFromMapData = false;
        public Tile       currentTile;
        public List<Tile> occupiedTiles = new List<Tile>();

        public virtual bool IsPassable => false;

        protected float _cooldown;

        private SpriteRenderer[] _cachedSrs;
        private int[]            _srBaseOrders;

        // ── 초기화 ────────────────────────────────────────────────────
        protected virtual void Awake()
        {
            CacheSrOrders();

            if (bodyRenderer == null)
            {
                var bodyTf = transform.Find("Body");
                bodyRenderer = bodyTf != null
                    ? bodyTf.GetComponent<SpriteRenderer>()
                    : GetComponent<SpriteRenderer>();
            }

            if (barrelRenderer == null)
            {
                var barrelTf = transform.Find("Barrel");
                if (barrelTf != null) barrelRenderer = barrelTf.GetComponent<SpriteRenderer>();
            }

            if (firePoint == null)
            {
                var fp = FindDeepChild(transform, "FirePoint");
                if (fp != null) firePoint = fp;
            }

            ApplyStatsFromData(level);
            ApplyLevelSprites(level);
            UpdateSortingOrder();
        }

        private void CacheSrOrders()
        {
            _cachedSrs      = GetComponentsInChildren<SpriteRenderer>(true);
            _srBaseOrders   = new int[_cachedSrs.Length];
            for (int i = 0; i < _cachedSrs.Length; i++)
                _srBaseOrders[i] = _cachedSrs[i].sortingOrder;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return child;
                var found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ── 스탯 적용 ─────────────────────────────────────────────────
        public void ApplyStatsFromData(int lv)
        {
            if (statData == null) return;
            var s    = statData.GetLevel(lv);
            damage   = s.damage;
            range    = s.range;
            fireRate = s.fireRate;
            hp       = s.hp;
        }

        /// <summary>
        /// statData의 해당 레벨에 Body/Barrel 스프라이트가 지정돼 있으면 교체.
        /// 비어있으면 현재 스프라이트 유지.
        /// </summary>
        public void ApplyLevelSprites(int lv)
        {
            if (statData == null) return;
            var s = statData.GetLevel(lv);
            if (s.bodySprite   != null && bodyRenderer   != null) bodyRenderer.sprite   = s.bodySprite;
            if (s.barrelSprite != null && barrelRenderer != null) barrelRenderer.sprite = s.barrelSprite;
        }

        // ── 비주얼 헬퍼 ───────────────────────────────────────────────
        public Vector3 GetFirePosition() => firePoint != null ? firePoint.position : transform.position;

        public void AimBarrel(Vector3 targetPos)
        {
            if (barrelRenderer == null) return;
            bool targetIsLeft = targetPos.x < transform.position.x;
            barrelRenderer.flipX = barrelDefaultFacingLeft ? !targetIsLeft : targetIsLeft;
        }

        public void UpdateSortingOrder()
        {
            if (_srBaseOrders == null) CacheSrOrders();
            int baseOrder = Mathf.RoundToInt(500f - transform.position.y * 10f);
            for (int i = 0; i < _cachedSrs.Length; i++)
            {
                if (_cachedSrs[i] == null) continue;
                _cachedSrs[i].sortingOrder = baseOrder + _srBaseOrders[i];
            }
        }

        // ── 업데이트 ──────────────────────────────────────────────────
        protected virtual void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return;
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;
            OnTick();
            _cooldown = 1f / Mathf.Max(fireRate, 0.01f);
        }

        protected abstract void OnTick();

        // ── 타겟 탐색 ─────────────────────────────────────────────────
        protected Monster FindClosestInRange()
        {
            Monster best = null;
            float   minD = float.MaxValue;
            var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                float d = Vector2.Distance(transform.position, m.transform.position);
                if (d <= range && d < minD) { minD = d; best = m; }
            }
            return best;
        }

        protected List<Monster> FindAllInRange()
        {
            var result   = new List<Monster>();
            var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                if (Vector2.Distance(transform.position, m.transform.position) <= range)
                    result.Add(m);
            }
            return result;
        }

        // ── 레벨업 ────────────────────────────────────────────────────
        public void LevelUp()
        {
            level++;

            if (statData != null)
            {
                ApplyStatsFromData(level);
                ApplyLevelSprites(level);   // ← 스프라이트 교체
            }
            else
            {
                damage   *= 1.5f;
                range    *= 1.1f;
                fireRate *= 1.2f;
                hp       *= 1.5f;
            }

            bool isAutoScale = statData != null && statData.autoScale;
            if (isAutoScale)
            {
                float growRatio = 1.06f;
                transform.localScale = new Vector3(
                    transform.localScale.x * growRatio,
                    transform.localScale.y * growRatio,
                    transform.localScale.z);
            }

            if (bodyRenderer != null)
                bodyRenderer.color = Color.Lerp(bodyRenderer.color, Color.white, 0.2f);

            OnLevelUp();
        }

        protected virtual void OnLevelUp() { }
    }
}
