using UnityEngine;

namespace Underdark
{
    [CreateAssetMenu(fileName = "TurretStatData", menuName = "Underdark/Turret Stat Data")]
    public class TurretStatData : ScriptableObject
    {
        [System.Serializable]
        public class LevelStat
        {
            [Header("Level")]
            public int level = 1;

            [Header("Combat")]
            public float damage   = 10f;
            public float range    = 2f;
            [Tooltip("Attacks per second")]
            public float fireRate = 1f;
            public float hp       = 50f;

            [Header("Special")]
            [Tooltip("MeleeTurret: tiles ahead to attack")]
            public int attackTiles = 1;
        }

        [Header("Turret Type")]
        public TurretType turretType;

        [Header("Visual")]
        [Tooltip("Auto-scale to fit tile size. If false, uses prefab's original scale.")]
        public bool autoScale = true;

        // ── 타일 모양 설정 ─────────────────────────────────────────────
        [Header("Tile Shape")]
        [Tooltip("If true, use tileShape offsets below instead of sizeX/sizeY rectangle.")]
        public bool useCustomShape = false;

        [Tooltip(
            "Custom tile offsets from origin tile (0,0 = origin).\n" +
            "Example L-shape: (0,0),(1,0),(0,1)\n" +
            "Example T-shape: (0,0),(1,0),(2,0),(1,1)\n" +
            "Leave empty to use sizeX × sizeY rectangle.")]
        public Vector2Int[] tileShape = new Vector2Int[] { Vector2Int.zero };

        [Tooltip("Which tile indices in tileShape are passable (monsters can walk through).\n" +
                 "Used for ElectricGate-style traps. Leave empty = all block.")]
        public int[] passableTileIndices = new int[0];

        [Header("Level Stats (index 0 = Level 1)")]
        public LevelStat[] levels = new LevelStat[]
        {
            new LevelStat { level = 1 }
        };

        public LevelStat GetLevel(int lv)
        {
            int idx = Mathf.Clamp(lv - 1, 0, levels.Length - 1);
            return levels[idx];
        }

        public int MaxLevel => levels.Length;

        /// <summary>
        /// tileShape 배열로부터 실제 점유 타일 오프셋 반환.
        /// useCustomShape=false면 sizeX×sizeY 직사각형 반환.
        /// </summary>
public Vector2Int[] GetShape(int sizeX = 1, int sizeY = 1) { if (tileShape != null && tileShape.Length > 1) return tileShape; var list = new System.Collections.Generic.List<Vector2Int>(); for (int dy = 0; dy < sizeY; dy++) for (int dx = 0; dx < sizeX; dx++) list.Add(new Vector2Int(dx, dy)); return list.ToArray(); }

        /// <summary>해당 인덱스 타일이 통과 가능한가</summary>
public bool IsTilePassable(int shapeIndex) { if (passableTileIndices == null) return false; foreach (var i in passableTileIndices) if (i == shapeIndex) return true; return false; } public bool HasCustomShape() { return tileShape != null && tileShape.Length > 1; }
    }
}
