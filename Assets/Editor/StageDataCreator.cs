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
        public static void CreateAll()
        {
            var enemy1  = AssetDatabase.LoadAssetAtPath<MonsterStatData>("Assets/Data/Monsters/Enemy1_Stats.asset");
            var enemy2  = AssetDatabase.LoadAssetAtPath<MonsterStatData>("Assets/Data/Monsters/Enemy2_Stats.asset");
            var enemy3  = AssetDatabase.LoadAssetAtPath<MonsterStatData>("Assets/Data/Monsters/Enemy3_Stats.asset");
            var boss    = AssetDatabase.LoadAssetAtPath<MonsterStatData>("Assets/Data/Monsters/Boss0_Stats.asset");

            CreateStage("Stage1", "Stage 1 - Entrance", "던전의 입구. 기본 적들이 등장한다.", new List<WaveData>
            {
                MakeWave("Wave 1 - Scouts",  new[]{ Grp(enemy1, 6) }),
                MakeWave("Wave 2 - Assault", new[]{ Grp(enemy1, 8), Grp(enemy2, 3) }),
                MakeWave("Wave 3 - Boss",    new[]{ Grp(enemy1, 5), Grp(boss,   1, true) }),
            });

            CreateStage("Stage2", "Stage 2 - Catacombs", "지하 납골당. 더 강하고 빠른 적들이 나타난다.", new List<WaveData>
            {
                MakeWave("Wave 1",        new[]{ Grp(enemy2, 8) }),
                MakeWave("Wave 2",        new[]{ Grp(enemy2, 8),  Grp(enemy3, 4) }),
                MakeWave("Wave 3",        new[]{ Grp(enemy3, 12) }),
                MakeWave("Wave 4 - Boss", new[]{ Grp(enemy3, 6),  Grp(boss,   1, true) }),
            });

            CreateStage("Stage3", "Stage 3 - Dark Core", "던전의 심장부. 최강의 적이 기다린다.", new List<WaveData>
            {
                MakeWave("Wave 1",            new[]{ Grp(enemy3, 10), Grp(enemy2, 3) }),
                MakeWave("Wave 2",            new[]{ Grp(enemy3, 15) }),
                MakeWave("Wave 3 - Mini Boss",new[]{ Grp(enemy3, 8),  Grp(boss,   1, true) }),
                MakeWave("Wave 4 - Elite",    new[]{ Grp(enemy3, 20) }),
                MakeWave("Wave 5 - Final",    new[]{ Grp(boss,   1, true) }),
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StageDataCreator] 스테이지 3개 생성 완료!");
        }

        static void CreateStage(string fileName, string stageName, string desc, List<WaveData> waves)
        {
            string path = $"Assets/Data/Stages/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StageData>(path);
            var data = existing != null ? existing : ScriptableObject.CreateInstance<StageData>();
            data.stageName   = stageName;
            data.description = desc;
            data.waves       = waves;
            if (existing == null)
                AssetDatabase.CreateAsset(data, path);
            else
                EditorUtility.SetDirty(data);
        }

        static WaveData MakeWave(string name, MonsterSpawnGroup[] groups)
            => new WaveData { waveName = name, groups = new List<MonsterSpawnGroup>(groups) };

        static MonsterSpawnGroup Grp(MonsterStatData stat, int count, bool boss = false)
            => new MonsterSpawnGroup { statData = stat, count = count, isBoss = boss };
    }
}
