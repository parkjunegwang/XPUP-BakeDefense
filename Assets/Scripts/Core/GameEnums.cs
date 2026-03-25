namespace Underdark
{
    public static class SLayer
    {
        public const int Tile          = 0;   // 바닥 타일 고정
        public const int TrapEffect    = 1;
        public const int Turret        = 10;  // Y소팅 기준 (10 + offset)
        public const int Monster       = 100; // Y소팅 기준 (100 - y*10)
        public const int MonsterHPBg   = 5;
        public const int MonsterHPFill = 6;
        public const int Effect        = 200;
        public const int Projectile    = 210;
        public const int Ghost         = 300;
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
