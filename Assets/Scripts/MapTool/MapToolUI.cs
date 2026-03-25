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

private void Start() { _tool = FindObjectOfType<MapToolController>(); var existing = GameObject.Find("MapToolCanvas"); if (existing != null) { ConnectListeners(); } else { BuildUI(); } }

private void ConnectListeners() { ConnectBtn("Btn_Floor  [F]",   () => _tool.currentMode = MapToolController.PaintMode.Floor); ConnectBtn("Btn_Spawn  [S]",   () => _tool.currentMode = MapToolController.PaintMode.Spawn); ConnectBtn("Btn_End    [E]",    () => _tool.currentMode = MapToolController.PaintMode.End); ConnectBtn("Btn_Wall 1x1 [1]", () => _tool.currentMode = MapToolController.PaintMode.Wall1x1); ConnectBtn("Btn_Wall 2x1 [2]", () => _tool.currentMode = MapToolController.PaintMode.Wall2x1); ConnectBtn("Btn_Wall 1x2 [3]", () => _tool.currentMode = MapToolController.PaintMode.Wall1x2); ConnectBtn("Btn_Wall 2x2 [4]", () => _tool.currentMode = MapToolController.PaintMode.Wall2x2); ConnectBtn("Btn_Erase  [X]",   () => _tool.currentMode = MapToolController.PaintMode.Erase); ConnectBtn("Btn_<",  () => _tool.CycleSpriteIndex(-1)); ConnectBtn("Btn_>",  () => _tool.CycleSpriteIndex(1)); ConnectBtn("Btn_Rebuild Grid", () => { if (_ifColumns  != null && int.TryParse(_ifColumns.text,  out int c))  _tool.columns  = Mathf.Clamp(c,1,50); if (_ifRows     != null && int.TryParse(_ifRows.text,     out int r))  _tool.rows     = Mathf.Clamp(r,1,50); if (_ifTileSize != null && float.TryParse(_ifTileSize.text, out float s)) _tool.tileSize = Mathf.Clamp(s,0.1f,10f); if (_ifTileGap  != null && float.TryParse(_ifTileGap.text,  out float g)) _tool.tileGap  = Mathf.Clamp(g,0f,1f); _tool.BuildGrid(); }); ConnectBtn("Btn_Save  Ctrl+S", () => _tool.SaveToMapData()); ConnectBtn("Btn_Load  Ctrl+L", () => _tool.LoadFromMapData()); ConnectBtn("Btn_Clear All",    () => _tool.BuildGrid()); ConnectTMPLabels(); ConnectInputFields(); Debug.Log("[MapToolUI] Listeners connected to baked UI."); } private void ConnectBtn(string goName, UnityEngine.Events.UnityAction action) { var go = GameObject.Find(goName); if (go == null) { Debug.LogWarning($"[MapToolUI] Button not found: {goName}"); return; } var btn = go.GetComponent<UnityEngine.UI.Button>(); if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(action); } } private void ConnectTMPLabels() { _modeLabel   = FindTMP("ModeLabel"); _spriteLabel = FindTMP("SpriteLabel"); _coordLabel  = FindTMP("CoordLabel"); } private TMPro.TextMeshProUGUI FindTMP(string name) { var go = GameObject.Find(name); return go != null ? go.GetComponent<TMPro.TextMeshProUGUI>() : null; } private void ConnectInputFields() { _ifColumns  = FindIF("IF_7");   _ifRows     = FindIF("IF_11"); _ifTileSize = FindIF("IF_1.00"); _ifTileGap  = FindIF("IF_0.050"); if (_ifColumns  != null) _ifColumns.onEndEdit.AddListener(v  => { if (int.TryParse(v,out int x))    _tool.columns  = Mathf.Clamp(x,1,50); }); if (_ifRows     != null) _ifRows.onEndEdit.AddListener(v     => { if (int.TryParse(v,out int x))    _tool.rows     = Mathf.Clamp(x,1,50); }); if (_ifTileSize != null) _ifTileSize.onEndEdit.AddListener(v => { if (float.TryParse(v,out float f)) _tool.tileSize = Mathf.Clamp(f,0.1f,10f); }); if (_ifTileGap  != null) _ifTileGap.onEndEdit.AddListener(v  => { if (float.TryParse(v,out float f)) _tool.tileGap = Mathf.Clamp(f, 0f, 1f); }); } private TMPro.TMP_InputField FindIF(string name) { var go = GameObject.Find(name); return go != null ? go.GetComponent<TMPro.TMP_InputField>() : null; }


        private void Update()
        {
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

        public void InitForEditor() { if (_tool == null) _tool = FindObjectOfType<MapToolController>(); }

        public void BuildUI()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Canvas - 픽셀 퍼펙트로 화면 크기 그대로
            var cvGo = new GameObject("MapToolCanvas");
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
            float topH   = 52f;
            float botH   = 60f;
            float rightW = 180f;
            float btnH   = 40f;
            int   fsLbl  = 16;
            int   fsBtn  = 14;

            // ── 상단바 ────────────────────────────────────────────────
            var top = Rect(cvGo, "Top",
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

            // ── 좌측 모드 버튼 ────────────────────────────────────────
            var side = Rect(cvGo, "Side",
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

            // ── 우측 그리드 설정 ──────────────────────────────────────
            var right = Rect(cvGo, "Right",
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

            // ── 하단 버튼바 ──────────────────────────────────────────
            var bot = Rect(cvGo, "Bot",
                0, 0, sw, botH,
                new Color(0.06f, 0.06f, 0.14f, 0.95f));

            float bw = (sw - sideW - rightW - 24) / 3f;
            Btn(bot, "Save  Ctrl+S", sideW + bw*0.5f + 8,  botH/2, bw, botH-10,
                new Color(0.08f,0.55f,0.18f), () => _tool.SaveToMapData(), fsBtn);
            Btn(bot, "Load  Ctrl+L", sideW + bw*1.5f + 12, botH/2, bw, botH-10,
                new Color(0.08f,0.28f,0.65f), () => _tool.LoadFromMapData(), fsBtn);
            Btn(bot, "Clear All",    sideW + bw*2.5f + 16, botH/2, bw, botH-10,
                new Color(0.48f,0.18f,0.18f), () => _tool.BuildGrid(), fsBtn);
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
