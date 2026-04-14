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
    ///
    /// ── 자동 동작 ─────────────────────────────────────────────────────
    /// ToolScene이 열릴 때 MapToolCanvas는 있지만 StagePanel이 없으면
    /// 자동으로 StagePanel을 추가하고 씬을 dirty 처리합니다.
    /// </summary>
    [InitializeOnLoad]
    public static class MapToolUIBaker
    {
        private const string PrefabPath   = "Assets/Prefabs/UI/PF_MapToolUI.prefab";
        private const string ToolSceneName = "ToolScene";

        // ── 씬 열림 자동 감지 ──────────────────────────────────────────
        static MapToolUIBaker()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.name != ToolSceneName) return;
            // 씬이 열린 직후 바로 실행하면 오브젝트 참조가 불안정할 수 있으므로 한 프레임 뒤에 실행
            EditorApplication.delayCall += EnsureStagePanelInScene;
        }

        /// <summary>
        /// ToolScene에 MapToolCanvas는 있지만 StagePanel이 없으면 자동으로 추가.
        /// </summary>
        private static void EnsureStagePanelInScene()
        {
            // ToolScene이 아니면 스킵
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != ToolSceneName) return;

            var canvas = GameObject.Find("MapToolCanvas");
            if (canvas == null) return; // Canvas 자체가 없으면 스킵

            var stagePanel = canvas.transform.Find("StagePanel");
            if (stagePanel != null) return; // 이미 있으면 스킵

            // StagePanel이 없으면 자동 Bake
            Debug.Log("[MapToolUIBaker] ToolScene에 StagePanel이 없어 자동으로 추가합니다.");
            BakeStagePanelOnly();
        }

        /// <summary>
        /// StagePanel만 MapToolCanvas에 추가 (MapPanels는 건드리지 않음).
        /// </summary>
        private static void BakeStagePanelOnly()
        {
            var uiComp = Object.FindFirstObjectByType<MapToolUI>();
            if (uiComp == null) return;

            uiComp.InitForEditor();
            uiComp.BuildStagePanel(startActive: false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[MapToolUIBaker] StagePanel 자동 추가 완료 — Ctrl+S로 씬 저장하세요.");
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
