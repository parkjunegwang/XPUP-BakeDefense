using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// Underdark/Bake Lobby Scene 메뉴 실행 시
    /// LobbyScene.unity 를 처음부터 완전하게 구성합니다.
    /// 모든 UI 오브젝트가 씬에 실제로 존재하므로 Hierarchy에서
    /// 위치/크기를 자유롭게 수정할 수 있습니다.
    /// LobbyUI 컴포넌트의 참조 필드도 자동 연결됩니다.
    /// </summary>
    public static class LobbySceneBaker
    {
        // ── 색상 테마 ────────────────────────────────────────────────
        static readonly Color BG_DARK  = new Color(0.08f, 0.08f, 0.14f);
        static readonly Color BG_CARD  = new Color(0.15f, 0.15f, 0.25f);
        static readonly Color COL_CLEAR = new Color(0.25f, 0.85f, 0.40f);
        static readonly Color COL_AVAIL = new Color(0.30f, 0.65f, 1.00f);
        static readonly Color COL_LOCK  = new Color(0.35f, 0.35f, 0.40f);
        static readonly Color COL_GOLD  = new Color(1.00f, 0.85f, 0.20f);
        static readonly Color COL_GEM   = new Color(0.50f, 0.85f, 1.00f);
        static readonly Color COL_PLAY  = new Color(0.15f, 0.70f, 0.25f);

        // ── 레이아웃 수치 ────────────────────────────────────────────
        const float HUD_H      = 56f;
        const float BOTTOM_H   = 110f;
        const float SIDE_W     = 90f;
        const float CARD_W     = 300f;
        const float CARD_GAP   = 16f;
        const float REF_W      = 390f;
        const float REF_H      = 844f;

        // ────────────────────────────────────────────────────────────

        [MenuItem("Underdark/Bake Lobby Scene")]
        public static void BakeLobbyScene()
        {
            string scenePath = "Assets/Scenes/LobbyScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 기존 오브젝트 전부 제거
            foreach (var r in scene.GetRootGameObjects())
                Object.DestroyImmediate(r);

            // ── 카메라 ──────────────────────────────────────────────
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam   = camGo.AddComponent<Camera>();
            cam.backgroundColor = BG_DARK;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.orthographic    = true;

            // ── Canvas ──────────────────────────────────────────────
            var canvasGo = new GameObject("Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(REF_W, REF_H);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── 배경 ────────────────────────────────────────────────
            var bg = MakeImg(canvasGo, "Background", BG_DARK);
            Stretch(bg.rectTransform);

            // ── TopHUD ──────────────────────────────────────────────
            var hudGo  = MakePanel(canvasGo, "TopHUD",
                new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1),
                Vector2.zero, new Vector2(0, HUD_H),
                new Color(0.06f, 0.06f, 0.12f, 0.97f));
            // 하단 구분선
            var hudLine = MakeImg(hudGo, "Line", new Color(1,1,1,0.08f));
            hudLine.rectTransform.anchorMin = new Vector2(0,0);
            hudLine.rectTransform.anchorMax = new Vector2(1,0);
            hudLine.rectTransform.pivot     = new Vector2(0.5f,1);
            hudLine.rectTransform.anchoredPosition = Vector2.zero;
            hudLine.rectTransform.sizeDelta = new Vector2(0,1);

            // 골드 아이콘+텍스트
            var goldGo = MakeRectChild(hudGo, "Gold");
            SetAnchors(goldGo, new Vector2(0.03f,0), new Vector2(0.36f,1));
            var goldTmp = AddTMP(goldGo, "GoldText", "💰  0",
                COL_GOLD, 18, TextAlignmentOptions.MidlineLeft);

            // 젬 아이콘+텍스트
            var gemGo = MakeRectChild(hudGo, "Gem");
            SetAnchors(gemGo, new Vector2(0.36f,0), new Vector2(0.68f,1));
            var gemTmp = AddTMP(gemGo, "GemText", "💎  0",
                COL_GEM, 18, TextAlignmentOptions.MidlineLeft);

            // 설정 버튼
            var settingGo = MakeRectChild(hudGo, "BtnSetting");
            var settingRt = settingGo.GetComponent<RectTransform>();
            settingRt.anchorMin = new Vector2(1,0.5f);
            settingRt.anchorMax = new Vector2(1,0.5f);
            settingRt.pivot     = new Vector2(1,0.5f);
            settingRt.anchoredPosition = new Vector2(-10,0);
            settingRt.sizeDelta = new Vector2(44,44);
            MakeButton(settingGo, "⚙", new Color(0.2f,0.2f,0.3f), 18);

            // ── 좌 사이드 (상점) ────────────────────────────────────
            var shopGo = MakeRectChild(canvasGo, "BtnShop");
            var shopRt = shopGo.GetComponent<RectTransform>();
            shopRt.anchorMin = new Vector2(0,1); shopRt.anchorMax = new Vector2(0,1);
            shopRt.pivot     = new Vector2(0,1);
            shopRt.anchoredPosition = new Vector2(8, -(HUD_H + 8));
            shopRt.sizeDelta = new Vector2(SIDE_W, 70);
            var shopBtn = MakeButton(shopGo, "🛒\n상점", new Color(0.20f,0.45f,0.80f), 14);

            // ── 우 사이드 (컬렉션) ──────────────────────────────────
            var colGo = MakeRectChild(canvasGo, "BtnCollection");
            var colRt = colGo.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(1,1); colRt.anchorMax = new Vector2(1,1);
            colRt.pivot     = new Vector2(1,1);
            colRt.anchoredPosition = new Vector2(-8, -(HUD_H + 8));
            colRt.sizeDelta = new Vector2(SIDE_W, 70);
            var colBtn = MakeButton(colGo, "📦\n컬렉션", new Color(0.55f,0.25f,0.75f), 14);

            // ── StageArea (스크롤 + 스테이지 정보) ─────────────────
            var areaGo = MakeRectChild(canvasGo, "StageArea");
            var areaRt = areaGo.GetComponent<RectTransform>();
            areaRt.anchorMin = new Vector2(0, 0);
            areaRt.anchorMax = new Vector2(1, 1);
            areaRt.offsetMin = new Vector2(0, BOTTOM_H);
            areaRt.offsetMax = new Vector2(0, -HUD_H);

            // 스테이지 이름 (위)
            var stageInfoGo = MakeRectChild(areaGo, "StageInfo");
            SetAnchors(stageInfoGo, new Vector2(0,0.80f), Vector2.one);
            var stageNameTmp = AddTMP(stageInfoGo, "StageName", "Stage 1",
                Color.white, 22, TextAlignmentOptions.Center);
            stageNameTmp.fontStyle = FontStyles.Bold;

            var stageInfoPanel = MakeRectChild(areaGo, "StageInfoSub");
            SetAnchors(stageInfoPanel, new Vector2(0,0.65f), new Vector2(1,0.80f));
            var stageInfoTmp = AddTMP(stageInfoPanel, "StageInfoText", "3 Waves",
                new Color(0.7f,0.7f,0.8f), 14, TextAlignmentOptions.Center);

            // ── ScrollRect ─────────────────────────────────────────
            var scrollGo = MakeRectChild(areaGo, "StageScroll");
            SetAnchors(scrollGo, Vector2.zero, new Vector2(1, 0.65f));
            scrollGo.AddComponent<Image>().color = Color.clear;
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal        = true;
            scrollRect.vertical          = false;
            scrollRect.inertia           = true;
            scrollRect.decelerationRate  = 0.12f;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType      = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity        = 0.1f;

            // Viewport (RectMask2D = Image alpha 불필요, rect 경계로만 클리핑)
            var vpGo = MakeRectChild(scrollGo, "Viewport");
            Stretch(vpGo.GetComponent<RectTransform>());
            vpGo.AddComponent<RectMask2D>();
            scrollRect.viewport = vpGo.GetComponent<RectTransform>();

            // Content
            var contentGo = MakeRectChild(vpGo, "Content");
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0,0); contentRt.anchorMax = new Vector2(0,1);
            contentRt.pivot     = new Vector2(0,0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            scrollRect.content = contentRt;

            // ── 스테이지 카드 생성 (StageRegistry 기반) ────────────
            // Registry가 없으면 자동 스캔해서 생성
            // 카드는 런타임에 LobbyUI가 StageCardPrefab으로 생성함
            contentRt.sizeDelta = new Vector2(0, 0);

            // 좌우 화살표 힌트
            MakeArrow(areaGo, "ArrowL", "◀", new Vector2(0.01f, 0.32f));
            MakeArrow(areaGo, "ArrowR", "▶", new Vector2(0.99f, 0.32f));

            // ── BottomBar ──────────────────────────────────────────
            var barGo = MakePanel(canvasGo, "BottomBar",
                new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0),
                Vector2.zero, new Vector2(0, BOTTOM_H),
                new Color(0.06f, 0.06f, 0.12f, 0.97f));
            // 상단 구분선
            var barLine = MakeImg(barGo, "Line", new Color(1,1,1,0.08f));
            barLine.rectTransform.anchorMin = new Vector2(0,1);
            barLine.rectTransform.anchorMax = Vector2.one;
            barLine.rectTransform.pivot     = new Vector2(0.5f,1);
            barLine.rectTransform.anchoredPosition = Vector2.zero;
            barLine.rectTransform.sizeDelta = new Vector2(0,1);

            // 플레이 버튼
            var playGo = MakeRectChild(barGo, "BtnPlay");
            var playRt = playGo.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.5f,0.5f);
            playRt.anchorMax = new Vector2(0.5f,0.5f);
            playRt.pivot     = new Vector2(0.5f,0.5f);
            playRt.anchoredPosition = new Vector2(0, 5);
            playRt.sizeDelta = new Vector2(260, 56);
            var playBtn = MakeButton(playGo, "▶  PLAY", COL_PLAY, 22);
            var playLabel = playGo.GetComponentInChildren<TextMeshProUGUI>();
            if (playLabel != null) playLabel.fontStyle = FontStyles.Bold;

            // ── EventSystem ─────────────────────────────────────────
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // ── LobbyUI 컨트롤러 (참조 자동 연결) ──────────────────
            var lobbyGo = new GameObject("LobbyUI");
            var lobbyUI = lobbyGo.AddComponent<LobbyUI>();
            lobbyUI.towerSelectScene = "TowerSelectScene";
            lobbyUI.goldText         = goldTmp;
            lobbyUI.gemText          = gemTmp;
            lobbyUI.stageNameText    = stageNameTmp;
            lobbyUI.stageInfoText    = stageInfoTmp;
            lobbyUI.stageScroll      = scrollRect;
            lobbyUI.scrollContent    = contentRt;
            lobbyUI.playButton       = playBtn;
            lobbyUI.playButtonLabel  = playLabel;

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[LobbySceneBaker] LobbyScene 빌드 완료! 오브젝트 직접 수정 가능.");
        }

        // ── 스테이지 카드 ────────────────────────────────────────────

        static void CreateStageCard(GameObject contentGo, int idx, StageData sd)
        {
            bool cleared  = SaveData.IsCleared(idx);
            bool unlocked = SaveData.IsUnlocked(idx);

            float x = CARD_GAP + idx * (CARD_W + CARD_GAP);

            var go = MakeRectChild(contentGo, $"Card_{idx}");
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,0); rt.anchorMax = new Vector2(0,1);
            rt.pivot     = new Vector2(0,0.5f);
            rt.sizeDelta = new Vector2(CARD_W, 0);
            rt.anchoredPosition = new Vector2(x, 0);

            go.AddComponent<Image>().color = BG_CARD;

            var ol = go.AddComponent<Outline>();
            ol.effectColor    = cleared ? COL_CLEAR : unlocked ? COL_AVAIL : COL_LOCK;
            ol.effectDistance = new Vector2(2,-2);

            // 썸네일 영역 (상단 60%)
            var thumbGo = MakeRectChild(go, "Thumb");
            SetAnchors(thumbGo, new Vector2(0,0.40f), Vector2.one);
            var thumbImg = thumbGo.AddComponent<Image>();

            if (!unlocked)
            {
                thumbImg.color = new Color(0.1f,0.1f,0.15f);
                var qTmp = AddTMP(thumbGo, "Lock", "?", new Color(0.4f,0.4f,0.5f),
                    72, TextAlignmentOptions.Center);
                qTmp.fontStyle = FontStyles.Bold;
            }
            else if (sd.thumbnail != null)
            {
                thumbImg.sprite = sd.thumbnail;
                thumbImg.preserveAspect = true;
                thumbImg.color = Color.white;
            }
            else
            {
                thumbImg.color = new Color(0.18f,0.20f,0.35f);
                var numTmp = AddTMP(thumbGo, "StageNum", $"STAGE\n{idx+1}",
                    new Color(0.5f,0.6f,0.9f,0.6f), 36, TextAlignmentOptions.Center);
                numTmp.fontStyle = FontStyles.Bold;
            }

            // 정보 영역 (하단 40%)
            var infoGo = MakeRectChild(go, "Info");
            SetAnchors(infoGo, Vector2.zero, new Vector2(1,0.40f));
            var infoRt = infoGo.GetComponent<RectTransform>();
            infoRt.offsetMin = new Vector2(10,8); infoRt.offsetMax = new Vector2(-10,-4);

            // 이름
            string nameStr = unlocked ? sd.stageName : $"Stage {idx+1}";
            var nameTmp = AddTMP(infoGo, "Name", nameStr,
                Color.white, 18, TextAlignmentOptions.MidlineLeft);
            nameTmp.fontStyle = FontStyles.Bold;
            SetAnchors(nameTmp.gameObject, new Vector2(0,0.55f), Vector2.one);

            // 웨이브 수
            string waveStr = unlocked ? $"{sd.waves?.Count ?? 0} Waves" : "???";
            var waveTmp = AddTMP(infoGo, "Waves", waveStr,
                new Color(0.65f,0.65f,0.75f), 13, TextAlignmentOptions.MidlineLeft);
            SetAnchors(waveTmp.gameObject, Vector2.zero, new Vector2(1,0.55f));

            // 상태 뱃지 (우측)
            string statusStr = cleared ? "✓ CLEAR" : unlocked ? "PLAY" : "🔒";
            Color  statusCol = cleared ? COL_CLEAR : unlocked ? COL_AVAIL : COL_LOCK;
            var badgeGo = MakeRectChild(infoGo, "Badge");
            SetAnchors(badgeGo, new Vector2(0.68f,0.55f), Vector2.one);
            badgeGo.AddComponent<Image>().color =
                new Color(statusCol.r*0.3f, statusCol.g*0.3f, statusCol.b*0.3f, 0.8f);
            var badgeTmp = AddTMP(badgeGo, "Status", statusStr,
                statusCol, 13, TextAlignmentOptions.Center);
            badgeTmp.fontStyle = FontStyles.Bold;

            // 버튼 (카드 전체 클릭 가능)
            go.AddComponent<Button>();
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────

        static GameObject MakePanel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 pos, Vector2 size, Color col)
        {
            var go = MakeRectChild(parent, name);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot     = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = col;
            return go;
        }

        static Image MakeImg(GameObject parent, string name, Color col)
        {
            var go = MakeRectChild(parent, name);
            var img = go.AddComponent<Image>();
            img.color = col;
            return img;
        }

        static Button MakeButton(GameObject go, string label, Color bgCol, int fontSize)
        {
            if (go.GetComponent<Image>() == null)
                go.AddComponent<Image>().color = bgCol;
            else
                go.GetComponent<Image>().color = bgCol;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f,1.15f,1.15f);
            colors.pressedColor     = new Color(0.80f,0.80f,0.80f);
            btn.colors = colors;

            var lblGo = MakeRectChild(go, "Label");
            Stretch(lblGo.GetComponent<RectTransform>());
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        static TextMeshProUGUI AddTMP(GameObject parent, string name, string text,
            Color col, int fontSize, TextAlignmentOptions align)
        {
            var go = MakeRectChild(parent, name);
            Stretch(go.GetComponent<RectTransform>());
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text           = text;
            tmp.fontSize       = fontSize;
            tmp.color          = col;
            tmp.alignment      = align;
            tmp.raycastTarget  = false;
            return tmp;
        }

        static void MakeArrow(GameObject parent, string name, string arrow, Vector2 anchor)
        {
            var go = MakeRectChild(parent, name);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot     = new Vector2(0.5f,0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(28, 40);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = arrow; tmp.fontSize = 18;
            tmp.color = new Color(1,1,1,0.3f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        static GameObject MakeRectChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ── Add StageManager to GameScene ───────────────────────────

        [MenuItem("Underdark/Add StageManager to GameScene")]
        public static void AddStageManagerToGameScene()
        {
            string gamePath = "Assets/Scenes/GameScene.unity";
            var scene = EditorSceneManager.OpenScene(gamePath, OpenSceneMode.Single);

            if (Object.FindObjectOfType<StageManager>() != null)
            {
                Debug.Log("[LobbySceneBaker] StageManager 이미 존재함.");
                EditorSceneManager.SaveScene(scene);
                return;
            }

            var go = new GameObject("StageManager");
            var sm = go.AddComponent<StageManager>();
            sm.lobbySceneName = "LobbyScene";

            // stages는 런타임에 StageRegistry에서 자동 로드됨

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LobbySceneBaker] StageManager를 GameScene에 추가 완료!");
        }
    }
}
