using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 주기적 펄스로 사정거리 내 모든 몬스터를 슬로우 + 미약한 데미지
    /// </summary>
    public class PulseSlower : TurretBase
    {
        [Header("Pulse Settings")]
        public float pulseInterval = 2.5f;
        public float slowFactor    = 0.4f;
        public float slowDuration  = 1.5f;

        private float          _pulseTimer;
        private SpriteRenderer _ringRenderer;

        protected override void Awake()
        {
            base.Awake();
            _pulseTimer = pulseInterval;

            var ring = new GameObject("PulseRing");
            ring.transform.SetParent(transform, false);
            _ringRenderer = ring.AddComponent<SpriteRenderer>();
            _ringRenderer.sprite       = GetComponent<SpriteRenderer>()?.sprite;
            _ringRenderer.color        = new Color(0.5f, 0.8f, 1f, 0f);
            _ringRenderer.sortingOrder = SLayer.Effect;
            ring.transform.localScale  = Vector3.zero;
        }

        protected override void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return;
            _pulseTimer -= Time.deltaTime;
            if (_pulseTimer > 0f) return;
            _pulseTimer = pulseInterval;
            DoPulse();
        }

        // OnTick은 cooldown 기반이라 여기선 안 씀 (Update에서 직접 타이머)
        protected override void OnTick() { }

        private void DoPulse()
        {
            var monsters = MonsterManager.Instance?.ActiveMonsters;
            if (monsters == null) return;

            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                if (Vector3.Distance(transform.position, m.transform.position) > range) continue;
                bool isCrit;
                float dmg = RollDamage(out isCrit);
                m.TakeDamage(dmg, isCrit);
                m.ApplySlow(slowFactor, slowDuration);
            }
            StartCoroutine(PulseAnim());
        }

        private IEnumerator PulseAnim()
        {
            if (_ringRenderer == null) yield break;
            float t = 0f, dur = 0.5f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                float r = range > 0 ? range : 2f;
                _ringRenderer.transform.localScale = Vector3.one * (p * r * 2f);
                _ringRenderer.color = new Color(0.5f, 0.8f, 1f, (1f - p) * 0.6f);
                yield return null;
            }
            _ringRenderer.color = new Color(0.5f, 0.8f, 1f, 0f);
            _ringRenderer.transform.localScale = Vector3.zero;
        }
    }
}
