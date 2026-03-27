using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 블랙홀 터렛 - 느린 공격속도, 범위에 원이 생겨 적들을 중심으로 빨아들인 후 폭발 데미지
    /// </summary>
    public class BlackHoleTurret : TurretBase
    {
        [Header("Black Hole Settings")]
        [Tooltip("블랙홀 지속 시간 (초)")]
        public float suctionDuration = 2.5f;
        [Tooltip("빨아들이는 힘")]
        public float suctionForce    = 3.5f;
        [Tooltip("폭발 데미지 배율 (최종 데미지 = damage * burstMult)")]
        public float burstMult       = 3.0f;

        private bool _isActive = false;

        protected override void Awake()
        {
            turretType = TurretType.BlackHole;
            if (statData == null) { damage = 40f; range = 2.5f; fireRate = 0.25f; hp = 120f; }
            base.Awake();
        }

        // 웨이브 중에도 블랙홀은 쿨다운 방식으로 발동
        protected override void OnTick()
        {
            if (_isActive) return;
            StartCoroutine(BlackHoleRoutine());
        }

private IEnumerator BlackHoleRoutine() { _isActive = true; var first = FindClosestInRange(); Vector3 center = first != null ? first.transform.position : transform.position; var circleGo = new GameObject("BlackHoleCircle"); circleGo.transform.position = center; var lr = circleGo.AddComponent<LineRenderer>(); SetupCircleLR(lr); var particleGo = BuildSuctionParticles(center); float elapsed = 0f; while (elapsed < suctionDuration) { elapsed += Time.deltaTime; float t = elapsed / suctionDuration; float curRadius = Mathf.Lerp(0.1f, range, Mathf.SmoothStep(0f, 1f, t)); DrawCircle(lr, center, curRadius, t); var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters); foreach (var m in monsters) { if (m == null || !m.IsAlive) continue; float dist = Vector2.Distance(center, m.transform.position); if (dist > range) continue; Vector3 dir = (center - m.transform.position).normalized; float force = suctionForce * (1f - dist / range) * Time.deltaTime; m.transform.position += dir * force; } yield return null; } BurstExplosion(center); SpawnBurstEffect(center); Destroy(circleGo); if (particleGo != null) Destroy(particleGo); _isActive = false; }

private void BurstExplosion(Vector3 center) { float burstDmg = RollDamage(out bool isCrit); burstDmg *= burstMult; var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters); foreach (var m in monsters) { if (m == null || !m.IsAlive) continue; float dist = Vector2.Distance(center, m.transform.position); if (dist > range * 1.1f) continue; float ratio = 1f - dist / (range * 1.1f); m.TakeDamage(burstDmg * Mathf.Lerp(0.5f, 1f, ratio), isCrit); } }

        // ── 비주얼 헬퍼 ───────────────────────────────────────────────
        private void SetupCircleLR(LineRenderer lr)
        {
            lr.useWorldSpace    = true;
            lr.loop             = true;
            lr.positionCount    = 48;
            lr.material         = new Material(Shader.Find("Sprites/Default"));
            lr.startWidth       = 0.06f;
            lr.endWidth         = 0.06f;
            lr.sortingOrder     = SLayer.Effect;
        }

        private void DrawCircle(LineRenderer lr, Vector3 center, float radius, float t)
        {
            // 색상: 어두운 보라 → 밝은 보라→흰색 (폭발 직전)
            Color col = Color.Lerp(
                new Color(0.5f, 0f, 1f, 0.6f),
                new Color(1f, 0.5f, 1f, 1f), t);
            lr.startColor = col;
            lr.endColor   = col;
            lr.startWidth = 0.04f + t * 0.05f;
            lr.endWidth   = lr.startWidth;

            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48;
                lr.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius, -0.1f));
            }
        }

        private GameObject BuildSuctionParticles(Vector3 center)
        {
            var go = new GameObject("BHParticles");
            go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime   = 0.8f;
            main.startSpeed      = 1.5f;
            main.startSize       = 0.12f;
            main.startColor      = new Color(0.6f, 0.1f, 1f, 0.9f);
            main.maxParticles    = 60;
            main.loop            = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.enabled     = true;
            shape.shapeType   = ParticleSystemShapeType.Circle;
            shape.radius      = range;
            shape.radiusThickness = 0.1f;

            // 중심으로 향하도록
            var vel = ps.velocityOverLifetime;
            vel.enabled   = true;
            vel.radial    = -3f; // 음수 = 중심으로
            vel.space     = ParticleSystemSimulationSpace.Local;

            var psRenderer = go.GetComponent<ParticleSystemRenderer>();
            psRenderer.sortingOrder = SLayer.Effect;

            ps.Play();
            return go;
        }

        private void SpawnBurstEffect(Vector3 center)
        {
            // 폭발 원
            StartCoroutine(BurstRingRoutine(center));
        }

        private IEnumerator BurstRingRoutine(Vector3 center)
        {
            var go = new GameObject("BurstRing");
            go.transform.position = center;
            var lr = go.AddComponent<LineRenderer>();
            SetupCircleLR(lr);
            lr.startColor = new Color(1f, 0.6f, 1f, 1f);
            lr.endColor   = lr.startColor;

            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float r = range * (1f + t * 1.5f);
                float a = Mathf.Lerp(1f, 0f, t / 0.4f);
                lr.startColor = new Color(1f, 0.6f, 1f, a);
                lr.endColor   = lr.startColor;
                DrawCircle(lr, center, r, 1f);
                yield return null;
            }
            Destroy(go);
        }
    }
}
