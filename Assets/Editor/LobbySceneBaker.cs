using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    public static class LobbySceneBaker
    {
        [MenuItem("Underdark/Bake Lobby Scene")]
        public static void BakeLobbyScene()
        {
            string lobbyPath = "Assets/Scenes/LobbyScene.unity";
            var scene = EditorSceneManager.OpenScene(lobbyPath, OpenSceneMode.Single);

            // 기존 오브젝트 제거
            foreach (var r in scene.GetRootGameObjects())
                Object.DestroyImmediate(r);

            // ── 카메라 ───────────────────────────────────────────────
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.14f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;

            // ── Canvas ───────────────────────────────────────────────
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── 배경 ─────────────────────────────────────────────────
            var bgGo = MakeRectChild(canvasGo.transform, "Background");
            AnchorFull(bgGo);
            bgGo.AddComponent<Image>().color = new Color(0.08f, 0.07f, 0.15f, 1f);

            // ── 타이틀 ───────────────────────────────────────────────
            var titleGo = MakeRectChild(canvasGo.transform, "Title");
            AnchorStretch(titleGo, 0f, 0.85f, 1f, 0.97f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>UNDERDARK</b>";
            titleTmp.fontSize = 72f;
            titleTmp.color = new Color(0.9f, 0.75f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            // ── 부제목 ───────────────────────────────────────────────
            var subGo = MakeRectChild(canvasGo.transform, "Subtitle");
            AnchorStretch(subGo, 0f, 0.78f, 1f, 0.86f);
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "스테이지 선택";
            subTmp.fontSize = 34f;
            subTmp.color = new Color(0.7f, 0.7f, 0.8f);
            subTmp.alignment = TextAlignmentOptions.Center;

            // ── ScrollView ───────────────────────────────────────────
            var scrollGo = MakeRectChild(canvasGo.transform, "StageScrollView");
            AnchorStretch(scrollGo, 0.05f, 0.06f, 0.95f, 0.78f);
            // ScrollRect에는 Image 필요 (클릭 영역)
            var scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.01f);
            var scrollView = scrollGo.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.scrollSensitivity = 30f;

            // Viewport - Mask용 Image는 알파 > 0 이어야 함
            var viewportGo = MakeRectChild(scrollGo.transform, "Viewport");
            AnchorFull(viewportGo);
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 1f); // 불투명해야 Mask 작동
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;               // 화면엔 안 보이게
            scrollView.viewport = viewportGo.GetComponent<RectTransform>();

            // Content
            var contentGo = MakeRectChild(viewportGo.transform, "Content");
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot     = new Vector2(0.5f, 1f);
            contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing              = 20f;
            vlg.padding              = new RectOffset(16, 16, 16, 16);
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth    = true;
            vlg.childControlHeight   = true;
            vlg.childAlignment       = TextAnchor.UpperCenter;

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollView.content = contentRect;

            // ── StageSelectUI ─────────────────────────────────────────
            var uiGo = MakeRectChild(canvasGo.transform, "StageSelectUI");
            var stageUI = uiGo.AddComponent<StageSelectUI>();
            stageUI.buttonContainer = contentRect;
            stageUI.gameSceneName   = "GameScene";

            var s1 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage1.asset");
            var s2 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage2.asset");
            var s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage3.asset");
            stageUI.stages = new List<StageData> { s1, s2, s3 };

            // ── EventSystem ───────────────────────────────────────────
            var eventGo = new GameObject("EventSystem");
            eventGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            EditorSceneManager.SaveScene(scene, lobbyPath);
            Debug.Log("[LobbySceneBaker] LobbyScene 빌드 완료!");
        }

        [MenuItem("Underdark/Add StageManager to GameScene")]
        public static void AddStageManagerToGameScene()
        {
            string gamePath = "Assets/Scenes/GameScene.unity";
            var scene = EditorSceneManager.OpenScene(gamePath, OpenSceneMode.Single);

            var existing = Object.FindObjectOfType<StageManager>();
            if (existing != null)
            {
                Debug.Log("[LobbySceneBaker] StageManager 이미 존재함.");
                EditorSceneManager.SaveScene(scene);
                return;
            }

            var go = new GameObject("StageManager");
            var sm = go.AddComponent<StageManager>();
            sm.lobbySceneName = "LobbyScene";

            var s1 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage1.asset");
            var s2 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage2.asset");
            var s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage3.asset");
            sm.stages = new List<StageData> { s1, s2, s3 };

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LobbySceneBaker] StageManager를 GameScene에 추가 완료!");
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────
        static GameObject MakeRectChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static void AnchorFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void AnchorStretch(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
