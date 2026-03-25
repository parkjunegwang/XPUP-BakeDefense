using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 맵 툴에서 저장/불러오기하는 맵 데이터.
    /// Assets/Data/Maps/ 에 저장.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapData", menuName = "Underdark/Map Data")]
    public class MapData : ScriptableObject
    {
        [Header("Map Size")]
        public int columns = 7;
        public int rows    = 11;
        public float tileSize = 1.0f;
        public float tileGap  = 0.05f;

        [Header("Spawn / End Points")]
        public List<Vector2Int> spawnPositions = new List<Vector2Int>();
        public List<Vector2Int> endPositions   = new List<Vector2Int>();

        [Header("Tile Overrides")]
        public List<TileOverride> tileOverrides = new List<TileOverride>();

        [Header("Wall Placements")]
        public List<WallPlacement> wallPlacements = new List<WallPlacement>();

        [System.Serializable]
        public class TileOverride
        {
            public int gridX;
            public int gridY;
            public string spritePath; // Assets/... 경로
        }

        [System.Serializable]
        public class WallPlacement
        {
            public int gridX;
            public int gridY;
            public WallSizeType wallSize;
        }

        public enum WallSizeType { Wall1x1, Wall2x1, Wall1x2, Wall2x2 }
    }
}
