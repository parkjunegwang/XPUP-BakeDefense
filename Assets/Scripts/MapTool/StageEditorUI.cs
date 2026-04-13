using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Underdark
{
    /// <summary>
    /// StageData 비주얼 에디터 UI.
    /// 모든 RectTransform은 bottom-left anchor+pivot 기준 (anchoredPosition = 좌하단으로부터 offset).
    /// </summary>
    public class StageEditorUI : MonoBehaviour
    {
        private StageEditorController _ctrl;
        private GameObject _root;

        // ── 고정 레이아웃 크기 ─────────────────────────────────────────
        private float _sw, _sh;          // 전체 패널 크기 (Build 시 전달받음)
        private const float DETAIL_W  = 200f;
        private const float TOP_H     = 46f;
        private const float BOT_H     = 46f;
        private const float CELL_W    = 100f;
        private const float CELL_H    = 48f;
        private const float ROW_LBL_W = 72f;
        private const float COL_HDR_H = 24f;

        // ── UI 캐시 ───────────────────────────────────────────────────
        private TMP_InputField  _ifStageName;
        private TextMeshProUGUI _lblStatus;
        private GameObject      _sheetContent;   // ScrollRect Content
        private ScrollRect      _scrollRect;

        // 상세 패널
        private TextMeshProUGUI _lblDetail;
        private TMP_InputField  _ifHP, _ifSpeed, _ifCount, _ifInterval, _ifReward, _ifBossHpMult;
        private Toggle          _togBoss;
        private Button          _btnDelGroup, _btnDupGroup;

        // Auto-scale
        private TMP_InputField _ifSHP, _ifEHP, _ifSSPD, _ifESPD;

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _ctrl = GetComponent<StageEditorController>();
            if (_ctrl == null) _ctrl = gameObject.AddComponent<StageEditorController>();
        }

        private void Start()
        {
            _ctrl.onDataChanged      += RefreshSheet;
            _ctrl.onSelectionChanged += RefreshDetail;
            // Build()는 Start()보다 먼저 호출될 수 있으므로,
            // 현재 데이터가 있으면 즉시 갱신
            if (_ctrl?.currentStage != null)
                RefreshSheet();
        }

        private void OnDestroy()
        {
            if (_ctrl == null) return;
            _ctrl.onDataChanged      -= RefreshSheet;
            _ctrl.onSelectionChanged -= RefreshDetail;
        }

        // ─────────────────────────────────────────────────────────────
        #region Public API
        // ─────────────────────────────────────────────────────────────

        public void Build(GameObject canvasGo, float sw, float sh)
        {
            _sw = sw; _sh = sh;

            // 루트: 전체 영역을 덮는 배경
            _root = R(canvasGo, "StagePanel", 0, 0, sw, sh, new Color(0.07f,0.07f,0.15f,1f));
            _root.SetActive(false);

            BuildTop();
            BuildSheet();
            BuildDetail();
            BuildBottom();
            BuildAutoScale();

            _ctrl.NewStage();
        }

        public void Show() { if (_root) { _root.SetActive(true);  RefreshSheet(); } }
        public void Hide() { if (_root)   _root.SetActive(false); }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Layout Build
        // ─────────────────────────────────────────────────────────────

        // 상단 바: 스테이지 이름 / Save / Load / New
        void BuildTop()
        {
            float sheetW = _sw - DETAIL_W;
            var bar = R(_root, "Top", 0, _sh - TOP_H, sheetW, TOP_H, new Color(0.05f,0.05f,0.12f,1f));

            L(bar, "lStage", "Stage:", 4, TOP_H/2f, 54, TOP_H-4, 13, Color.yellow);
            _ifStageName = IF(bar, "ifName", "New Stage", 62, TOP_H/2f, 160, TOP_H-10);
            _ifStageName.contentType = TMP_InputField.ContentType.Standard;
            _ifStageName.onEndEdit.AddListener(v => _ctrl.SetStageName(v));

            float bx = sheetW - 10;
            B(bar, "New",  bx - 64,  TOP_H/2f, 62, TOP_H-10, new Color(0.45f,0.20f,0.08f), OnNew,  12);
            B(bar, "Load", bx - 132, TOP_H/2f, 62, TOP_H-10, new Color(0.10f,0.30f,0.65f), OnLoad, 12);
            B(bar, "Save", bx - 200, TOP_H/2f, 62, TOP_H-10, new Color(0.08f,0.52f,0.18f), () => _ctrl.SaveStage(), 12);

            // status 라벨: 버튼 왼쪽 공간 (스테이지 이름 IF 끝 ~ Save 버튼 시작 사이)
            float statusX = 62 + 166;           // ifName 끝 바로 뒤
            float statusW = (bx - 200) - statusX - 8; // Save 버튼 왼쪽까지
            _lblStatus = L(bar, "status", "", statusX, TOP_H/2f, Mathf.Max(statusW, 10f), TOP_H-4, 10, new Color(0.5f,1f,0.5f));
        }

        // 시트 영역 (ScrollRect)
        void BuildSheet()
        {
            float sheetW = _sw - DETAIL_W;
            float sheetH = _sh - TOP_H - BOT_H;
            float sheetY = BOT_H;

            var bg = R(_root, "SheetBg", 0, sheetY, sheetW, sheetH, new Color(0.06f,0.06f,0.14f,1f));

            // ScrollRect
            var svGo = new GameObject("SheetScroll");
            svGo.transform.SetParent(bg.transform, false);
            var svRt = svGo.AddComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero; svRt.anchorMax = Vector2.one;
            svRt.offsetMin = Vector2.zero; svRt.offsetMax = Vector2.zero;

            _scrollRect = svGo.AddComponent<ScrollRect>();
            _scrollRect.vertical = true; _scrollRect.horizontal = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(svGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            _scrollRect.viewport = vpRt;

            // Content — top-left pivot, Y grows downward
            _sheetContent = new GameObject("SheetContent");
            _sheetContent.transform.SetParent(vpGo.transform, false);
            var cRt = _sheetContent.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(0, 1);
            cRt.pivot     = new Vector2(0, 1);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(800, 400); // 초기값, RefreshSheet에서 갱신
            _sheetContent.AddComponent<Image>().color = Color.clear;
            _scrollRect.content = cRt;
        }

        // 우측 상세 패널
        void BuildDetail()
        {
            float panelH = _sh - BOT_H;
            var dp = R(_root, "DetailPanel", _sw - DETAIL_W, BOT_H, DETAIL_W, panelH,
                new Color(0.09f,0.09f,0.18f,1f));

            // 패널 내부는 top-left 기준으로 Y를 아래로 쌓음
            // → 헬퍼 DL/DIF/DB: y = panelH - (아래로 쌓인 픽셀)
            float y = panelH - 24f;

            _lblDetail = DL(dp, "hdr", "셀 선택하세요", DETAIL_W/2f, y, DETAIL_W-8, 22, 13, Color.yellow, true);
            y -= 30f;

            // 구분선
            HR(dp, y, DETAIL_W); y -= 8f;

            DL(dp, "lHP",  "HP",       8, y, 60, 22, 12, Color.white, false);
            _ifHP    = DIF(dp, "HP",   "0", DETAIL_W/2f, y, DETAIL_W/2f-6, 26, "hp");     y -= 32f;

            DL(dp, "lSpd", "Speed",    8, y, 60, 22, 12, Color.white, false);
            _ifSpeed = DIF(dp, "Spd",  "0", DETAIL_W/2f, y, DETAIL_W/2f-6, 26, "speed");  y -= 32f;

            DL(dp, "lCnt", "Count",    8, y, 60, 22, 12, Color.white, false);
            _ifCount = DIF(dp, "Cnt",  "0", DETAIL_W/2f, y, DETAIL_W/2f-6, 26, "count");  y -= 32f;

            DL(dp, "lItv", "Interval", 8, y, 70, 22, 12, Color.white, false);
            _ifInterval = DIF(dp, "Itv","0", DETAIL_W/2f, y, DETAIL_W/2f-6, 26, "interval"); y -= 32f;

            DL(dp, "lRwd", "Reward",   8, y, 60, 22, 12, Color.white, false);
            _ifReward = DIF(dp, "Rwd", "0", DETAIL_W/2f, y, DETAIL_W/2f-6, 26, "reward"); y -= 38f;

            HR(dp, y, DETAIL_W); y -= 8f;

            // Boss 토글
            DL(dp, "lBoss","Boss",     8, y, 48, 22, 12, Color.red, false);
            _togBoss = DTog(dp, y, 58, 26, v => { _ctrl.SetGroupBoss(_ctrl.selectedWaveIdx, _ctrl.selectedGroupIdx, v); RefreshSheet(); });
            y -= 30f;

            DL(dp, "lBHM","Boss HP×",  8, y, 70, 22, 12, Color.white, false);
            _ifBossHpMult = DIF(dp, "BHM","1", DETAIL_W/2f, y, DETAIL_W/2f-6, 26, "bossHpMult"); y -= 38f;

            HR(dp, y, DETAIL_W); y -= 8f;

            // 그룹 조작 버튼
            float bw = (DETAIL_W - 12) / 2f;
            _btnDupGroup = DB(dp, "복제", 6,          y - 16, bw, 30, new Color(0.2f,0.38f,0.6f),
                () => { _ctrl.DuplicateGroup(_ctrl.selectedWaveIdx, _ctrl.selectedGroupIdx); });
            _btnDelGroup = DB(dp, "삭제", 8 + bw,     y - 16, bw, 30, new Color(0.6f,0.15f,0.15f),
                () => { _ctrl.DeleteGroup(_ctrl.selectedWaveIdx, _ctrl.selectedGroupIdx); });

            RefreshDetail();
        }

        // 하단 바: Wave 조작
        void BuildBottom()
        {
            float sheetW = _sw - DETAIL_W;
            var bar = R(_root, "Bottom", 0, 0, sheetW, BOT_H, new Color(0.05f,0.05f,0.12f,1f));

            float bw = sheetW / 6f;
            // selectedWaveIdx가 -1이어도 AddWave는 항상 가능
            B(bar, "+Wave",  bw*0.5f, BOT_H/2f, bw-4, BOT_H-10, new Color(0.08f,0.52f,0.18f), () => _ctrl.AddWave(), 12);
            // selectedWaveIdx가 유효한 경우에만 동작 (컨트롤러 내부에서 ValidWave 체크)
            B(bar, "▲",      bw*1.5f, BOT_H/2f, bw-4, BOT_H-10, new Color(0.22f,0.28f,0.50f), () => {
                if (_ctrl.selectedWaveIdx >= 0) { _ctrl.MoveWaveUp(_ctrl.selectedWaveIdx); RefreshSheet(); }
            }, 12);
            B(bar, "▼",      bw*2.5f, BOT_H/2f, bw-4, BOT_H-10, new Color(0.22f,0.28f,0.50f), () => {
                if (_ctrl.selectedWaveIdx >= 0) { _ctrl.MoveWaveDown(_ctrl.selectedWaveIdx); RefreshSheet(); }
            }, 12);
            B(bar, "복제",   bw*3.5f, BOT_H/2f, bw-4, BOT_H-10, new Color(0.32f,0.18f,0.48f), () => {
                if (_ctrl.selectedWaveIdx >= 0) _ctrl.DuplicateWave(_ctrl.selectedWaveIdx);
            }, 12);
            B(bar, "삭제",   bw*4.5f, BOT_H/2f, bw-4, BOT_H-10, new Color(0.55f,0.14f,0.14f), () => {
                if (_ctrl.selectedWaveIdx >= 0) _ctrl.DeleteWave(_ctrl.selectedWaveIdx);
            }, 12);
            B(bar, "+Group", bw*5.5f, BOT_H/2f, bw-4, BOT_H-10, new Color(0.10f,0.42f,0.38f), () => {
                int wi = _ctrl.selectedWaveIdx;
                // 선택된 웨이브 없으면 마지막 웨이브에 추가
                if (wi < 0 && _ctrl.currentStage != null && _ctrl.currentStage.waves.Count > 0)
                    wi = _ctrl.currentStage.waves.Count - 1;
                if (wi >= 0) _ctrl.AddGroup(wi);
            }, 12);
        }

        // Auto-Scale 패널 (Detail 패널 하단에 붙음)
        void BuildAutoScale()
        {
            float apH = BOT_H + 30f;
            var ap = R(_root, "AutoScale", _sw - DETAIL_W, 0, DETAIL_W, apH,
                new Color(0.07f,0.09f,0.14f,1f));

            DL(ap, "lAS", "AutoScale", 4, apH - 14, 80, 20, 11, Color.cyan, false);

            DL(ap, "lHP1","HP",   4,  apH-34, 20, 20, 11, Color.white, false);
            _ifSHP = ASIF(ap, "SHP","30",  26, apH-34, 54, 24);
            DL(ap, "arr1","→",    82, apH-34, 16, 20, 11, Color.gray,  false);
            _ifEHP = ASIF(ap, "EHP","300",100, apH-34, 54, 24);

            DL(ap, "lSP1","Spd",  4,  apH-60, 24, 20, 11, Color.white, false);
            _ifSSPD = ASIF(ap,"SSPD","1.8", 30, apH-60, 54, 24);
            DL(ap, "arr2","→",    86, apH-60, 16, 20, 11, Color.gray,  false);
            _ifESPD = ASIF(ap,"ESPD","3.0",104, apH-60, 54, 24);

            B(ap, "적용", DETAIL_W-38, apH/2f-4, 68, 36, new Color(0.25f,0.45f,0.20f), OnAutoScale, 12);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Refresh
        // ─────────────────────────────────────────────────────────────

        private void RefreshSheet()
        {
            if (_sheetContent == null || _ctrl?.currentStage == null) return;

            // 기존 셀 전부 즉시 삭제 (Destroy는 다음 프레임이라 중복 빌드 발생)
            for (int i = _sheetContent.transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(_sheetContent.transform.GetChild(i).gameObject);

            var waves = _ctrl.currentStage.waves;
            if (waves == null) return;

            int maxGroups = 1;
            foreach (var w in waves)
                if (w.groups != null) maxGroups = Mathf.Max(maxGroups, w.groups.Count);

            // Content 크기 (top-left pivot, Y 음수 방향)
            float totalW = ROW_LBL_W + CELL_W * (maxGroups + 1); // +1: '+' 빈 열
            float totalH = COL_HDR_H + CELL_H * waves.Count;
            var cRt = _sheetContent.GetComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(Mathf.Max(totalW, _sw - DETAIL_W), Mathf.Max(totalH, _sh - TOP_H - BOT_H));

            // 헤더 행
            CellTxt(_sheetContent, "H0", "Wave\\Group", 0, 0, ROW_LBL_W, COL_HDR_H,
                new Color(0.12f,0.12f,0.24f), 10, Color.yellow);
            for (int gi = 0; gi < maxGroups; gi++)
                CellTxt(_sheetContent, $"HG{gi}", $"G{gi+1}",
                    ROW_LBL_W + gi*CELL_W, 0, CELL_W, COL_HDR_H,
                    new Color(0.12f,0.12f,0.24f), 10, Color.cyan);

            // 데이터 행
            for (int wi = 0; wi < waves.Count; wi++)
            {
                float rowY = COL_HDR_H + wi * CELL_H;  // content는 top-pivot → Y 아래방향
                var w = waves[wi];

                // 행 라벨 (클릭하면 해당 웨이브 선택)
                string wname = w.waveName ?? $"Wave {wi+1}";
                if (wname.Length > 9) wname = wname.Substring(0, 9);
                bool wSel = (_ctrl.selectedWaveIdx == wi);
                Color wLblBg = wSel ? new Color(0.20f,0.30f,0.50f) : new Color(0.10f,0.10f,0.20f);
                int wCapture = wi;
                CellBtn(_sheetContent, wname, 0, rowY, ROW_LBL_W, CELL_H, wLblBg,
                    () => { _ctrl.Select(wCapture, -1); RefreshSheet(); });

                // 그룹 셀
                for (int gi = 0; gi < maxGroups; gi++)
                {
                    float cellX = ROW_LBL_W + gi * CELL_W;
                    bool hasGrp = w.groups != null && gi < w.groups.Count;

                    if (hasGrp)
                    {
                        var grp = w.groups[gi];
                        bool sel = (_ctrl.selectedWaveIdx == wi && _ctrl.selectedGroupIdx == gi);
                        Color bg = sel ? new Color(0.25f,0.55f,0.90f)
                                       : (grp.isBoss ? new Color(0.50f,0.18f,0.18f)
                                                     : new Color(0.16f,0.20f,0.36f));
                        string lbl = $"HP:{grp.hp:0}\nCnt:{grp.count} S:{grp.speed:0.0}";
                        if (grp.isBoss) lbl = "★BOSS\n" + $"HP:{grp.hp:0}";
                        int wc = wi, gc = gi;
                        CellBtn(_sheetContent, lbl, cellX, rowY, CELL_W, CELL_H, bg,
                            () => { _ctrl.Select(wc, gc); RefreshSheet(); });
                    }
                    else
                    {
                        // 빈 셀
                        int wc = wi;
                        CellBtn(_sheetContent, "+", cellX, rowY, CELL_W, CELL_H,
                            new Color(0.10f,0.10f,0.20f),
                            () => { _ctrl.AddGroup(wc); });
                    }
                }
            }

            // 상태 라벨
            if (_lblStatus != null)
            {
                var s = _ctrl.currentStage;
                _lblStatus.text = $"{s.stageName}{(_ctrl.isDirty?"*":"")}  {waves.Count} waves";
            }
            if (_ifStageName != null && _ctrl.currentStage != null)
                _ifStageName.SetTextWithoutNotify(_ctrl.currentStage.stageName);

            RefreshDetail();
        }

        private void RefreshDetail()
        {
            var grp = _ctrl?.SelectedGroup;
            bool has = grp != null;

            if (_lblDetail) _lblDetail.text = has
                ? $"W{_ctrl.selectedWaveIdx+1} / G{_ctrl.selectedGroupIdx+1}"
                : "셀 선택하세요";

            void SetF(TMP_InputField f, string v) { if (f) f.SetTextWithoutNotify(v); }
            SetF(_ifHP,       has ? grp.hp.ToString("0")           : "");
            SetF(_ifSpeed,    has ? grp.speed.ToString("0.0")      : "");
            SetF(_ifCount,    has ? grp.count.ToString()           : "");
            SetF(_ifInterval, has ? grp.spawnInterval.ToString("0.00") : "");
            SetF(_ifReward,   has ? grp.reward.ToString()          : "");
            SetF(_ifBossHpMult, has ? grp.bossHpMult.ToString("0.0") : "");
            if (_togBoss) _togBoss.SetIsOnWithoutNotify(has && grp.isBoss);

            void SetI(TMP_InputField f, bool v) { if (f) f.interactable = v; }
            SetI(_ifHP, has); SetI(_ifSpeed, has); SetI(_ifCount, has);
            SetI(_ifInterval, has); SetI(_ifReward, has); SetI(_ifBossHpMult, has);
            if (_togBoss)    _togBoss.interactable    = has;
            if (_btnDelGroup) _btnDelGroup.interactable = has;
            if (_btnDupGroup) _btnDupGroup.interactable = has;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Callbacks
        // ─────────────────────────────────────────────────────────────

        void OnNew()  { _ctrl.NewStage(); }

        void OnLoad()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("StageData 열기", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;
            path = "Assets" + path.Replace(Application.dataPath, "").Replace('\\', '/');
            var asset = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (asset != null) _ctrl.LoadStage(asset);
            else Debug.LogWarning("[StageEditorUI] 로드 실패: " + path);
#endif
        }

        void OnAutoScale()
        {
            float sh = P(_ifSHP, 30f), eh = P(_ifEHP, 300f);
            float ss = P(_ifSSPD, 1.8f), es = P(_ifESPD, 3.0f);
            _ctrl.AutoScaleAll(sh, eh, ss, es);
        }

        float P(TMP_InputField f, float def) =>
            f != null && float.TryParse(f.text, out float v) ? v : def;

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Keyboard
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl && kb.sKey.wasPressedThisFrame) _ctrl.SaveStage();
            if (ctrl && kb.lKey.wasPressedThisFrame) OnLoad();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Sheet Cell Helpers (Top-Left pivot, Y downward)
        // ─────────────────────────────────────────────────────────────

        // 시트 셀 — content는 top-left pivot, Y = 위에서부터 아래로 px
        void CellTxt(GameObject parent, string name, string text,
            float x, float y, float w, float h, Color bg, int fs, Color fc)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1);
            rt.pivot = new Vector2(0,1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w - 1, h - 1);
            go.AddComponent<Image>().color = bg;

            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(2,1); trt.offsetMax = new Vector2(-2,-1);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fs; tmp.color = fc;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
        }

        void CellBtn(GameObject parent, string label, float x, float y,
            float w, float h, Color bg, System.Action onClick)
        {
            var go = new GameObject($"C_{x}_{y}");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1);
            rt.pivot = new Vector2(0,1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w - 2, h - 2);
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick.Invoke);

            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(2,1); trt.offsetMax = new Vector2(-2,-1);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 10; tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region General UI Helpers (bottom-left anchor, Y upward)
        // ─────────────────────────────────────────────────────────────

        // R: Rect 배경 — BL anchor+pivot
        GameObject R(GameObject par, string name, float x, float y, float w, float h, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(par.transform, false);
            go.AddComponent<Image>().color = col;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot     = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(w, h);
            return go;
        }

        // L: 텍스트 라벨 — BL anchor, center-left pivot (x=좌, y=중앙)
        TextMeshProUGUI L(GameObject par, string name, string text,
            float x, float y, float w, float h, int fs, Color col, bool center = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = center ? new Vector2(0.5f,0.5f) : new Vector2(0f,0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fs; tmp.color = col;
            tmp.alignment = center ? TMPro.TextAlignmentOptions.Center
                                   : TMPro.TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        // B: 버튼 — BL anchor, center pivot
        Button B(GameObject par, string label, float x, float y, float w, float h,
            Color bg, System.Action onClick, int fs)
        {
            var go = new GameObject($"B_{label}");
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f,0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick.Invoke);
            var cb = btn.colors;
            cb.highlightedColor = Color.Lerp(bg, Color.white, 0.3f);
            cb.pressedColor     = Color.Lerp(bg, Color.black, 0.2f);
            btn.colors = cb;

            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(2,1); trt.offsetMax = new Vector2(-2,-1);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = fs; tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            return btn;
        }

        // IF: InputField — BL anchor, center pivot
        TMP_InputField IF(GameObject par, string name, string val,
            float x, float y, float w, float h)
        {
            var go = new GameObject($"IF_{name}");
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = new Color(0.12f,0.12f,0.22f);
            return AttachTMPInput(go, val, 13);
        }

        // ── 디테일 패널 전용 헬퍼 (top-left pivot 기준, y = 패널 높이에서 내려옴) ──

        // DL: 디테일 라벨 (top-left pivot 기준 y 좌표 — 패널 상단 기준)
        TextMeshProUGUI DL(GameObject par, string name, string text,
            float x, float topY, float w, float h, int fs, Color col, bool center = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1);
            rt.pivot = center ? new Vector2(0.5f,0.5f) : new Vector2(0f,0.5f);
            rt.anchoredPosition = new Vector2(x, -topY);
            rt.sizeDelta = new Vector2(w, h);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fs; tmp.color = col;
            tmp.alignment = center ? TMPro.TextAlignmentOptions.Center
                                   : TMPro.TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        // DIF: 디테일 InputField (top-left pivot 기준 y)
        TMP_InputField DIF(GameObject par, string name, string val,
            float x, float topY, float w, float h, string fieldKey)
        {
            var go = new GameObject($"DIF_{name}");
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -topY);
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = new Color(0.12f,0.12f,0.22f);
            var f = AttachTMPInput(go, val, 13);
            f.contentType = TMP_InputField.ContentType.DecimalNumber;
            f.onEndEdit.AddListener(v =>
            {
                if (float.TryParse(v, out float fv))
                    _ctrl.SetGroupField(_ctrl.selectedWaveIdx, _ctrl.selectedGroupIdx, fieldKey, fv);
                RefreshSheet();
            });
            return f;
        }

        // DB: 디테일 버튼 (bottom-left pivot 기준, y = 패널 하단 기준)
        Button DB(GameObject par, string label, float x, float y, float w, float h,
            Color bg, System.Action onClick)
        {
            var go = new GameObject($"DB_{label}");
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick.Invoke);
            var tgo = new GameObject("T");
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(2,1); trt.offsetMax = new Vector2(-2,-1);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 12; tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            return btn;
        }

        // DTog: 디테일 토글 (top-left pivot 기준 y는 DL과 같은 흐름)
        Toggle DTog(GameObject par, float topY, float x, float h,
            System.Action<bool> onChange)
        {
            var go = new GameObject("DTog");
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1);
            rt.pivot = new Vector2(0f,0.5f);
            rt.anchoredPosition = new Vector2(x, -topY);
            rt.sizeDelta = new Vector2(h, h);
            var tgl = go.AddComponent<Toggle>();

            var bg = new GameObject("Bg");
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f,0.2f,0.3f);

            var ck = new GameObject("Ck");
            ck.transform.SetParent(bg.transform, false);
            var ckRt = ck.AddComponent<RectTransform>();
            ckRt.anchorMin = Vector2.zero; ckRt.anchorMax = Vector2.one;
            ckRt.offsetMin = new Vector2(3,3); ckRt.offsetMax = new Vector2(-3,-3);
            var ckImg = ck.AddComponent<Image>();
            ckImg.color = Color.red;

            tgl.targetGraphic = bgImg;
            tgl.graphic = ckImg;
            if (onChange != null) tgl.onValueChanged.AddListener(v => onChange(v));
            return tgl;
        }

        // HR: 구분선
        void HR(GameObject par, float topY, float w)
        {
            var go = new GameObject("HR");
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1);
            rt.pivot = new Vector2(0,1);
            rt.anchoredPosition = new Vector2(4, -topY);
            rt.sizeDelta = new Vector2(w - 8, 1);
            go.AddComponent<Image>().color = new Color(0.3f,0.3f,0.5f);
        }

        // ASIF: AutoScale용 InputField (bottom-left pivot)
        TMP_InputField ASIF(GameObject par, string name, string val, float x, float y, float w, float h)
        {
            var go = new GameObject($"ASIF_{name}");
            go.transform.SetParent(par.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = new Color(0.12f,0.12f,0.22f);
            var f = AttachTMPInput(go, val, 11);
            f.contentType = TMP_InputField.ContentType.DecimalNumber;
            return f;
        }

        // AttachTMPInput: TMP_InputField 공통 구성
        TMP_InputField AttachTMPInput(GameObject go, string val, int fs)
        {
            var input = go.AddComponent<TMP_InputField>();
            var area = new GameObject("Area");
            area.transform.SetParent(go.transform, false);
            var aRt = area.AddComponent<RectTransform>();
            aRt.anchorMin = Vector2.zero; aRt.anchorMax = Vector2.one;
            aRt.offsetMin = new Vector2(3,1); aRt.offsetMax = new Vector2(-3,-1);
            area.AddComponent<RectMask2D>();
            var tGo = new GameObject("T");
            tGo.transform.SetParent(area.transform, false);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text = val; tmp.fontSize = fs; tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            input.textViewport = aRt;
            input.textComponent = tmp;
            input.text = val;
            return input;
        }

        #endregion
    }
}
