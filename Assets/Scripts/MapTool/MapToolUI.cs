using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Underdark
{
    public class MapToolUI : MonoBehaviour
    {
        private MapToolController _tool;
        private TextMeshProUGUI   _modeLabel;
        private TextMeshProUGUI   _spriteLabel;
        private TextMeshProUGUI   _coordLabel;
        private TMP_InputField    _ifColumns;
        private TMP_InputField    _ifRows;
        private TMP_InputField    _ifTileSize;
        private TMP_InputField    _ifTileGap;

        // ── 탭 시스템 ──────────────────────────────────────────────────
        private enum ToolTab { Map, Stage }
        private ToolTab       _activeTab  = ToolTab.Map;
        private StageEditorUI _stageUI;
        private GameObject    _mapPanels;   // 맵 탭 전용 UI 컨테이너 (Top/Side/Right/Bot의 부모)
        private Button        _tabMap, _tabStage;
        private GameObject    _cvGo;        // MapToolCanvas 루트

        // BuildUI() 후 Baker가 BuildStagePanel()을 호출할 때 필요한 크기 캐시
        private float _builtSw, _builtSh; // sh는 tabH 뺀 값

private void Awake()
{
    // BuildUI() 내부에서 _stageUI가 필요하므로 Start보다 먼저 확보
    _stageUI = GetComponent<StageEditorUI>();
    if (_stageUI == null) _stageUI = gameObject.AddComponent<StageEditorUI>();
}

private void Start()
{
    _tool = Object.FindFirstObjectByType<MapToolController>();
    var existing = GameObject.Find("MapToolCanvas");
    if (existing != null)
    {
        // 씬에 베이크된 Canvas가 있는 경우: 기존 리스너 연결 + 탭 바 추가
        _cvGo = existing;
        ConnectListeners();
        AppendTabBarToExistingCanvas(existing);
    }
    else
    {
        // 프리팹이 있으면 Instantiate, 없으면 코드로 동적 빌드
        var prefab = Resources.Load<GameObject>("UI/PF_MapToolUI");
        if (prefab == null)
        {
            // Resources 폴더 외부 경로도 시도 (AssetDatabase는 에디터 전용)
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/PF_MapToolUI.prefab");
#endif
        }

        if (prefab != null)
        {
            var inst = Instantiate(prefab);
            inst.name = "MapToolCanvas";
            _cvGo = inst;
            ConnectListeners();
            AppendTabBarToExistingCanvas(inst);
        }
        else
        {
            BuildUI();
        }
    }
}

private void ConnectListeners() { ConnectBtn("Btn_Floor  [F]",   () => _tool.currentMode = MapToolController.PaintMode.Floor); ConnectBtn("Btn_Spawn  [S]",   () => _tool.currentMode = MapToolController.PaintMode.Spawn); ConnectBtn("Btn_End    [E]",    () => _tool.currentMode = MapToolController.PaintMode.End); ConnectBtn("Btn_Wall 1x1 [1]", () => _tool.currentMode = MapToolController.PaintMode.Wall1x1); ConnectBtn("Btn_Wall 2x1 [2]", () => _tool.currentMode = MapToolController.PaintMode.Wall2x1); ConnectBtn("Btn_Wall 1x2 [3]", () => _tool.currentMode = MapToolController.PaintMode.Wall1x2); ConnectBtn("Btn_Wall 2x2 [4]", () => _tool.currentMode = MapToolController.PaintMode.Wall2x2); ConnectBtn("Btn_Erase  [X]",   () => _tool.currentMode = MapToolController.PaintMode.Erase); ConnectBtn("Btn_<",  () => _tool.CycleSpriteIndex(-1)); ConnectBtn("Btn_>",  () => _tool.CycleSpriteIndex(1)); ConnectBtn("Btn_Rebuild Grid", () => { if (_ifColumns  != null && int.TryParse(_ifColumns.text,  out int c))  _tool.columns  = Mathf.Clamp(c,1,50); if (_ifRows     != null && int.TryParse(_ifRows.text,     out int r))  _tool.rows     = Mathf.Clamp(r,1,50); if (_ifTileSize != null && float.TryParse(_ifTileSize.text, out float s)) _tool.tileSize = Mathf.Clamp(s,0.1f,10f); if (_ifTileGap  != null && float.TryParse(_ifTileGap.text,  out float g)) _tool.tileGap  = Mathf.Clamp(g,0f,1f); _tool.BuildGrid(); }); ConnectBtn("Btn_Save  Ctrl+S", () => _tool.SaveToMapData()); ConnectBtn("Btn_Load  Ctrl+L", () => _tool.LoadFromMapData()); ConnectBtn("Btn_Clear All",    () => _tool.BuildGrid()); ConnectTMPLabels(); ConnectInputFields(); Debug.Log("[MapToolUI] Listeners connected to baked UI."); } private void ConnectBtn(string goName, UnityEngine.Events.UnityAction action) { var go = GameObject.Find(goName); if (go == null) { Debug.LogWarning($"[MapToolUI] Button not found: {goName}"); return; } var btn = go.GetComponent<UnityEngine.UI.Button>(); if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(action); } } private void ConnectTMPLabels() {
    // 베이크된 씬의 이름과 동적 생성 이름 모두 시도
    _modeLabel   = FindTMP("ModeLabel")   ?? FindTMP("Mode");
    _spriteLabel = FindTMP("SpriteLabel") ?? FindTMP("Spr");
    _coordLabel  = FindTMP("CoordLabel")  ?? FindTMP("Coord");
 } private TMPro.TextMeshProUGUI FindTMP(string name) { var go = GameObject.Find(name); return go != null ? go.GetComponent<TMPro.TextMeshProUGUI>() : null; } private void ConnectInputFields() { _ifColumns  = FindIF("IF_7");   _ifRows     = FindIF("IF_11"); _ifTileSize = FindIF("IF_1.00"); _ifTileGap  = FindIF("IF_0.050"); if (_ifColumns  != null) _ifColumns.onEndEdit.AddListener(v  => { if (int.TryParse(v,out int x))    _tool.columns  = Mathf.Clamp(x,1,50); }); if (_ifRows     != null) _ifRows.onEndEdit.AddListener(v     => { if (int.TryParse(v,out int x))    _tool.rows     = Mathf.Clamp(x,1,50); }); if (_ifTileSize != null) _ifTileSize.onEndEdit.AddListener(v => { if (float.TryParse(v,out float f)) _tool.tileSize = Mathf.Clamp(f,0.1f,10f); }); if (_ifTileGap  != null) _ifTileGap.onEndEdit.AddListener(v  => { if (float.TryParse(v,out float f)) _tool.tileGap = Mathf.Clamp(f, 0f, 1f); }); } private TMPro.TMP_InputField FindIF(string name) { var go = GameObject.Find(name); return go != null ? go.GetComponent<TMPro.TMP_InputField>() : null; }


        private void Update()
        {
            // Stage 탭이 활성화 중이면 맵 단축키 스킵 (StageEditorUI.Update가 처리)
            if (_activeTab != ToolTab.Map) return;
            if (_tool == null) return; // MapToolController 없으면 스킵

            if (_coordLabel != null && Camera.main != null && Mouse.current != null)
            {
                Vector2 screen   = Mouse.current.position.ReadValue();
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
                float   step     = _tool.tileSize + _tool.tileGap;
                float   offX     = -(_tool.columns - 1) * step * 0.5f;
                float   offY     = -(_tool.rows    - 1) * step * 0.5f;
                int     gx       = Mathf.RoundToInt((worldPos.x - offX) / step);
                int     gy       = Mathf.RoundToInt((worldPos.y - offY) / step);
                _coordLabel.text = $"({gx} , {gy})";
            }
            if (_modeLabel   != null) _modeLabel.text   = $"Mode: {_tool.currentMode}";
            if (_spriteLabel != null)
            {
                var s = _tool.CurrentSprite;
                _spriteLabel.text = s != null ? $"Spr: {s.name}" : "Spr: none";
            }

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.fKey.wasPressedThisFrame) _tool.currentMode = MapToolController.PaintMode.Floor;
            if (kb.sKey.wasPressedThisFrame && !kb.leftCtrlKey.isPressed)
                _tool.currentMode = MapToolController.PaintMode.Spawn;
            if (kb.eKey.wasPressedThisFrame) _tool.currentMode = MapToolController.PaintMode.End;
            if (kb.digit1Key.wasPressedThisFrame) _tool.currentMode = MapToolController.PaintMode.Wall1x1;
            if (kb.digit2Key.wasPressedThisFrame) _tool.currentMode = MapToolController.PaintMode.Wall2x1;
            if (kb.digit3Key.wasPressedThisFrame) _tool.currentMode = MapToolController.PaintMode.Wall1x2;
            if (kb.digit4Key.wasPressedThisFrame) _tool.currentMode = MapToolController.PaintMode.Wall2x2;
            if (kb.xKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame)
                _tool.currentMode = MapToolController.PaintMode.Erase;
            if (kb.leftBracketKey.wasPressedThisFrame)  _tool.CycleSpriteIndex(-1);
            if (kb.rightBracketKey.wasPressedThisFrame) _tool.CycleSpriteIndex(1);
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl && kb.sKey.wasPressedThisFrame) _tool.SaveToMapData();
            if (ctrl && kb.lKey.wasPressedThisFrame) _tool.LoadFromMapData();
        }

        // ── 탭 전환 ──────────────────────────────────────────────────
        private void SwitchTab(ToolTab tab)
        {
            _activeTab = tab;
            bool isMap = (tab == ToolTab.Map);

            // _mapPanels가 null이면 cvGo에서 즉시 재탐색 (호출 타이밍 문제 방어)
            if (_mapPanels == null && _cvGo != null)
            {
                var mp = _cvGo.transform.Find("MapPanels");
                if (mp != null) _mapPanels = mp.gameObject;
            }

            if (_mapPanels != null)
            {
                _mapPanels.SetActive(isMap);
            }

            // Stage 패널 토글
            if (_stageUI != null)
            {
                if (!isMap) _stageUI.Show();
                else        _stageUI.Hide();
            }

            // 탭 버튼 색 강조
            UpdateTabColors();
        }

        private void UpdateTabColors()
        {
            if (_tabMap   != null) { var c = _tabMap.colors;   c.normalColor = _activeTab == ToolTab.Map   ? new Color(0.30f,0.45f,0.75f) : new Color(0.15f,0.20f,0.35f); _tabMap.colors   = c; }
            if (_tabStage != null) { var c = _tabStage.colors; c.normalColor = _activeTab == ToolTab.Stage ? new Color(0.30f,0.45f,0.75f) : new Color(0.15f,0.20f,0.35f); _tabStage.colors = c; }
        }

        /// <summary>씬에 베이크된 기존 Canvas가 있을 때 탭 바 + Stage 패널을 덧붙임.</summary>
        private void AppendTabBarToExistingCanvas(GameObject cvGo)
        {
            float tabH = 34f;

            // 씬에 베이크된 MapPanels 컨테이너를 _mapPanels에 직접 연결.
            // → SwitchTab이 항상 _mapPanels.SetActive() 경로를 사용하게 되어
            //   비활성 후 재활성화가 정상 동작함 (GameObject.Find는 비활성 오브젝트를 못 찾음).
            if (_mapPanels == null)
            {
                var mp = cvGo.transform.Find("MapPanels");
                if (mp != null) _mapPanels = mp.gameObject;
            }

            // Canvas 논리 픽셀 크기를 정확히 계산
            float sw, sh;
            var cvs = cvGo.GetComponent<CanvasScaler>();
            if (cvs != null && cvs.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                // ScaleWithScreenSize 모드: 기준 해상도 사용
                sw = cvs.referenceResolution.x;
                sh = cvs.referenceResolution.y;
            }
            else if (cvs != null && cvs.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                float sf = cvs.scaleFactor > 0 ? cvs.scaleFactor : 1f;
                sw = Screen.width  / sf;
                sh = Screen.height / sf;
            }
            else
            {
                // CanvasScaler 없거나 기본: 실제 픽셀
                sw = Screen.width;
                sh = Screen.height;
            }

            // 탭 바가 이미 있으면 버튼 참조만 재연결하고 StageUI는 새로 빌드
            var existingTabBar = cvGo.transform.Find("TabBar");
            if (existingTabBar != null)
            {
                // 탭 버튼 참조 복구
                var mapBtnGo   = existingTabBar.Find("Btn_🗺 Map");
                var stageBtnGo = existingTabBar.Find("Btn_📋 Stage");
                if (mapBtnGo   != null) _tabMap   = mapBtnGo.GetComponent<Button>();
                if (stageBtnGo != null) _tabStage = stageBtnGo.GetComponent<Button>();

                // 탭 리스너 재등록 (베이크된 프리팹에는 클로저가 없음)
                if (_tabMap   != null) { _tabMap.onClick.RemoveAllListeners();   _tabMap.onClick.AddListener(()   => SwitchTab(ToolTab.Map)); }
                if (_tabStage != null) { _tabStage.onClick.RemoveAllListeners(); _tabStage.onClick.AddListener(() => SwitchTab(ToolTab.Stage)); }
            }
            else
            {
                // TabBar가 없으면 새로 생성
                var tabBar = Rect(cvGo, "TabBar",
                    0, sh - tabH, sw, tabH,
                    new Color(0.04f, 0.04f, 0.10f, 0.99f));

                _tabMap   = Btn(tabBar, "🗺 Map",   sw/4,   tabH/2, sw/2 - 4, tabH - 6,
                    new Color(0.30f, 0.45f, 0.75f), () => SwitchTab(ToolTab.Map),   14);
                _tabStage = Btn(tabBar, "📋 Stage", sw*3/4, tabH/2, sw/2 - 4, tabH - 6,
                    new Color(0.15f, 0.20f, 0.35f), () => SwitchTab(ToolTab.Stage), 14);
            }

            // StagePanel: _builtSw/Sh를 설정하고 BuildStagePanel()로 통일
            // (C# 내부 참조가 null이라 재사용 불가 — 항상 Build로 연결해야 함)
            _builtSw = sw;
            _builtSh = sh - tabH;

            BuildStagePanel();

            UpdateTabColors();
        }

        public void InitForEditor()
        {
            if (_tool == null) _tool = Object.FindFirstObjectByType<MapToolController>();
            // 에디터 모드에서 BuildStagePanel() 호출을 위해 _cvGo 복원
            RestoreCvGoForEditor();
        }

        /// <summary>
        /// 에디터 베이크 시 _cvGo / _builtSw / _builtSh 를 씬에서 복원.
        /// BuildStagePanel()은 _cvGo가 null이면 아무것도 안 하므로 이 메서드를 먼저 호출해야 함.
        /// </summary>
        public void RestoreCvGoForEditor()
        {
            // _stageUI 복원 (Awake가 안 불렸을 때 대비)
            if (_stageUI == null)
            {
                _stageUI = GetComponent<StageEditorUI>();
                if (_stageUI == null) _stageUI = gameObject.AddComponent<StageEditorUI>();
            }
            // StageEditorUI._ctrl도 에디터 모드에선 Awake가 안 불리므로 수동 초기화
            _stageUI.InitForEditor();

            if (_cvGo == null)
                _cvGo = GameObject.Find("MapToolCanvas");
            if (_cvGo == null) return;

            if (_builtSw <= 0f)
            {
                var tabBar = _cvGo.transform.Find("TabBar");
                float tabH = 34f;
                if (tabBar != null)
                {
                    var tbRt = tabBar.GetComponent<RectTransform>();
                    if (tbRt != null && tbRt.sizeDelta.y > 0f) tabH = tbRt.sizeDelta.y;
                }
                var cvRt = _cvGo.GetComponent<RectTransform>();
                if (cvRt != null)
                {
                    _builtSw = cvRt.rect.width;
                    _builtSh = cvRt.rect.height - tabH;
                }
            }
        }

        public void BuildUI()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Canvas - 픽셀 퍼펙트로 화면 크기 그대로
            var cvGo = new GameObject("MapToolCanvas");
            _cvGo = cvGo;
            var cv   = cvGo.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 10;
            // CanvasScaler: ConstantPixelSize → 실제 픽셀 기준, scaleFactor로 조절
            var cvs = cvGo.AddComponent<CanvasScaler>();
            cvs.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            cvs.scaleFactor = Screen.height / 600f; // 600px 기준으로 스케일
            cvGo.AddComponent<GraphicRaycaster>();

            float sf  = cvs.scaleFactor;
            float sw  = Screen.width  / sf;
            float sh  = Screen.height / sf;

            // 크기 상수 (논리 픽셀 기준)
            float sideW  = 130f;
            float tabH   = 34f;    // 탭 바 높이
            float topH   = 52f;
            float botH   = 60f;
            float rightW = 180f;
            float btnH   = 40f;
            int   fsLbl  = 16;
            int   fsBtn  = 14;

            // ── 탭 바 (최상단) ────────────────────────────────────────
            var tabBar = Rect(cvGo, "TabBar",
                0, sh - tabH, sw, tabH,
                new Color(0.04f, 0.04f, 0.10f, 0.99f));

            _tabMap   = Btn(tabBar, "🗺 Map",   sw/4, tabH/2, sw/2 - 4, tabH - 6,
                new Color(0.22f,0.30f,0.55f), () => SwitchTab(ToolTab.Map), 14);
            _tabStage = Btn(tabBar, "📋 Stage", sw*3/4, tabH/2, sw/2 - 4, tabH - 6,
                new Color(0.15f,0.20f,0.35f), () => SwitchTab(ToolTab.Stage), 14);

            // ── 맵 탭 패널 컨테이너 (탭 바 아래 영역) ─────────────────
            _mapPanels = Rect(cvGo, "MapPanels",
                0, 0, sw, sh - tabH,
                new Color(0, 0, 0, 0)); // 투명 컨테이너 — 맵 UI 요소들의 부모

            // ※ StagePanel은 BuildUI()에서 생성하지 않음
            //   → Bake 시 StagePanel이 프리팹/씬에 포함되지 않도록 의도적으로 제외
            //   → 런타임 Start() → AppendTabBarToExistingCanvas()에서 동적으로 Build

            // ── 화면 사이즈를 탭 높이만큼 줄임 (기존 맵 UI용) ─────────
            sh = sh - tabH;
            _builtSw = sw; _builtSh = sh; // Baker에서 BuildStagePanel() 호출 시 사용

            // ── 상단바 (_mapPanels 아래) ──────────────────────────────
            var top = Rect(_mapPanels, "Top",
                0, sh - topH, sw, topH,
                new Color(0.06f, 0.06f, 0.14f, 0.95f));

            _modeLabel   = Txt(top, "Mode", "Mode: Floor",
                4, topH/2, 200, topH, fsLbl, Color.yellow, false);
            _spriteLabel = Txt(top, "Spr",  "Spr: none",
                210, topH/2, 200, topH, fsBtn, Color.white, false);
            _coordLabel  = Txt(top, "Coord","(0,0)",
                sw - rightW - 100, topH/2, 120, topH, fsLbl, Color.cyan, false);

            Btn(top, "<", sw - rightW - 170, topH/2, 44, btnH-4,
                new Color(0.2f,0.2f,0.35f), () => _tool.CycleSpriteIndex(-1), fsBtn);
            Btn(top, ">", sw - rightW - 118, topH/2, 44, btnH-4,
                new Color(0.2f,0.2f,0.35f), () => _tool.CycleSpriteIndex(1), fsBtn);

            // ── 좌측 모드 버튼 (_mapPanels 아래) ─────────────────────
            var side = Rect(_mapPanels, "Side",
                0, botH, sideW, sh - topH - botH,
                new Color(0.06f, 0.06f, 0.14f, 0.95f));

            float sideH  = sh - topH - botH;
            float margin = 4f;
            float bH     = (sideH - margin * 9) / 8f;
            bH = Mathf.Min(bH, 50f);

            (string lbl, MapToolController.PaintMode mode, Color col)[] modes =
            {
                ("Floor  [F]",    MapToolController.PaintMode.Floor,   new Color(0.22f,0.28f,0.50f)),
                ("Spawn  [S]",    MapToolController.PaintMode.Spawn,   new Color(0.08f,0.52f,0.18f)),
                ("End    [E]",    MapToolController.PaintMode.End,     new Color(0.60f,0.10f,0.10f)),
                ("Wall 1x1 [1]",  MapToolController.PaintMode.Wall1x1, new Color(0.48f,0.38f,0.28f)),
                ("Wall 2x1 [2]",  MapToolController.PaintMode.Wall2x1, new Color(0.44f,0.34f,0.24f)),
                ("Wall 1x2 [3]",  MapToolController.PaintMode.Wall1x2, new Color(0.44f,0.34f,0.24f)),
                ("Wall 2x2 [4]",  MapToolController.PaintMode.Wall2x2, new Color(0.40f,0.30f,0.20f)),
                ("Erase  [X]",    MapToolController.PaintMode.Erase,   new Color(0.38f,0.14f,0.14f)),
            };
            for (int i = 0; i < modes.Length; i++)
            {
                int idx = i;
                float by = sideH - margin - bH/2f - i * (bH + margin);
                Btn(side, modes[i].lbl, sideW/2, by, sideW - 8, bH,
                    modes[i].col, () => _tool.currentMode = modes[idx].mode, fsBtn);
            }

            // ── 우측 그리드 설정 (_mapPanels 아래) ───────────────────
            var right = Rect(_mapPanels, "Right",
                sw - rightW, botH, rightW, sh - topH - botH,
                new Color(0.06f, 0.06f, 0.14f, 0.95f));

            float rh = sh - topH - botH;
            Txt(right, "Title", "Grid Settings",
                rightW/2, rh - 20, rightW - 8, 30, fsLbl, Color.yellow, true);

            string[] lbNames  = { "Cols", "Rows", "Size", "Gap" };
            string[] defaults = {
                _tool.columns.ToString(), _tool.rows.ToString(),
                _tool.tileSize.ToString("F2"), _tool.tileGap.ToString("F3")
            };
            float rowH = 44f;
            TMP_InputField[] ifs = new TMP_InputField[4];
            for (int i = 0; i < 4; i++)
            {
                float ry = rh - 60 - i * rowH;
                Txt(right, lbNames[i], lbNames[i],
                    8, ry, 50, 30, fsBtn, Color.white, false);
                ifs[i] = IF(right, defaults[i], rightW/2 + 20, ry, rightW - 68, 32);
            }
            _ifColumns = ifs[0]; _ifRows = ifs[1];
            _ifTileSize = ifs[2]; _ifTileGap = ifs[3];

            _ifColumns.onEndEdit.AddListener(v  => { if (int.TryParse(v,out int x))    _tool.columns  = Mathf.Clamp(x,1,50); });
            _ifRows.onEndEdit.AddListener(v     => { if (int.TryParse(v,out int x))    _tool.rows     = Mathf.Clamp(x,1,50); });
            _ifTileSize.onEndEdit.AddListener(v => { if (float.TryParse(v,out float f)) _tool.tileSize = Mathf.Clamp(f,0.1f,10f); });
            _ifTileGap.onEndEdit.AddListener(v  => { if (float.TryParse(v,out float f)) _tool.tileGap  = Mathf.Clamp(f,0f,1f); });

            Btn(right, "Rebuild Grid", rightW/2, 36, rightW - 16, btnH,
                new Color(0.32f,0.18f,0.48f), () => {
                    if (int.TryParse(_ifColumns.text, out int c))   _tool.columns  = Mathf.Clamp(c,1,50);
                    if (int.TryParse(_ifRows.text, out int r))      _tool.rows     = Mathf.Clamp(r,1,50);
                    if (float.TryParse(_ifTileSize.text,out float s)) _tool.tileSize = Mathf.Clamp(s,0.1f,10f);
                    if (float.TryParse(_ifTileGap.text, out float g)) _tool.tileGap  = Mathf.Clamp(g,0f,1f);
                    _tool.BuildGrid();
                }, fsBtn);

            // ── 하단 버튼바 (_mapPanels 아래) ────────────────────────
            var bot = Rect(_mapPanels, "Bot",
                0, 0, sw, botH,
                new Color(0.06f, 0.06f, 0.14f, 0.95f));

            float bw = (sw - sideW - rightW - 24) / 3f;
            Btn(bot, "Save  Ctrl+S", sideW + bw*0.5f + 8,  botH/2, bw, botH-10,
                new Color(0.08f,0.55f,0.18f), () => _tool.SaveToMapData(), fsBtn);
            Btn(bot, "Load  Ctrl+L", sideW + bw*1.5f + 12, botH/2, bw, botH-10,
                new Color(0.08f,0.28f,0.65f), () => _tool.LoadFromMapData(), fsBtn);
            Btn(bot, "Clear All",    sideW + bw*2.5f + 16, botH/2, bw, botH-10,
                new Color(0.48f,0.18f,0.18f), () => _tool.BuildGrid(), fsBtn);

            // StagePanel은 BuildUI() 에서 직접 생성하지 않음
            // → 아래 BuildStagePanel()을 호출하는 쪽에서 필요 시 명시적으로 생성
            BuildStagePanel();
        }

        /// <summary>
        /// StagePanel을 _cvGo(MapToolCanvas)에 생성/재생성.
        /// - 동적 빌드(프리팹/씬 없음): BuildUI() 내부에서 자동 호출
        /// - Bake To Scene: BuildUI() 후 Baker에서 추가 호출 (씬에 구움)
        /// - Bake To Prefab: BuildUI() 후 Baker가 호출 안 함 (프리팹에 미포함)
        /// - 런타임(베이크 씬/프리팹): AppendTabBarToExistingCanvas()에서 호출
        /// </summary>
        public void BuildStagePanel(bool startActive = false)
        {
            if (_cvGo == null || _stageUI == null) return;

            // 기존 StagePanel이 있으면 제거 후 새로 빌드
            var existing = _cvGo.transform.Find("StagePanel");
            if (existing != null)
            {
#if UNITY_EDITOR
                Object.DestroyImmediate(existing.gameObject);
#else
                Destroy(existing.gameObject);
#endif
            }
            _stageUI.Build(_cvGo, _builtSw, _builtSh, startActive);
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────
        // anchoredPosition 기준 (bottom-left origin in local rect)
        private GameObject Rect(GameObject parent, string name,
            float x, float y, float w, float h, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = col;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot     = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(w, h);
            return go;
        }

        private TextMeshProUGUI Txt(GameObject parent, string name, string text,
            float x, float y, float w, float h, int fs, Color col, bool center)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = center ? new Vector2(0.5f,0.5f) : new Vector2(0f,0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(w, h);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fs;
            tmp.color     = col;
            tmp.alignment = center
                ? TMPro.TextAlignmentOptions.Center
                : TMPro.TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private Button Btn(GameObject parent, string label, float x, float y,
            float w, float h, Color bg, UnityEngine.Events.UnityAction onClick, int fs)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f,0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(w, h);
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var cb = btn.colors;
            cb.highlightedColor = Color.Lerp(bg, Color.white, 0.3f);
            cb.pressedColor     = Color.Lerp(bg, Color.black, 0.2f);
            btn.colors = cb;
            // 텍스트 자식
            var lbl = new GameObject("Text");
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(2,1); lrt.offsetMax = new Vector2(-2,-1);
            var tmp = lbl.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = fs; tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            return btn;
        }

        private TMP_InputField IF(GameObject parent, string val,
            float x, float y, float w, float h)
        {
            var go = new GameObject($"IF_{val}");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f,0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(w, h);
            go.AddComponent<Image>().color = new Color(0.12f,0.12f,0.22f);

            var input = go.AddComponent<TMP_InputField>();

            // TextArea
            var area = new GameObject("TextArea");
            area.transform.SetParent(go.transform, false);
            var aRt = area.AddComponent<RectTransform>();
            aRt.anchorMin = Vector2.zero; aRt.anchorMax = Vector2.one;
            aRt.offsetMin = new Vector2(4,1); aRt.offsetMax = new Vector2(-4,-1);
            area.AddComponent<RectMask2D>();

            // Text
            var tGo = new GameObject("Text");
            tGo.transform.SetParent(area.transform, false);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text = val; tmp.fontSize = 14; tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

            input.textViewport  = aRt;
            input.textComponent = tmp;
            input.text          = val;
            input.contentType   = TMP_InputField.ContentType.DecimalNumber;
            return input;
        }
    }
}
