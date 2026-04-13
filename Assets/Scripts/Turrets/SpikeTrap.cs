using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class SpikeTrap : TurretBase
    {
        public override bool IsPassable => true;

        // ── Barrel 튀어오르기 설정 ─────────────────────────────────────
        // Barrel 로컬 Y 기준 위치 (Awake에서 캡처)
        private Vector3 _barrelRestLocalPos;
        // Barrel이 올라갈 최고 높이 (로컬 Y 오프셋)
        private const float BarrelLaunchOffsetY = 0.55f;
        // 올라가는 시간 / 내려오는 시간
        private const float RiseTime  = 0.10f;
        private const float FallTime  = 0.14f;
        // 데미지 판정 최고점 유지 시간
        private const float PeakHold  = 0.04f;

        // ── (레거시) Spike 자식 오브젝트 ──────────────────────────────
        private List<Transform> _spikes = new List<Transform>();

        protected override void Awake()
        {
            turretType = TurretType.SpikeTrap;
            if (statData == null) { damage = 18f; range = 1.1f; fireRate = 0.8f; hp = 150f; }
            base.Awake();

            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = SLayer.Tile + 1;

            // Barrel 기본 위치 저장
            if (barrelRenderer != null)
                _barrelRestLocalPos = barrelRenderer.transform.localPosition;

            // 프리팹에 Spike 자식이 있으면 수집 (없어도 무방)
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t != transform && t.name.StartsWith("Spike"))
                    _spikes.Add(t);
        }

        protected override void OnTick()
        {
            // SpikeTrap은 Update()에서 직접 트리거 → 여기서는 아무것도 안 함
        }

        protected override void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return;

            // 타일 위에 몬스터가 있을 때만 쿨다운 진행
            if (!HasMonsterOnTiles()) return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            StartCoroutine(BarrelLaunchRoutine());
            _cooldown = 1f / Mathf.Max(fireRate, 0.01f);
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>Barrel이 위로 솟구쳤다가 내려오며 데미지를 줌</summary>
        // ─────────────────────────────────────────────────────────────
        private IEnumerator BarrelLaunchRoutine()
        {
            Transform barrelTf = barrelRenderer != null ? barrelRenderer.transform : null;
            Vector3   restPos  = _barrelRestLocalPos;
            Vector3   peakPos  = restPos + new Vector3(0f, BarrelLaunchOffsetY, 0f);

            // ① 빠르게 위로 솟구침 ──────────────────────────────────
            float t = 0f;
            while (t < RiseTime)
            {
                float ratio = t / RiseTime;
                // EaseOut: 처음에 빠르고 끝에 느려지게
                float eased = 1f - (1f - ratio) * (1f - ratio);
                if (barrelTf != null)
                    barrelTf.localPosition = Vector3.LerpUnclamped(restPos, peakPos, eased);
                // 레거시 Spike도 함께 올리기
                foreach (var s in _spikes)
                {
                    var lp = s.localPosition;
                    lp.y = Mathf.Lerp(-0.55f, 0.05f, eased);
                    s.localPosition = lp;
                }
                t += Time.deltaTime;
                yield return null;
            }

            // 최고점 고정
            if (barrelTf != null) barrelTf.localPosition = peakPos;

            // ② 최고점에서 데미지 판정 ──────────────────────────────
            DealDamageToTileMonsters();

            yield return new WaitForSeconds(PeakHold);

            // ③ 천천히 내려옴 ───────────────────────────────────────
            t = 0f;
            while (t < FallTime)
            {
                float ratio = t / FallTime;
                // EaseIn: 처음에 느리고 끝에 빠르게 (중력감)
                float eased = ratio * ratio;
                if (barrelTf != null)
                    barrelTf.localPosition = Vector3.LerpUnclamped(peakPos, restPos, eased);
                // 레거시 Spike도 함께 내리기
                foreach (var s in _spikes)
                {
                    var lp = s.localPosition;
                    lp.y = Mathf.Lerp(0.05f, -0.55f, eased);
                    s.localPosition = lp;
                }
                t += Time.deltaTime;
                yield return null;
            }

            // 원점 정확히 복귀
            if (barrelTf != null) barrelTf.localPosition = restPos;
            foreach (var s in _spikes)
            {
                var lp = s.localPosition; lp.y = -0.55f; s.localPosition = lp;
            }
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>점유 타일 위 몬스터에게 데미지 적용</summary>
        // ─────────────────────────────────────────────────────────────
        private void DealDamageToTileMonsters()
        {
            float dmg      = RollDamage(out bool isCrit);
            float step     = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            float halfTile = step * 0.6f;

            var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                foreach (var tile in occupiedTiles)
                {
                    if (tile == null) continue;
                    Vector2 tp = tile.transform.position;
                    Vector2 mp = m.transform.position;
                    if (Mathf.Abs(mp.x - tp.x) <= halfTile &&
                        Mathf.Abs(mp.y - tp.y) <= halfTile)
                    {
                        m.TakeDamage(dmg, isCrit);
                        break; // 같은 몬스터 중복 피해 방지
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>점유 타일 위에 살아있는 몬스터가 있는지 체크</summary>
        // ─────────────────────────────────────────────────────────────
        private bool HasMonsterOnTiles()
        {
            float step     = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            float halfTile = step * 0.6f;
            foreach (var m in MonsterManager.Instance.ActiveMonsters)
            {
                if (m == null || !m.IsAlive) continue;
                Vector2 mp = m.transform.position;
                foreach (var tile in occupiedTiles)
                {
                    if (tile == null) continue;
                    Vector2 tp = tile.transform.position;
                    if (Mathf.Abs(mp.x - tp.x) <= halfTile &&
                        Mathf.Abs(mp.y - tp.y) <= halfTile)
                        return true;
                }
            }
            return false;
        }
    }
}
