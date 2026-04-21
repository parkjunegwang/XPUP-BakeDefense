using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Underdark
{
    public static class PassRewardTableEditor
    {
        [MenuItem("Underdark/Pass Reward/Create Default Table")]
        public static void CreateDefaultTable()
        {
            const string folder = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            const string path = "Assets/Resources/PassRewardTable.asset";

            if (AssetDatabase.LoadAssetAtPath<PassRewardData>(path) != null)
            {
                if (!EditorUtility.DisplayDialog("PassRewardTable",
                    $"이미 {path} 가 존재합니다. 덮어씁니까?\n(기존 데이터 초기화됨)", "덮어쓰기", "취소"))
                    return;
                AssetDatabase.DeleteAsset(path);
            }

            var table = ScriptableObject.CreateInstance<PassRewardData>();
            table.entries = BuildDefaultEntries();

            AssetDatabase.CreateAsset(table, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = table;
            Debug.Log($"[PassRewardTable] 생성 완료: {path}");
        }

        [MenuItem("Underdark/Pass Reward/Select Table")]
        public static void SelectTable()
        {
            var table = Resources.Load<PassRewardData>(PassRewardData.RESOURCE_PATH);
            if (table == null)
            {
                EditorUtility.DisplayDialog("PassRewardTable",
                    "PassRewardTable.asset 을 찾을 수 없습니다.\n'Create Default Table' 을 먼저 실행하세요.", "확인");
                return;
            }
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = table;
        }

        private static List<PassLevelEntry> BuildDefaultEntries()
        {
            return new List<PassLevelEntry>
            {
                Make(1,  PassRewardType.Gold,       100,  PassRewardType.Gem,        50),
                Make(2,  PassRewardType.Gold,       200,  PassRewardType.Gem,       100),
                Make(3,  PassRewardType.TurretCard,   1,  PassRewardType.Gold,      500),
                Make(4,  PassRewardType.Gold,       300,  PassRewardType.Gem,       150),
                Make(5,  PassRewardType.Gem,          5,  PassRewardType.TurretCard,  3),
                Make(6,  PassRewardType.Gold,       400,  PassRewardType.Gem,       200),
                Make(7,  PassRewardType.TurretCard,   2,  PassRewardType.Gold,      800),
                Make(8,  PassRewardType.Gold,       500,  PassRewardType.Gem,       250),
                Make(9,  PassRewardType.Gem,         10,  PassRewardType.TurretCard,  5),
                Make(10, PassRewardType.Gold,       600,  PassRewardType.Gem,       300),
                Make(11, PassRewardType.TurretCard,   3,  PassRewardType.Gold,     1200),
                Make(12, PassRewardType.Gold,       700,  PassRewardType.Gem,       350),
                Make(13, PassRewardType.Gem,         15,  PassRewardType.TurretCard,  8),
                Make(14, PassRewardType.Gold,       800,  PassRewardType.Gem,       400),
                Make(15, PassRewardType.TurretCard,   5,  PassRewardType.Gold,     1500),
                Make(16, PassRewardType.Gold,       900,  PassRewardType.Gem,       450),
                Make(17, PassRewardType.Gem,         20,  PassRewardType.TurretCard, 10),
                Make(18, PassRewardType.Gold,      1000,  PassRewardType.Gem,       500),
                Make(19, PassRewardType.TurretCard,  10,  PassRewardType.Gold,     2000),
                new PassLevelEntry
                {
                    level = 20,
                    freeReward = new PassReward { type = PassRewardType.LootBox, amount = 1, displayName = "★ 전설 상자" },
                    paidReward = new PassReward { type = PassRewardType.LootBox, amount = 3, displayName = "★★ 프리미엄 상자" },
                },
            };
        }

        private static PassLevelEntry Make(int lv,
            PassRewardType ft, int fa,
            PassRewardType pt, int pa)
        {
            return new PassLevelEntry
            {
                level      = lv,
                freeReward = new PassReward { type = ft, amount = fa },
                paidReward = new PassReward { type = pt, amount = pa },
            };
        }
    }
}
