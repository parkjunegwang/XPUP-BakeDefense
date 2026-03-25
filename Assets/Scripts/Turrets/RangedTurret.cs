using System.Collections;
using UnityEngine;

namespace Underdark
{
    public class RangedTurret : TurretBase
    {
        public GameObject projectilePrefab;

        [Header("Visual Effects")]
        public SpriteRenderer flashRenderer; // 프리팹에서 할당 (없으면 자동 생성)

        private SpriteRenderer _flashSr;

        protected override void Awake()
        {
            turretType = TurretType.RangedTurret;

            // statData 없을 때 기본값
            if (statData == null) { damage = 12f; range = 2.8f; fireRate = 1f; hp = 60f; }

            base.Awake();

            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = SLayer.Turret;

            // 총구 플래시 - 프리팹에 이미 있으면 사용, 없으면 자동 생성
            if (flashRenderer != null)
                _flashSr = flashRenderer;
            else
                _flashSr = BuildFlash();
        }

        private SpriteRenderer BuildFlash()
        {
            var go = new GameObject("Flash");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one * 1.3f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = GameSetup.WhiteSquareStatic();
            sr.color        = new Color(1f, 0.95f, 0.3f, 0f);
            sr.sortingOrder = SLayer.Effect;
            return sr;
        }

protected override void OnTick() { var target = FindClosestInRange(); if (target == null) return; AimBarrel(target.transform.position); if (projectilePrefab != null) { bool was = projectilePrefab.activeSelf; projectilePrefab.SetActive(true); var go = Instantiate(projectilePrefab, transform.position, Quaternion.identity); projectilePrefab.SetActive(was); go.GetComponent<Projectile>()?.Init(target, damage); } else target.TakeDamage(damage); StartCoroutine(FlashRoutine()); }

        private IEnumerator FlashRoutine()
        {
            if (_flashSr == null) yield break;
            _flashSr.color = new Color(1f, 0.95f, 0.3f, 0.95f);
            yield return new WaitForSeconds(0.07f);
            _flashSr.color = new Color(1f, 0.95f, 0.3f, 0f);
        }
    }
}
