using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 1. 범위 데미지 터렛 - 공격 범위 안 모든 몬스터에게 공격속도당 데미지
    /// </summary>
    public class AreaDamageTurret : TurretBase
    {
        [Header("Area Visual")]
        public Color areaColor = new Color(0.8f, 0.2f, 0.9f, 0.15f);

        private SpriteRenderer _areaRenderer;

        protected override void Awake()
        {
            turretType = TurretType.AreaDamage;
            if (statData == null) { damage = 6f; range = 1.8f; fireRate = 1.5f; hp = 80f; }
            base.Awake();
            BuildAreaVisual();
        }

        private void BuildAreaVisual()
        {
            var go = new GameObject("AreaCircle");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            float d = range * 2f;
            go.transform.localScale = new Vector3(d, d, 1f);
            _areaRenderer = go.AddComponent<SpriteRenderer>();
            _areaRenderer.sprite = GameSetup.WhiteSquareStatic();
            _areaRenderer.color = areaColor;
            _areaRenderer.sortingOrder = SLayer.TrapEffect;
        }

protected override void OnTick() { var targets = FindAllInRange(); float dmg = RollDamage(out bool isCrit); foreach (var m in targets) m.TakeDamage(dmg, isCrit); if (targets.Count > 0) StartCoroutine(PulseRoutine()); }

        private System.Collections.IEnumerator PulseRoutine()
        {
            if (_areaRenderer == null) yield break;
            var col = areaColor;
            _areaRenderer.color = new Color(col.r, col.g, col.b, col.a * 4f);
            yield return new WaitForSeconds(0.08f);
            _areaRenderer.color = col;
        }
    }
}
