using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class SpikeTrap : TurretBase
    {
        public override bool IsPassable => true;

        // ── Barrel 튀어오르기 설정 ─────────────────────────────────────
        private Vector3 _barrelRestLocalPos;
        private const float BarrelLaunchOffsetY = 0.17f;
        private const float RiseTime  = 0.10f;
        private const float FallTime  = 0.14f;
        private const float PeakHold  = 0.04f;

        // ── (레거시) Spike 자식 오브젝트 ──────────────────────────────
        private List<Transform> _spikes = new List<Transform>();

        protected override void Awake()
        {
            turretType = TurretType.SpikeTrap;
            if (statData == null) { damage = 18f; range = 1.1f; fireRate = 0.8f; hp = 150f; }
            base.Awake(); // 내부에서 UpdateSortingOrder() 호출 → 바로 아래서 덮어씀

            // Barrel 기본 위치 저장
            if (barrelRenderer != null)
                _barrelRestLocalPos = barrelRenderer.transform.localPosition;

            // ── SpikeTrap은 바닥 함정: 모든 SR을 타일 레벨로 고정 ──
            ForceTrapSortingOrder();

            // 프리팹에 Spike 자식이 있으면 수집
            foreach (var t in GetComponentsInChildren<Transform>())
                if (t != transform && t.name.StartsWith("Spike"))
                    _spikes.Add(t);
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// SpikeTrap 전용 소팅 고정.
        /// TurretBase.UpdateSortingOrder()의 Y소팅(500 기준)을 사용하지 않고
        /// 타일 바로 위에 고정 → 몬스터(100기준 ~600)가 항상 위에 렌더됨.
        /// </summary>
        // ─────────────────────────────────────────────────────────────
        private void ForceTrapSortingOrder()
        {
            // Body: Tile(0) + 1 = 1
            //if (bodyRenderer != null)
            //    bodyRenderer.sortingOrder = SLayer.Tile + 1;

            //// Barrel: 튀어오를 때 body 위에 보이도록 +2
            //if (barrelRenderer != null)
            //    barrelRenderer.sortingOrder = SLayer.Tile + 2;

            //// 나머지 자식 SR → TrapEffect(1) 고정
            //foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            //{
            //    if (sr == bodyRenderer || sr == barrelRenderer) continue;
            //    sr.sortingOrder = SLayer.TrapEffect;
            //}
        }

        // TurretManager 등 외부에서 UpdateSortingOrder()를 호출해도 트랩 레이어 유지
        public override void UpdateSortingOrder() => ForceTrapSortingOrder();

        // ─────────────────────────────────────────────────────────────
        protected override void OnTick()
        {
            // Update()에서 직접 트리거 → 여기는 사용 안 함
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

            // ① 빠르게 위로 솟구침 (EaseOut)
            float t = 0f;
            while (t < RiseTime)
            {
                float ratio = t / RiseTime;
                float eased = 1f - (1f - ratio) * (1f - ratio);
                if (barrelTf != null)
                    barrelTf.localPosition = Vector3.LerpUnclamped(restPos, peakPos, eased);
                foreach (var s in _spikes)
                {
                    var lp = s.localPosition;
                    lp.y = Mathf.Lerp(-0.55f, 0.05f, eased);
                    s.localPosition = lp;
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (barrelTf != null) barrelTf.localPosition = peakPos;

            // ② 최고점에서 데미지 판정
            DealDamageToTileMonsters();
            yield return new WaitForSeconds(PeakHold);

            // ③ 천천히 내려옴 (EaseIn — 중력감)
            t = 0f;
            while (t < FallTime)
            {
                float ratio = t / FallTime;
                float eased = ratio * ratio;
                if (barrelTf != null)
                    barrelTf.localPosition = Vector3.LerpUnclamped(peakPos, restPos, eased);
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
                        break;
                    }
                }
            }
        }

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
