#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Underdark
{
    public static class GameUIBaker
    {
        [MenuItem("Underdark/Bake Game UI to Scene")]
        public static void BakeGameUI()
        {
            // ── 기존 GameCanvas 제거 ──────────────────────────────────
            var old = GameObject.Find("GameCanvas");
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
                Debug.Log("[GameUIBaker] 기존 GameCanvas 제거됨");
            }

            // ── GameSetup 찾기 ────────────────────────────────────────
            var setup = Object.FindObjectOfType<GameSetup>();
            if (setup == null)
            {
                EditorUtility.DisplayDialog("GameUIBaker",
                    "씬에 GameSetup 컴포넌트가 없습니다.\n--- MANAGERS --- 오브젝트를 확인해주세요.", "OK");
                return;
            }

            var ui = Object.FindObjectOfType<UIManager>();
            if (ui == null)
            {
                EditorUtility.DisplayDialog("GameUIBaker",
                    "씬에 UIManager 컴포넌트가 없습니다.", "OK");
                return;
            }

            // ── EventSystem 확인 ─────────────────────────────────────
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            // ── Canvas 생성 ───────────────────────────────────────────
            var cvGo = new GameObject("GameCanvas");
            Undo.RegisterCreatedObjectUndo(cvGo, "Bake GameCanvas");

            var cv = cvGo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 10;

            var cvs = cvGo.AddComponent<CanvasScaler>();
            cvs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cvs.referenceResolution = new Vector2(390, 844);
            cvs.matchWidthOrHeight = 0.5f;

            cvGo.AddComponent<GraphicRaycaster>();

            // ── UI 내용 구성 ──────────────────────────────────────────
            setup.BuildUIContents(cvGo, ui);

            // ── 씬 저장 플래그 ────────────────────────────────────────
            EditorUtility.SetDirty(cvGo);
            EditorUtility.SetDirty(ui);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[GameUIBaker] GameCanvas Bake 완료! Ctrl+S로 씬을 저장하세요.");
            EditorUtility.DisplayDialog("GameUIBaker",
                "GameCanvas Bake 완료!\n\nCtrl+S 로 씬을 저장하면 플레이 시 Canvas가 미리 생성되어 있습니다.", "OK");
        }

        [MenuItem("Underdark/Remove Baked Game UI")]
        public static void RemoveBakedUI()
        {
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("GameUIBaker", "GameCanvas가 없습니다.", "OK");
                return;
            }
            Undo.DestroyObjectImmediate(canvas);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[GameUIBaker] GameCanvas 제거됨");
        }
    }
}
#endif
