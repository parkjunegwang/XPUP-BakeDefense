namespace Underdark
{
    public static class SLayer
    {
        public const int Tile          = 0;
        public const int TrapEffect    = 1;  // 함정 가시/전기 이펙트
        public const int Turret        = 2;
        public const int Monster       = 4;
        public const int MonsterHPBg   = 5;
        public const int MonsterHPFill = 6;
        public const int Effect        = 8;
        public const int Projectile    = 9;
        public const int Ghost         = 15;
    }

    public enum GameState  { Preparation, WaveInProgress, GameOver, Victory }
    public enum TileType   { Empty, SpawnPoint, EndPoint }
    public enum TurretType
    {
        None,
        RangedTurret,   // 4. 원거리 타워
        MeleeTurret,    // 3. 근거리 타워 (방향 고정)
        SpikeTrap,      // 1. 가시 함정 (2칸, 통과 가능)
        ElectricGate,   // 2. 전기 기둥 (3칸, 가운데 통과 가능)
        Wall,           // 벽 1x1
        Wall2x1,        // 벽 2x1 (가로)
        Wall1x2,        // 벽 1x2 (세로)
        Wall2x2,        // 벽 2x2
    }
}
