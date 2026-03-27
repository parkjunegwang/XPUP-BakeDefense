using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    public static class TowerSelectSceneBaker
    {
        [MenuItem("Underdark/Bake TowerSelect Scene")]
        public static void Bake()
        {
            string path = "Assets/Scenes/TowerSelectScene.unity";
            var scene = System.IO.File.Exists(path)
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            foreach (var r in scene.GetRootGameObjects())
                Object.DestroyImmediate(r);

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor  = new Color(0.06f, 0.06f, 0.12f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.orthographic     = true;

            // Canvas
            var cvGo = new GameObject("Canvas");
            var cv   = cvGo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            var cvs = cvGo.AddComponent<CanvasScaler>();
            cvs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cvs.referenceResolution = new Vector2(1080, 1920);
            cvs.matchWidthOrHeight  = 0.5f;
            cvGo.AddComponent<GraphicRaycaster>();

            // Background
            MakeStretch(cvGo, "BG").AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 1f);

            // Title
            var titleGo = MakeAnchor(cvGo, "Title", 0f, 0.90f, 1f, 0.98f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Select Your Towers";
            titleTmp.fontSize = 48f; titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.9f, 0.75f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Count text
            var countGo = MakeAnchor(cvGo, "CountText", 0.1f, 0.83f, 0.9f, 0.90f);
            var countTmp = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text = "Select 0 / 4";
            countTmp.fontSize = 28f;
            countTmp.color = new Color(0.7f, 0.75f, 1f);
            countTmp.alignment = TextAlignmentOptions.Center;

            // Scroll View
            var scrollGo = MakeAnchor(cvGo, "ScrollView", 0.02f, 0.15f, 0.98f, 0.83f);
            var scrollBg = scrollGo.AddComponent<Image>(); scrollBg.color = new Color(0, 0, 0, 0.01f);
            var scrollView = scrollGo.AddComponent<ScrollRect>();
            scrollView.horizontal = false; scrollView.vertical = true;
            scrollView.scrollSensitivity = 40f;

            // Viewport
            var vpGo = MakeStretch(scrollGo, "Viewport");
            var vpImg = vpGo.AddComponent<Image>(); vpImg.color = new Color(1f, 1f, 1f, 1f);
            var mask  = vpGo.AddComponent<Mask>(); mask.showMaskGraphic = false;
            scrollView.viewport = vpGo.GetComponent<RectTransform>();

            // Content (GridLayoutGroup for 2-column card grid)
            var contentGo = MakeStretch(vpGo, "Content");
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot     = new Vector2(0.5f, 1f);
            contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;

            var glg = contentGo.AddComponent<GridLayoutGroup>();
            glg.cellSize        = new Vector2(490f, 130f);
            glg.spacing         = new Vector2(16f, 14f);
            glg.padding         = new RectOffset(18, 18, 18, 18);
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment  = TextAnchor.UpperCenter;

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            scrollView.content = contentRect;

            // Back button (bottom-left)
            var backGo = MakeAnchor(cvGo, "BackBtn", 0.02f, 0.03f, 0.22f, 0.13f);
            var backImg = backGo.AddComponent<Image>(); backImg.color = new Color(0.3f, 0.3f, 0.4f, 1f);
            backGo.AddComponent<Button>(); // onClick wired at runtime by TowerSelectUI
            var backTxt = MakeStretch(backGo, "Txt").AddComponent<TextMeshProUGUI>();
            backTxt.text = "<  Back"; backTxt.fontSize = 26f;
            backTxt.color = Color.white; backTxt.alignment = TextAlignmentOptions.Center;

            // Confirm button (bottom-center)
            var confGo = MakeAnchor(cvGo, "ConfirmBtn", 0.25f, 0.03f, 0.98f, 0.13f);
            var confImg = confGo.AddComponent<Image>(); confImg.color = new Color(0.15f, 0.55f, 0.25f, 1f);
            confGo.AddComponent<Button>(); // onClick wired at runtime by TowerSelectUI
            var confTxt = MakeStretch(confGo, "Txt").AddComponent<TextMeshProUGUI>();
            confTxt.text = "Start Game!"; confTxt.fontSize = 32f;
            confTxt.fontStyle = FontStyles.Bold;
            confTxt.color = Color.white; confTxt.alignment = TextAlignmentOptions.Center;

            // TowerSelectUI component — buttons wired at runtime via Start()
            var uiGo = new GameObject("TowerSelectUI");
            uiGo.transform.SetParent(cvGo.transform, false);
            uiGo.AddComponent<RectTransform>();
            var ui = uiGo.AddComponent<TowerSelectUI>();
            ui.cardContainer  = contentRect;
            ui.confirmButton  = confGo.GetComponent<Button>();
            ui.backButton     = backGo.GetComponent<Button>();
            ui.titleText      = titleTmp;
            ui.countText      = countTmp;
            ui.gameSceneName  = "GameScene";
            ui.lobbySceneName = "LobbyScene";

            // Inject stage assets
            var s1 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage1.asset");
            var s2 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage2.asset");
            var s3 = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage3.asset");
            ui.stages = new List<StageData> { s1, s2, s3 };

            // EventSystem
            var evGo = new GameObject("EventSystem");
            evGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[TowerSelectSceneBaker] TowerSelectScene done!");

            // Add to build settings if missing
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in scenes) if (s.path == path) { found = true; break; }
            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        static GameObject MakeStretch(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        static GameObject MakeAnchor(GameObject parent, string name,
            float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }
    }
}
