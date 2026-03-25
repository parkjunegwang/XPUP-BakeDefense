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
        public TurretStatData statData; // 프리팹에서 Inspector로 할당

        // 런타임 스탯 (statData에서 로드됨)
        public float damage;
        public float range;
        public float fireRate;
        public float hp;

        [Header("Visual - Body (방향 고정)")]
        public SpriteRenderer bodyRenderer;   // 몸체 - 항상 고정

        [Header("Visual - Barrel (좌우 방향전환)")]
        [Tooltip("포신 SpriteRenderer. 공격 방향에 따라 flipX 적용.")]
        public SpriteRenderer barrelRenderer;  // 포신 - 좌우 flip만
        [Tooltip("포신이 기본으로 오른쪽을 바라보면 false, 왼쪽이면 true")]
        public bool barrelDefaultFacingLeft = false;

        [Header("Runtime")]
        public Tile       currentTile;
        public List<Tile> occupiedTiles = new List<Tile>();

        public virtual bool IsPassable => false;

        protected float _cooldown;

        // ── 스탯 초기화 ────────────────────────────────────────────────
protected virtual void Awake() { ApplyStatsFromData(level); UpdateSortingOrder(); } public void AimBarrel(Vector3 targetPos) { if (barrelRenderer == null) return; bool targetIsLeft = targetPos.x < transform.position.x; barrelRenderer.flipX = barrelDefaultFacingLeft ? !targetIsLeft : targetIsLeft; } public void UpdateSortingOrder() { var allSr = GetComponentsInChildren<SpriteRenderer>(true); int baseOrder = Mathf.RoundToInt(500f - transform.position.y * 10f); foreach (var sr in allSr) sr.sortingOrder = baseOrder; }

        /// <summary>statData에서 현재 레벨 스탯을 적용</summary>
        public void ApplyStatsFromData(int lv)
        {
            if (statData == null) return;
            var s = statData.GetLevel(lv);
            damage   = s.damage;
            range    = s.range;
            fireRate = s.fireRate;
            hp       = s.hp;
        }

        // ── 업데이트 ───────────────────────────────────────────────────
        protected virtual void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return;
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;
            OnTick();
            _cooldown = 1f / Mathf.Max(fireRate, 0.01f);
        }

        protected abstract void OnTick();

        // ── 타겟 탐색 ──────────────────────────────────────────────────
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

        // ── 레벨업 ─────────────────────────────────────────────────────
        public void LevelUp()
        {
            level++;

            if (statData != null)
            {
                // ScriptableObject에 해당 레벨이 있으면 그 값 사용
                ApplyStatsFromData(level);
            }
            else
            {
                // fallback: 이전 방식
                damage   *= 1.5f;
                range    *= 1.1f;
                fireRate *= 1.2f;
                hp       *= 1.5f;
            }

            // AutoScale 사용 중이면 현재 scale 기준 비율 증가, 아니면 scale 유지
            bool isAutoScale = statData != null && statData.autoScale;
            if (isAutoScale)
            {
                // 현재 스케일에서 영리하게 키우기 (6%/lv)
                float growRatio = 1f + 0.06f;
                transform.localScale = new Vector3(
                    transform.localScale.x * growRatio,
                    transform.localScale.y * growRatio,
                    transform.localScale.z);
            }
            // isAutoScale=false 시: 프리팩 원본 scale 유지 (뒤스케일 안함)

            if (bodyRenderer != null)
                bodyRenderer.color = Color.Lerp(bodyRenderer.color, Color.white, 0.2f);

            OnLevelUp();
        }

        protected virtual void OnLevelUp() { }
    }
}
