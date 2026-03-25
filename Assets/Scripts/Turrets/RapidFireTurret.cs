using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 4. 속사형 터렛 - 공격속도 매우 빠르고 데미지 약함
    /// </summary>
    public class RapidFireTurret : TurretBase
    {
        private SpriteRenderer _flashSr;

        protected override void Awake()
        {
            turretType = TurretType.RapidFire;
            if (statData == null) { damage = 4f; range = 2.5f; fireRate = 5f; hp = 50f; }
            base.Awake();

            // 빠른 총구 플래시
            var go = new GameObject("Flash");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * 0.5f;
            _flashSr = go.AddComponent<SpriteRenderer>();
            _flashSr.sprite = GameSetup.WhiteSquareStatic();
            _flashSr.color = new Color(1f, 1f, 0.2f, 0f);
            _flashSr.sortingOrder = SLayer.Effect;
        }

protected override void OnTick() { var target = FindClosestInRange(); if (target == null) return; AimBarrel(target.transform.position); target.TakeDamage(damage); if (_flashSr != null) { _flashSr.color = new Color(1f, 1f, 0.2f, 0.9f); Invoke(nameof(ClearFlash), 0.04f); } }

        private void ClearFlash()
        {
            if (_flashSr != null) _flashSr.color = new Color(1f, 1f, 0.2f, 0f);
        }
    }
}
