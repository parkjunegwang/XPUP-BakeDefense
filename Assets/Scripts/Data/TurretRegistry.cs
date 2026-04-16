using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 터렛 하나 추가 시 여기에만 항목 추가하면 끝.
    /// TurretManager, CardManager, InventoryUI 전부 자동 반영.
    /// </summary>
    [CreateAssetMenu(fileName = "TurretRegistry", menuName = "Underdark/Turret Registry")]
    public class TurretRegistry : ScriptableObject
    {
        [System.Serializable]
        public class TurretEntry
        {
            [Header("타입 (GameEnums.cs에서 enum 추가 후 선택)")]
            public TurretType type;

            [Header("프리팹")]
            public GameObject prefab;

            [Header("배치 설정")]
            public int  sizeX      = 1;
            public int  sizeY      = 1;
            public int  cost       = 10;
            public bool isPassable = false;

            [Header("표시")]
            public string label = "Tower";
            public string emoji = "🔫";
            public Color  color = Color.white;

            [Header("벽 타입 여부 (머지 불가, 카드 제외)")]
            public bool isWall = false;

            [Header("기본 소유 여부 (처음부터 해금된 터렛)")]
            public bool isDefault = false;
        }

        public List<TurretEntry> entries = new List<TurretEntry>();

        // ── 조회 헬퍼 ────────────────────────────────────────────────

        private Dictionary<TurretType, TurretEntry> _cache;

        private void OnEnable() => RebuildCache();

        public void RebuildCache()
        {
            _cache = new Dictionary<TurretType, TurretEntry>();
            foreach (var e in entries)
                if (e != null && !_cache.ContainsKey(e.type))
                    _cache[e.type] = e;
        }

        public TurretEntry Get(TurretType type)
        {
            if (_cache == null) RebuildCache();
            return _cache.TryGetValue(type, out var e) ? e : null;
        }

        public GameObject GetPrefab(TurretType type) => Get(type)?.prefab;

        public TurretDef GetDef(TurretType type)
        {
            var e = Get(type);
            if (e == null) return new TurretDef { type = type, sizeX = 1, sizeY = 1, cost = 10 };
            return new TurretDef
            {
                type       = e.type,
                sizeX      = e.sizeX,
                sizeY      = e.sizeY,
                cost       = e.cost,
                color      = e.color,
                label      = e.label,
                emoji      = e.emoji,
                isPassable = e.isPassable,
            };
        }

        public bool IsWall(TurretType type) => Get(type)?.isWall ?? false;

        public IReadOnlyList<TurretEntry> All => entries;
    }
}
