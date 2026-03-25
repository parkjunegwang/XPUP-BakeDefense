using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 보유 타워 설치권 관리.
    /// 카드를 통해 획득하고, 설치 시 차감.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        // 타입별 남은 설치 가능 수
        private Dictionary<TurretType, int> _stock = new Dictionary<TurretType, int>();

        public event System.Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Add(TurretType type, int count)
        {
            if (!_stock.ContainsKey(type)) _stock[type] = 0;
            _stock[type] += count;
            OnInventoryChanged?.Invoke();
        }

        public int Get(TurretType type)
        {
            return _stock.ContainsKey(type) ? _stock[type] : 0;
        }

        public bool CanPlace(TurretType type) => Get(type) > 0;

        /// <summary>설치 시 차감. 성공하면 true.</summary>
        public bool Consume(TurretType type)
        {
            if (!CanPlace(type)) return false;
            _stock[type]--;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public Dictionary<TurretType, int> GetAll() => new Dictionary<TurretType, int>(_stock);

        public void Clear()
        {
            _stock.Clear();
            OnInventoryChanged?.Invoke();
        }
    }
}
