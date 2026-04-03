using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class SpikeTrap : TurretBase
    {
        public override bool IsPassable => true;

        private List<Transform> _spikes = new List<Transform>();
        private static readonly Vector3 SpikeHide = new Vector3(0f, -0.55f, 0f);
        private static readonly Vector3 SpikeShow = new Vector3(0f,  0.05f, 0f);

        protected override void Awake()
        {
            turretType = TurretType.SpikeTrap;
            if (statData == null) { damage = 18f; range = 1.1f; fireRate = 0.8f; hp = 150f; }
            base.Awake();

            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = SLayer.Tile + 1;

            // 프리팹에 Spike 자식이 없으면 자동 생성
            var existing = GetComponentsInChildren<Transform>();
            bool hasSpikes = false;
            foreach (var t in existing)
                if (t.name.StartsWith("Spike")) { _spikes.Add(t); hasSpikes = true; }

            if (!hasSpikes) BuildSpikes();
        }

        private void BuildSpikes()
        {
            //float[] xPos = { -0.35f, -0.12f, 0.12f, 0.35f };
            //foreach (var xOff in xPos)
            //{
            //    var spike = new GameObject("Spike");
            //    spike.transform.SetParent(transform);
            //    spike.transform.localPosition = SpikeHide + new Vector3(xOff, 0f, 0f);
            //    spike.transform.localScale    = new Vector3(0.045f, 0.3f, 1f);
            //    var sr = spike.AddComponent<SpriteRenderer>();
            //    sr.sprite       = GameSetup.WhiteSquareStatic();
            //    sr.color        = new Color(0.75f, 0.75f, 0.8f);
            //    sr.sortingOrder = SLayer.TrapEffect;
            //    _spikes.Add(spike.transform);
            //}
        }

protected override void OnTick()
        {
            // TurretBase.Update에서 호출되지만 실제 판정은 Update에서 직접 처리
            StartCoroutine(SpikeRoutine());
        }

protected override void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return;

            // 타일 위에 먼스터가 없으면 쾼다운을 다시 조절하지 않음
            // 타일에 먼스터가 진입하는 순간 바로 발동 가능
            if (!HasMonsterOnTiles()) return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            StartCoroutine(SpikeRoutine());
            _cooldown = 1f / Mathf.Max(fireRate, 0.01f);
        }


        /// <summary>점유 타일 위에 먼스터가 있는지 매 프레임 체크</summary>
private bool HasMonsterOnTiles()
        {
            float step     = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            float halfTile = step * 0.5f;
            foreach (var m in MonsterManager.Instance.ActiveMonsters)
            {
                if (m == null || !m.IsAlive) continue;
                foreach (var tile in occupiedTiles)
                {
                    if (tile == null) continue;
                    if (SweepCheck(m.LastPosition, m.transform.position,
                                   tile.transform.position, halfTile))
                        return true;
                }
            }
            return false;
        }

        /// <summary>prev→cur 선분이 tileCenter 중심 halfSize AABB와 교차하는지</summary>
        private bool SweepCheck(Vector2 prev, Vector2 cur, Vector2 tileCenter, float half)
        {
            // 1차: 현재 위치 AABB 체크
            if (Mathf.Abs(cur.x - tileCenter.x) <= half &&
                Mathf.Abs(cur.y - tileCenter.y) <= half)
                return true;

            // 2차: 이전 위치 AABB 체크
            if (Mathf.Abs(prev.x - tileCenter.x) <= half &&
                Mathf.Abs(prev.y - tileCenter.y) <= half)
                return true;

            // 3차: 선분-AABB 스윗 교차 체크
            // AABB를 (열린 구간)으로 보고 선분이 안에 들어오는 t 찾기
            float dx = cur.x - prev.x;
            float dy = cur.y - prev.y;
            if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return false;

            float txMin = (tileCenter.x - half - prev.x) / dx;
            float txMax = (tileCenter.x + half - prev.x) / dx;
            float tyMin = (tileCenter.y - half - prev.y) / dy;
            float tyMax = (tileCenter.y + half - prev.y) / dy;

            if (Mathf.Abs(dx) < 0.0001f) { txMin = float.NegativeInfinity; txMax = float.PositiveInfinity; }
            if (Mathf.Abs(dy) < 0.0001f) { tyMin = float.NegativeInfinity; tyMax = float.PositiveInfinity; }

            if (txMin > txMax) { float tmp = txMin; txMin = txMax; txMax = tmp; }
            if (tyMin > tyMax) { float tmp = tyMin; tyMin = tyMax; tyMax = tmp; }

            float tEnter = Mathf.Max(txMin, tyMin);
            float tExit  = Mathf.Min(txMax, tyMax);

            return tEnter <= tExit && tExit >= 0f && tEnter <= 1f;
        }

private IEnumerator SpikeRoutine()
        {
            float dur = 0.12f, t = 0f;
            while (t < dur)
            {
                float ratio = t / dur;
                foreach (var s in _spikes)
                {
                    var lp = s.localPosition; lp.y = Mathf.Lerp(SpikeHide.y, SpikeShow.y, ratio);
                    s.localPosition = lp;
                }
                t += Time.deltaTime; yield return null;
            }

            float dmg      = RollDamage(out bool isCrit);
            float step     = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            float halfTile = step * 0.5f;

            var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                bool hit = false;
                foreach (var tile in occupiedTiles)
                {
                    if (tile == null) continue;
                    // 스윗 판정: 이전프레임→현재 선분이 타일 AABB 통과시도 적중
                    if (SweepCheck(m.LastPosition, m.transform.position,
                                   tile.transform.position, halfTile))
                    { hit = true; break; }
                }
                if (hit) m.TakeDamage(dmg, isCrit);
            }

            yield return new WaitForSeconds(0.18f);

            t = 0f;
            while (t < dur)
            {
                float ratio = t / dur;
                foreach (var s in _spikes)
                {
                    var lp = s.localPosition; lp.y = Mathf.Lerp(SpikeShow.y, SpikeHide.y, ratio);
                    s.localPosition = lp;
                }
                t += Time.deltaTime; yield return null;
            }
            foreach (var s in _spikes) { var lp = s.localPosition; lp.y = SpikeHide.y; s.localPosition = lp; }
        }
    }
}
