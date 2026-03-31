using UnityEngine;
using UnityEditor;
using System.IO;

namespace Underdark
{
    /// <summary>
    /// 빌드 전 실행 - Assets/Data/Cards 의 CardData 에셋을
    /// Assets/Resources/Cards 로 복사해서 빌드에 포함시킴
    /// </summary>
    public static class CopyCardsToResources
    {
        private const string SRC  = "Assets/Data/Cards";
        private const string DEST = "Assets/Resources/Cards";

        [MenuItem("Underdark/Copy Cards to Resources (Before Build)")]
        public static void CopyCards()
        {
            // Resources/Cards 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(DEST))
                AssetDatabase.CreateFolder("Assets/Resources", "Cards");

            // 기존 파일 전부 삭제
            var existing = AssetDatabase.FindAssets("t:CardData", new[] { DEST });
            foreach (var g in existing)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                AssetDatabase.DeleteAsset(p);
            }

            // 새로 복사
            var guids = AssetDatabase.FindAssets("t:CardData", new[] { SRC });
            int count = 0;
            foreach (var g in guids)
            {
                var srcPath  = AssetDatabase.GUIDToAssetPath(g);
                var fileName = Path.GetFileName(srcPath);
                var dstPath  = $"{DEST}/{fileName}";
                AssetDatabase.CopyAsset(srcPath, dstPath);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CopyCards] Done! {count} cards copied to {DEST}");
        }
    }
}
