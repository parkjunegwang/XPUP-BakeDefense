using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 사정거리 내 아군 터렛의 공격속도(fireRate)를 지속적으로 버프.
    /// </summary>
    public class HasteTower : TurretBase
    {
        [Header("Haste Buff")]
        public float buffMultiplier = 1.35f;
        public float checkInterval  = 1f;

        private float _checkTimer;
        private Dictionary<TurretBase, float> _buffed = new Dictionary<TurretBase, float>();

        protected override void Awake()
        {
            base.Awake();
            _checkTimer = 0f;
        }

        // OnTick은 fireRate 기반 - 여기선 별도 타이머 쓸거라 Update override
        protected override void OnTick() { }

        protected override void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress &&
                GameManager.Instance.CurrentState != GameState.Preparation) return;
            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;
            _checkTimer = checkInterval;
            RefreshBuffs();
        }

        private void RefreshBuffs()
        {
            var all = TurretManager.Instance?.GetAll();
            if (all == null) return;

            // 범위 벗어난 터렛 버프 해제
            var toRemove = new List<TurretBase>();
            foreach (var kv in _buffed)
            {
                var t = kv.Key;
                if (t == null || Vector3.Distance(transform.position, t.transform.position) > range)
                {
                    if (t != null) t.fireRate = kv.Value;
                    toRemove.Add(t);
                }
            }
            foreach (var t in toRemove) _buffed.Remove(t);

            // 범위 내 신규 터렛에 버프
            foreach (var t in all)
            {
                if (t == null || t == this) continue;
                if (t.turretType == TurretType.HasteTower) continue;
                if (Vector3.Distance(transform.position, t.transform.position) > range) continue;
                if (_buffed.ContainsKey(t)) continue;

                float original = t.fireRate;
                _buffed[t] = original;
                t.fireRate  = original * buffMultiplier;
            }
        }

        private void OnDestroy()
        {
            foreach (var kv in _buffed)
                if (kv.Key != null) kv.Key.fireRate = kv.Value;
            _buffed.Clear();
        }
    }
}
