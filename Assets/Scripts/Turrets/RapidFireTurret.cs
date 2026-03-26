using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 4. 속사형 터렛 - 공격속도 매우 빠르고 데미지 약함
    /// </summary>
    public class RapidFireTurret : TurretBase
    {
        [Header("Projectile")]
        public GameObject projectilePrefab;
        private SpriteRenderer _flashSr;

protected override void Awake() { turretType = TurretType.RapidFire; if (statData == null) { damage = 4f; range = 2.5f; fireRate = 5f; hp = 50f; } base.Awake(); var flashGo = new GameObject("Flash"); flashGo.transform.SetParent(transform); flashGo.transform.localPosition = Vector3.zero; flashGo.transform.localScale = Vector3.one * 0.5f; _flashSr = flashGo.AddComponent<SpriteRenderer>(); _flashSr.sprite = GameSetup.WhiteSquareStatic(); _flashSr.color = new Color(1f, 1f, 0.2f, 0f); _flashSr.sortingOrder = SLayer.Effect; }

protected override void OnTick() { var target = FindClosestInRange(); if (target == null) return; AimBarrel(target.transform.position); Vector3 spawnPos = GetFirePosition(); if (projectilePrefab != null) { bool was = projectilePrefab.activeSelf; projectilePrefab.SetActive(true); var go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity); projectilePrefab.SetActive(was); go.GetComponent<Projectile>()?.Init(target, damage); } else { target.TakeDamage(damage); } if (_flashSr != null) { _flashSr.color = new Color(1f, 1f, 0.2f, 0.9f); Invoke(nameof(ClearFlash), 0.04f); } }

        private void ClearFlash()
        {
            if (_flashSr != null) _flashSr.color = new Color(1f, 1f, 0.2f, 0f);
        }
    }
}
