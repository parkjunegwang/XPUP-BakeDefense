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
            StartCoroutine(SpikeRoutine());
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

            float dmg = RollDamage(out bool isCrit);
            float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            float halfTile = step * 0.5f;

            var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                // 점유 타일 위에 있는 몸스터만 공격 (AABB 판정)
                bool onTile = false;
                foreach (var tile in occupiedTiles)
                {
                    if (tile == null) continue;
                    Vector2 tilePos = tile.transform.position;
                    Vector2 monPos  = m.transform.position;
                    if (Mathf.Abs(monPos.x - tilePos.x) <= halfTile &&
                        Mathf.Abs(monPos.y - tilePos.y) <= halfTile)
                    {
                        onTile = true;
                        break;
                    }
                }
                if (onTile) m.TakeDamage(dmg, isCrit);
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
