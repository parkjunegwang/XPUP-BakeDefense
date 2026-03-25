using UnityEngine;

namespace Underdark
{
    [System.Serializable]
    public class TurretDef
    {
        public TurretType type;

        [Tooltip("가로 칸수 (X 방향)")]
        public int sizeX = 1;

        [Tooltip("세로 칸수 (Y 방향) - 보통 1")]
        public int sizeY = 1;

        public int    cost       = 10;
        public Color  color      = Color.white;
        public string label      = "타워";
        public string emoji      = "🔫";

        [Tooltip("몬스터가 통과 가능한 타워 (함정/전기 가운데)")]
        public bool isPassable = false;

        // autoScale은 TurretStatData에서 관리됩니다

        // 하위 호환
        public int sizeInTiles => sizeX;
    }
}
