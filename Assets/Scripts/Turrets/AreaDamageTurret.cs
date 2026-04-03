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
        [Tooltip("공격범위 표시 스프라이트 (없으면 흰 사각형)")]
        public Sprite areaSprite;
        public Color areaColor = new Color(0.8f, 0.2f, 0.9f, 0.15f);

        private SpriteRenderer _areaRenderer;

protected override void Awake()
        {
            turretType = TurretType.AreaDamage;
            base.Awake();
            BuildAreaVisual();
        }

private void BuildAreaVisual()
        {
            // 기존 AreaCircle 제거
            var old = transform.Find("AreaCircle");
            if (old != null) Destroy(old.gameObject);

            var go = new GameObject("AreaCircle");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            _areaRenderer = go.AddComponent<SpriteRenderer>();
            _areaRenderer.sortingOrder = SLayer.TrapEffect;

            RefreshAreaVisual();
        }

        public void RefreshAreaVisual()
        {
            if (_areaRenderer == null) return;

            // areaSprite가 있으면 사용, 없으면 화이트 사각형
            _areaRenderer.sprite = areaSprite != null ? areaSprite : GameSetup.WhiteSquareStatic();
            _areaRenderer.color  = areaColor;

            // range에 따라 크기 조정
            float curRange = range > 0 ? range : 1.8f;
            float d = curRange * 2f;

            if (areaSprite != null)
            {
                // 스프라이트 픽셀당 유닛스 기준으로 크기 맞춤
                float ppu  = areaSprite.pixelsPerUnit;
                float sprW = areaSprite.rect.width  / ppu;
                float sprH = areaSprite.rect.height / ppu;
                _areaRenderer.transform.localScale = new Vector3(d / sprW, d / sprH, 1f);
            }
            else
            {
                _areaRenderer.transform.localScale = new Vector3(d, d, 1f);
            }
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
