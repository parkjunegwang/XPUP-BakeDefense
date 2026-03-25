using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class ElectricGate : TurretBase
    {
        public override bool IsPassable => false;

        [Header("Visual Effects")]
        public SpriteRenderer electricBeamRenderer; // 프리팹에서 할당 가능
        public SpriteRenderer[] zapLineRenderers;   // 프리팹에서 할당 가능

        private SpriteRenderer       _electricSr;
        private List<SpriteRenderer> _zapLines = new List<SpriteRenderer>();

        protected override void Awake()
        {
            turretType = TurretType.ElectricGate;
            if (statData == null) { damage = 25f; range = 0.6f; fireRate = 0.8f; hp = 120f; }
            base.Awake();

            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = SLayer.Turret;

            // 프리팹에서 할당됐으면 사용, 아니면 자동 생성
            if (electricBeamRenderer != null)
                _electricSr = electricBeamRenderer;
            else
                BuildElectric();

            if (zapLineRenderers != null && zapLineRenderers.Length > 0)
                _zapLines.AddRange(zapLineRenderers);
        }

        private void BuildElectric()
        {
            var elec = new GameObject("ElectricBeam");
            elec.transform.SetParent(transform);
            elec.transform.localPosition = Vector3.zero;
            elec.transform.localScale    = new Vector3(1.0f, 0.08f, 1f);
            _electricSr = elec.AddComponent<SpriteRenderer>();
            _electricSr.sprite       = GameSetup.WhiteSquareStatic();
            _electricSr.color        = new Color(0.4f, 0.8f, 1f, 0f);
            _electricSr.sortingOrder = SLayer.TrapEffect;

            for (int i = 0; i < 3; i++)
            {
                var zap = new GameObject($"Zap{i}");
                zap.transform.SetParent(transform);
                zap.transform.localScale    = new Vector3(0.95f, 0.04f, 1f);
                zap.transform.localPosition = new Vector3(0f, (i - 1) * 0.06f, 0f);
                var sr = zap.AddComponent<SpriteRenderer>();
                sr.sprite       = GameSetup.WhiteSquareStatic();
                sr.color        = new Color(0.7f, 0.95f, 1f, 0f);
                sr.sortingOrder = SLayer.TrapEffect;
                _zapLines.Add(sr);
            }
        }

        protected override void OnTick()
        {
            StartCoroutine(ElectricRoutine());
        }

        private IEnumerator ElectricRoutine()
        {
            if (_electricSr != null) _electricSr.color = new Color(0.4f, 0.8f, 1f, 0.85f);
            foreach (var z in _zapLines) z.color = new Color(0.7f, 0.95f, 1f, 0.7f);

            float dur = 0.3f, t = 0f;
            while (t < dur)
            {
                float flicker = Mathf.Sin(t * 80f) * 0.3f + 0.7f;
                if (_electricSr != null) _electricSr.color = new Color(0.4f, 0.8f, 1f, flicker);
                foreach (var z in _zapLines) z.color = new Color(0.7f, 0.95f, 1f, flicker * 0.8f);

                var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
                foreach (var m in monsters)
                {
                    if (m == null || !m.IsAlive) continue;
                    if (Vector2.Distance(transform.position, m.transform.position) <= range)
                        m.TakeDamage(damage * Time.deltaTime);
                }
                t += Time.deltaTime; yield return null;
            }

            if (_electricSr != null) _electricSr.color = new Color(0.4f, 0.8f, 1f, 0f);
            foreach (var z in _zapLines) z.color = new Color(0.7f, 0.95f, 1f, 0f);
        }
    }
}
