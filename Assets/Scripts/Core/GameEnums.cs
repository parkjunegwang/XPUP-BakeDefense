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
        public const int Projectile    = 9999; // 발사체 - 항상 최상위
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
        // ── 신규 터렛 ──
        AreaDamage,     // 1. 범위 지속 데미지
        ExplosiveCannon,// 2. 폭발 포탄
        SlowShooter,    // 3. 슬로우 발사체
        RapidFire,      // 4. 빠른 공격속도
        Tornado,        // 5. 회오리
        LavaRain,       // 6. 마그마 비
        ChainLightning, // 7. 체인 라이트닝
        BlackHole,      // 8. 블랙홀
        PrecisionStrike,// 9. 정밀 타격 (항상 크리, 작은 데미지)
        GambleBat,      // 10. 갬블 배트 (낮은 확률 대박 크리)
        // ── 신규 터렛 2탄 ──
        PulseSlower,    // 11. 주기적 펄스 전체 슬로우 + 미약 데미지
        DragonStatue,   // 12. 좌우 화염 브레스 + 화상 도트뎀
        HasteTower,     // 13. 주변 터렛 공격속도 버프
        PinballCannon,  // 14. 튕기는 포탄
        BoomerangTurret,// 15. 부메랑 (왕복 관통 데미지 감쇄)
    }
}
