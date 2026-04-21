using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Underdark
{
    public static class CreateSeasonPassPrefab
    {
        [MenuItem("Underdark/Create SeasonPass Prefab")]
        public static void Create()
        {
            // ── 저장 폴더 확인 ───────────────────────────────────────
            const string folder = "Assets/Resources/Popups";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Popups");
            }

            // ── 루트 ─────────────────────────────────────────────────
            var root = new GameObject("PF_SeasonPassPopup");
            root.AddComponent<RectTransform>();
            var cg   = root.AddComponent<CanvasGroup>();
            var sp   = root.AddComponent<SeasonPassPopup>();

            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(800, 600);

            // ── BG (어두운 반투명 배경 + 닫기 버튼) ──────────────────
            var bg = MakeGO("BG", root.transform);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.65f);
            var bgBtn = bg.AddComponent<Button>();
            // BG 클릭 = 닫기
            // (런타임에 SeasonPassPopup.Awake에서 closeButton이 연결되므로 별도 처리 불필요)

            // ── Panel ────────────────────────────────────────────────
            var panel = MakeGO("Panel", root.transform);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot     = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(520, 700);
            panelRt.anchoredPosition = Vector2.zero;
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.10f, 0.10f, 0.14f, 1f);

            // ── Header ────────────────────────────────────────────────
            var header = MakeGO("Header", panel.transform);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot     = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 110f);
            headerRt.anchoredPosition = Vector2.zero;
            var headerImg = header.AddComponent<Image>();
            headerImg.color = new Color(0.15f, 0.15f, 0.20f, 1f);

            // Title
            var titleGo = MakeGO("Title", header.transform);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.55f);
            titleRt.anchorMax = new Vector2(0.70f, 1.00f);
            titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text      = "시즌 패스";
            titleTmp.fontSize  = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color     = Color.white;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // LevelText
            var lvGo = MakeGO("LevelText", header.transform);
            var lvRt = lvGo.GetComponent<RectTransform>();
            lvRt.anchorMin = new Vector2(0.05f, 0.10f);
            lvRt.anchorMax = new Vector2(0.55f, 0.55f);
            lvRt.offsetMin = lvRt.offsetMax = Vector2.zero;
            var lvTmp = lvGo.AddComponent<TextMeshProUGUI>();
            lvTmp.text     = "LV. 0 / 20";
            lvTmp.fontSize = 18;
            lvTmp.color    = new Color(0.8f, 0.8f, 0.8f);
            lvTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // PassStatusText
            var statusGo = MakeGO("PassStatus", header.transform);
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.55f, 0.10f);
            statusRt.anchorMax = new Vector2(0.88f, 0.55f);
            statusRt.offsetMin = statusRt.offsetMax = Vector2.zero;
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.text     = "무료 패스";
            statusTmp.fontSize = 16;
            statusTmp.color    = new Color(0.7f, 0.7f, 0.7f);
            statusTmp.alignment = TextAlignmentOptions.MidlineRight;

            // BuyPassButton
            var buyGo = MakeGO("BuyPassButton", header.transform);
            var buyRt = buyGo.GetComponent<RectTransform>();
            buyRt.anchorMin = new Vector2(0.60f, 0.08f);
            buyRt.anchorMax = new Vector2(0.88f, 0.52f);
            buyRt.offsetMin = buyRt.offsetMax = Vector2.zero;
            var buyImg = buyGo.AddComponent<Image>();
            buyImg.color = new Color(0.9f, 0.6f, 0.1f, 1f);
            var buyBtn = buyGo.AddComponent<Button>();
            buyBtn.targetGraphic = buyImg;
            var buyTextGo = MakeGO("Text", buyGo.transform);
            var buyTextRt = buyTextGo.GetComponent<RectTransform>();
            buyTextRt.anchorMin = Vector2.zero;
            buyTextRt.anchorMax = Vector2.one;
            buyTextRt.offsetMin = buyTextRt.offsetMax = Vector2.zero;
            var buyTmp = buyTextGo.AddComponent<TextMeshProUGUI>();
            buyTmp.text      = "패스 구매";
            buyTmp.fontSize  = 14;
            buyTmp.fontStyle = FontStyles.Bold;
            buyTmp.color     = Color.white;
            buyTmp.alignment = TextAlignmentOptions.Center;
            buyTmp.raycastTarget = false;

            // CloseButton
            var closeGo = MakeGO("CloseButton", header.transform);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.88f, 0.50f);
            closeRt.anchorMax = new Vector2(1.00f, 1.00f);
            closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.7f, 0.2f, 0.2f, 1f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            var closeTextGo = MakeGO("Text", closeGo.transform);
            var closeTextRt = closeTextGo.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.offsetMin = closeTextRt.offsetMax = Vector2.zero;
            var closeTmp = closeTextGo.AddComponent<TextMeshProUGUI>();
            closeTmp.text      = "✕";
            closeTmp.fontSize  = 20;
            closeTmp.color     = Color.white;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.raycastTarget = false;

            // ── Column Header ─────────────────────────────────────────
            var colHdr = MakeGO("ColHeader", panel.transform);
            var colRt  = colHdr.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0f, 1f);
            colRt.anchorMax = new Vector2(1f, 1f);
            colRt.pivot     = new Vector2(0.5f, 1f);
            colRt.sizeDelta = new Vector2(0f, 32f);
            colRt.anchoredPosition = new Vector2(0f, -110f);
            var colImg = colHdr.AddComponent<Image>();
            colImg.color = new Color(0.12f, 0.12f, 0.17f, 1f);

            MakeHeaderLabel("LV",    colHdr.transform, 0f,    0.10f);
            MakeHeaderLabel("무료 보상", colHdr.transform, 0.11f, 0.55f);
            MakeHeaderLabel("유료 보상", colHdr.transform, 0.57f, 1.00f);

            // ── ScrollRect ───────────────────────────────────────────
            var scrollGo = MakeGO("Scroll", panel.transform);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(0f,  4f);
            scrollRt.offsetMax = new Vector2(0f, -142f);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            // Viewport
            var vpGo = MakeGO("Viewport", scrollGo.transform);
            var vpRt = vpGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            var vpImg = vpGo.AddComponent<Image>();
            vpImg.color = Color.white;
            var mask = vpGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            var contentGo = MakeGO("Content", vpGo.transform);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            contentRt.anchoredPosition = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ScrollRect 연결
            scrollRect.viewport = vpRt;
            scrollRect.content  = contentRt;

            // ── SeasonPassPopup 필드 연결 ─────────────────────────────
            sp.closeButton    = closeBtn;
            sp.levelText      = lvTmp;
            sp.passStatusText = statusTmp;
            sp.buyPassButton  = buyBtn;
            sp.rowContainer   = contentGo.transform;
            sp.scrollRect     = scrollRect;

            // ── 프리팹 저장 ───────────────────────────────────────────
            const string prefabPath = "Assets/Resources/Popups/PF_SeasonPassPopup.prefab";
            bool success;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
            Object.DestroyImmediate(root);

            if (success)
                Debug.Log($"[CreateSeasonPassPrefab] 저장 완료: {prefabPath}");
            else
                Debug.LogError("[CreateSeasonPassPrefab] 프리팹 저장 실패!");
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────

        private static GameObject MakeGO(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void MakeHeaderLabel(string text, Transform parent, float left, float right)
        {
            var go = MakeGO("Lbl_" + text, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(left,  0f);
            rt.anchorMax = new Vector2(right, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = 12;
            tmp.color     = new Color(0.7f, 0.7f, 0.7f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
    }
}
