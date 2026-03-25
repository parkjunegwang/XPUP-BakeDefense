#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Underdark
{
    public static class MapToolUIBaker
    {
        [MenuItem("Underdark/Bake Map Tool UI to Scene")]
        public static void BakeUI()
        {
            var uiComp = Object.FindObjectOfType<MapToolUI>();
            if (uiComp == null)
            {
                EditorUtility.DisplayDialog("Error", "MapToolUI component not found in scene!", "OK");
                return;
            }

            // 기존 Canvas 제거
            var existing = GameObject.Find("MapToolCanvas");
            if (existing != null) Undo.DestroyObjectImmediate(existing);
            var existingES = GameObject.Find("EventSystem");
            if (existingES != null) Undo.DestroyObjectImmediate(existingES);

            // _tool 초기화 후 BuildUI 호출
            uiComp.InitForEditor();
            uiComp.BuildUI();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[MapToolUIBaker] Done! Hierarchy에서 MapToolCanvas를 확인하세요.");
            EditorUtility.DisplayDialog("완료!",
                "MapToolCanvas가 Hierarchy에 생성됐어요!\n\n" +
                "이제 Inspector에서 직접:\n" +
                "• RectTransform으로 크기/위치 조절\n" +
                "• Image로 배경색 변경\n" +
                "• TextMeshProUGUI로 폰트 크기/색상 변경\n\n" +
                "Ctrl+S로 씬 저장하면 영구 반영!", "OK");
        }

        [MenuItem("Underdark/Remove Baked Map Tool UI")]
        public static void RemoveUI()
        {
            var c = GameObject.Find("MapToolCanvas");
            if (c != null) { Undo.DestroyObjectImmediate(c); Debug.Log("[MapToolUIBaker] MapToolCanvas 제거됨"); }
            var es = GameObject.Find("EventSystem");
            if (es != null) Undo.DestroyObjectImmediate(es);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif
