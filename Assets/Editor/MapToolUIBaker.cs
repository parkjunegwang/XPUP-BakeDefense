#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Underdark
{
    /// <summary>
    /// MapToolUI + StageEditorUI 를 에디터 모드에서 빌드해 씬/프리팹에 굽는 툴.
    ///
    /// ── 메뉴 ──────────────────────────────────────────────────────────
    /// Underdark/Bake Map Tool UI to Scene
    ///   → 현재 씬 Hierarchy에 MapToolCanvas 생성 (즉시 Inspector 편집 가능)
    ///
    /// Underdark/Bake Map Tool UI to Prefab
    ///   → Assets/Prefabs/UI/PF_MapToolUI.prefab 로 저장
    ///     런타임에 MapToolUI.Start()가 이 프리팹을 Instantiate
    ///
    /// Underdark/Remove Baked Map Tool UI
    ///   → 씬에서 제거
    /// </summary>
    public static class MapToolUIBaker
    {
        private const string PrefabPath = "Assets/Prefabs/UI/PF_MapToolUI.prefab";

        // ──────────────────────────────────────────────────────────────
        [MenuItem("Underdark/Bake Map Tool UI to Scene")]
        public static void BakeToScene()
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

            // _tool + _stageUI 초기화 후 BuildUI 호출
            uiComp.InitForEditor();
            uiComp.BuildUI();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[MapToolUIBaker] Bake to Scene 완료 — MapToolCanvas를 Hierarchy에서 확인하세요.");
            EditorUtility.DisplayDialog("씬 Bake 완료",
                "MapToolCanvas가 Hierarchy에 생성됐어요!\n\n" +
                "이제 Inspector에서 직접:\n" +
                "• RectTransform으로 크기/위치 조절\n" +
                "• Image로 배경색 변경\n" +
                "• TextMeshProUGUI로 폰트/색상 변경\n\n" +
                "만족스러우면 Ctrl+S 씬 저장 후\n" +
                "[Bake to Prefab]으로 프리팹도 저장하세요.", "OK");
        }

        // ──────────────────────────────────────────────────────────────
        [MenuItem("Underdark/Bake Map Tool UI to Prefab")]
        public static void BakeToPrefab()
        {
            var uiComp = Object.FindObjectOfType<MapToolUI>();
            if (uiComp == null)
            {
                EditorUtility.DisplayDialog("Error", "MapToolUI component not found in scene!", "OK");
                return;
            }

            // 기존 임시 Canvas 제거 후 새로 빌드
            var existing = GameObject.Find("MapToolCanvas");
            if (existing != null) DestroyImmediate(existing);
            var existingES = GameObject.Find("EventSystem");
            if (existingES != null) DestroyImmediate(existingES);

            uiComp.InitForEditor();
            uiComp.BuildUI();

            var canvas = GameObject.Find("MapToolCanvas");
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "BuildUI 후 MapToolCanvas를 찾지 못했습니다.", "OK");
                return;
            }

            // 저장 폴더 확인
            EnsureFolder("Assets/Prefabs/UI");

            // 프리팹으로 저장
            bool success;
            PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath, out success);

            // 씬에 임시로 만든 Canvas는 제거 (프리팹만 남김)
            DestroyImmediate(canvas);
            var es = GameObject.Find("EventSystem");
            if (es != null) DestroyImmediate(es);

            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            if (success)
            {
                Debug.Log($"[MapToolUIBaker] 프리팹 저장 완료: {PrefabPath}");
                EditorUtility.DisplayDialog("프리팹 Bake 완료",
                    $"저장 경로:\n{PrefabPath}\n\n" +
                    "런타임에 MapToolUI가 이 프리팹을 자동으로 Instantiate합니다.\n" +
                    "Inspector에서 위치/크기를 수정하고\n다시 [Bake to Prefab]을 실행하면 반영됩니다.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("저장 실패", $"프리팹 저장에 실패했습니다.\n{PrefabPath}", "OK");
            }
        }

        // ──────────────────────────────────────────────────────────────
        [MenuItem("Underdark/Remove Baked Map Tool UI")]
        public static void RemoveUI()
        {
            var c = GameObject.Find("MapToolCanvas");
            if (c != null) { Undo.DestroyObjectImmediate(c); Debug.Log("[MapToolUIBaker] MapToolCanvas 제거됨"); }
            var es = GameObject.Find("EventSystem");
            if (es != null) Undo.DestroyObjectImmediate(es);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // ──────────────────────────────────────────────────────────────
        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
