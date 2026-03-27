using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Underdark
{
    /// <summary>
    /// StageData 샘플 자동 생성 에디터 유틸
    /// </summary>
    public static class StageDataCreator
    {
        [MenuItem("Underdark/Create Sample Stage Data")]
public static void CreateAll() { var enemy1 = AssetDatabase.LoadAssetAtPath<MonsterStatData>("Assets/Data/Monsters/Enemy1_Stats.asset"); var enemy2 = AssetDatabase.LoadAssetAtPath<MonsterStatData>("Assets/Data/Monsters/Enemy2_Stats.asset"); var enemy3 = AssetDatabase.LoadAssetAtPath<MonsterStatData>("Assets/Data/Monsters/Enemy3_Stats.asset"); var boss   = AssetDatabase.LoadAssetAtPath<MonsterStatData>("Assets/Data/Monsters/Boss0_Stats.asset"); var pool1 = new TurretType[] { TurretType.RangedTurret, TurretType.MeleeTurret, TurretType.SpikeTrap, TurretType.Wall, TurretType.SlowShooter }; var pool2 = new TurretType[] { TurretType.RangedTurret, TurretType.ExplosiveCannon, TurretType.ElectricGate, TurretType.SlowShooter, TurretType.RapidFire }; var pool3 = new TurretType[] { TurretType.ChainLightning, TurretType.BlackHole, TurretType.LavaRain, TurretType.Tornado, TurretType.GambleBat }; CreateStage("Stage1", "Stage 1 - Entrance", pool1, 4, 2, new List<WaveData> { MakeWave("Wave 1 - Scouts",  new[]{ Grp(enemy1, 6) }), MakeWave("Wave 2 - Assault", new[]{ Grp(enemy1, 8), Grp(enemy2, 3) }), MakeWave("Wave 3 - Boss",    new[]{ Grp(enemy1, 5), Grp(boss, 1, true) }), }); CreateStage("Stage2", "Stage 2 - Catacombs", pool2, 4, 2, new List<WaveData> { MakeWave("Wave 1",        new[]{ Grp(enemy2, 8) }), MakeWave("Wave 2",        new[]{ Grp(enemy2, 8),  Grp(enemy3, 4) }), MakeWave("Wave 3",        new[]{ Grp(enemy3, 12) }), MakeWave("Wave 4 - Boss", new[]{ Grp(enemy3, 6), Grp(boss, 1, true) }), }); CreateStage("Stage3", "Stage 3 - Dark Core", pool3, 4, 2, new List<WaveData> { MakeWave("Wave 1",             new[]{ Grp(enemy3, 10), Grp(enemy2, 3) }), MakeWave("Wave 2",             new[]{ Grp(enemy3, 15) }), MakeWave("Wave 3 - Mini Boss", new[]{ Grp(enemy3, 8), Grp(boss, 1, true) }), MakeWave("Wave 4 - Elite",     new[]{ Grp(enemy3, 20) }), MakeWave("Wave 5 - Final",     new[]{ Grp(boss, 1, true) }), }); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Debug.Log("[StageDataCreator] 스테이지 3개 생성 완료!"); }

static void CreateStage(string fileName, string stageName, TurretType[] pool, int count, int cardPicks, List<WaveData> waves) { string path = $"Assets/Data/Stages/{fileName}.asset"; var existing = AssetDatabase.LoadAssetAtPath<StageData>(path); var data = existing != null ? existing : ScriptableObject.CreateInstance<StageData>(); data.stageName = stageName; data.startTurretPool = pool; data.startTurretCount = count; data.initialCardPicks = cardPicks; data.waves = waves; if (existing == null) AssetDatabase.CreateAsset(data, path); else EditorUtility.SetDirty(data); }

        static WaveData MakeWave(string name, MonsterSpawnGroup[] groups)
            => new WaveData { waveName = name, groups = new List<MonsterSpawnGroup>(groups) };

        static MonsterSpawnGroup Grp(MonsterStatData stat, int count, bool boss = false)
            => new MonsterSpawnGroup { statData = stat, count = count, isBoss = boss };
    }
}
