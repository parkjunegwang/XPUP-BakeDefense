using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    [DefaultExecutionOrder(100)]
    public class GameSetup : MonoBehaviour
    {
        private bool _init;

        [Header("=== Turret Prefabs (assign in Inspector) ===")]
        public GameObject rangedTurretPrefab;
        public GameObject meleeTurretPrefab;
        public GameObject spikeTrapPrefab;
        public GameObject electricGatePrefab;
        public GameObject wallPrefab;
        public GameObject wall2x1Prefab;
        public GameObject wall1x2Prefab;
        public GameObject wall2x2Prefab;
        public GameObject projectilePrefab;

        [Header("=== Tile / Monster Prefab ===")]
        public GameObject tilePrefab;
        public GameObject monsterPrefab;

        private void Start()
        {
            if (_init) return; _init = true;
            EnsureManagers();
            EnsurePrefabs();
            ConnectPrefabsToManagers();
            BuildUI();
            BuildMap();
            // UI 빌드 완료 후 시작 타워 지급 (UIInventoryPanel이 준비된 후)
            // MapData 벽 먼저 설치 (인벤토리/골드 소모 없이)
            FindObjectOfType<MapManager>()?.ApplyWallsFromMapData();
            GameManager.Instance?.GiveStartTurrets();
            new GameObject("InputController").AddComponent<InputController>();
            Debug.Log("[GameSetup] Done");
        }

        // ── 매니저 오브젝트 자동 생성 ─────────────────────────────────
        private void EnsureManagers()
        {
            EnsureManager<InventoryManager>("InventoryManager");
            EnsureManager<CardManager>("CardManager");
        }

        private void EnsureManager<T>(string name) where T : MonoBehaviour
        {
            if (FindObjectOfType<T>() == null)
                new GameObject(name).AddComponent<T>();
        }

        // ── 프리팹 fallback ────────────────────────────────────────────
        private void EnsurePrefabs()
        {
            if (tilePrefab == null)
            {
                tilePrefab = MakeGO("_TilePfb", new Color(0.18f,0.18f,0.28f), 1.0f, SLayer.Tile);
                tilePrefab.AddComponent<BoxCollider2D>();
                tilePrefab.AddComponent<Tile>();
                tilePrefab.SetActive(false);
            }
            if (rangedTurretPrefab  == null) rangedTurretPrefab  = MakeTurretFallback<RangedTurret>( new Color(0.3f,0.6f,1f));
            if (meleeTurretPrefab   == null) meleeTurretPrefab   = MakeTurretFallback<MeleeTurret>(  new Color(0.9f,0.7f,0.2f));
            if (spikeTrapPrefab     == null) spikeTrapPrefab     = MakeTurretFallback<SpikeTrap>(    new Color(0.4f,0.35f,0.3f));
            if (electricGatePrefab  == null) electricGatePrefab  = MakeTurretFallback<ElectricGate>( new Color(0.9f,0.8f,0.1f));
            if (wallPrefab    == null) wallPrefab    = MakeTurretFallback<WallTurret>(new Color(0.55f,0.45f,0.35f));
            if (wall2x1Prefab == null) wall2x1Prefab = MakeTurretFallback<WallTurret>(new Color(0.50f,0.40f,0.30f));
            if (wall1x2Prefab == null) wall1x2Prefab = MakeTurretFallback<WallTurret>(new Color(0.50f,0.40f,0.30f));
            if (wall2x2Prefab == null) wall2x2Prefab = MakeTurretFallback<WallTurret>(new Color(0.45f,0.35f,0.25f));
            if (projectilePrefab    == null)
            {
                projectilePrefab = MakeGO("_ProjPfb", new Color(1f,1f,0.2f), 0.18f, SLayer.Projectile);
                projectilePrefab.AddComponent<Projectile>();
                projectilePrefab.SetActive(false);
            }
            if (monsterPrefab == null) monsterPrefab = BuildMonsterFallback();
        }

        private GameObject MakeTurretFallback<T>(Color col) where T : TurretBase
        {
            var go = MakeGO($"_Pfb_{typeof(T).Name}", col, 0.70f, SLayer.Turret);
            go.AddComponent<T>(); go.SetActive(false); return go;
        }

        private void ConnectPrefabsToManagers()
        {
            var map = FindObjectOfType<MapManager>();
            var tm  = FindObjectOfType<TurretManager>();
            var mm  = FindObjectOfType<MonsterManager>();
            if (map != null) map.tilePrefab   = tilePrefab;
            if (mm  != null) mm.monsterPrefab = monsterPrefab;
            if (tm  != null)
            {
                tm.rangedTurretPrefab = rangedTurretPrefab;
                tm.meleeTurretPrefab  = meleeTurretPrefab;
                tm.spikeTrapPrefab    = spikeTrapPrefab;
                tm.electricGatePrefab = electricGatePrefab;
                tm.wallPrefab    = wallPrefab;
                tm.wall2x1Prefab = wall2x1Prefab;
                tm.wall1x2Prefab = wall1x2Prefab;
                tm.wall2x2Prefab = wall2x2Prefab;
                tm.projectilePrefab   = projectilePrefab;
            }
        }

        // ── MAP ───────────────────────────────────────────────────────
        private void BuildMap()
        {
            var map = FindObjectOfType<MapManager>();
            if (map == null) return;
            var defSpawns = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,10) };
            var defEnds   = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,0)  };
            map.GenerateMap(defSpawns, defEnds);
            Debug.Log("[GameSetup] Map built");
        }

        // ── UI ────────────────────────────────────────────────────────
        private void BuildUI()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            var cvGo = new GameObject("Canvas");
            var cv   = cvGo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 10;
            var cvs = cvGo.AddComponent<CanvasScaler>();
            cvs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cvs.referenceResolution = new Vector2(390, 844);
            cvs.matchWidthOrHeight = 0.5f;
            cvGo.AddComponent<GraphicRaycaster>();

            var ui = FindObjectOfType<UIManager>();
            if (ui == null) return;

            // ── 상단 HUD ──────────────────────────────────────────────
            ui.waveText  = Txt(cvGo,"WaveText","Ready... (Wave 1)",
                new Vector2(0.5f,1f),new Vector2(0.5f,1f),new Vector2(0f,-14f),new Vector2(260f,40f),19);
            ui.levelText = Txt(cvGo,"LevelText","Lv.1",
                new Vector2(0f,1f),new Vector2(0f,1f),new Vector2(10f,-14f),new Vector2(80f,40f),20,Color.yellow);

            // XP 바
            var xpBg = new GameObject("XPBarBg");
            xpBg.transform.SetParent(cvGo.transform, false);
            xpBg.AddComponent<Image>().color = new Color(0.15f,0.15f,0.15f,0.9f);
            var xpBgRt = xpBg.GetComponent<RectTransform>();
            xpBgRt.anchorMin = new Vector2(0,1); xpBgRt.anchorMax = new Vector2(1,1);
            xpBgRt.pivot = new Vector2(0.5f,1f);
            xpBgRt.anchoredPosition = new Vector2(0,-46f);
            xpBgRt.sizeDelta = new Vector2(0,14f);

            var xpFillGo = new GameObject("XPBarFill");
            xpFillGo.transform.SetParent(xpBg.transform, false);
            var xpFillImg = xpFillGo.AddComponent<Image>();
            xpFillImg.color = new Color(0.2f,0.8f,0.3f);
            xpFillImg.type = Image.Type.Filled;
            xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            xpFillImg.fillAmount = 0f;
            var xpFillRt = xpFillGo.GetComponent<RectTransform>();
            xpFillRt.anchorMin = Vector2.zero; xpFillRt.anchorMax = Vector2.one;
            xpFillRt.offsetMin = new Vector2(1,1); xpFillRt.offsetMax = new Vector2(-1,-1);
            ui.xpBarFill = xpFillImg;

            ui.xpText = Txt(xpBg,"XPText","0/100",
                new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(200f,14f),9,Color.white);

            // 메시지
            ui.messageText = Txt(cvGo,"MsgText","",
                new Vector2(0.5f,0.65f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(340f,54f),18);
            ui.messageText.gameObject.SetActive(false);

            // ── 하단 인벤토리 패널 ────────────────────────────────────
            var bot = new GameObject("BotPanel");
            bot.transform.SetParent(cvGo.transform, false);
            bot.AddComponent<Image>().color = new Color(0.05f,0.05f,0.1f,0.92f);
            var botRt = bot.GetComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0,0); botRt.anchorMax = new Vector2(1,0);
            botRt.pivot = new Vector2(0.5f,0); botRt.anchoredPosition = Vector2.zero;
            botRt.sizeDelta = new Vector2(0, 110f);

            // 인벤토리 슬롯 컨테이너
            var invContainer = new GameObject("InvContainer");
            invContainer.transform.SetParent(bot.transform, false);
            var invRt = invContainer.AddComponent<RectTransform>();
            invRt.anchorMin = Vector2.zero; invRt.anchorMax = Vector2.one;
            invRt.offsetMin = new Vector2(4,4); invRt.offsetMax = new Vector2(-4,-4);

            var invPanel = invContainer.AddComponent<UIInventoryPanel>();
            invPanel.Init(invContainer);
            // 실행 순서 문제 해결: Init 후 강제 갱신
            invPanel.Refresh();

            // Start Wave 버튼
            #if UNITY_EDITOR
            // 디버그: XP +50 버튼
            var dbgBtn = Btn(cvGo, "BtnDebugXP", "+50 XP (Debug)",
                new Vector2(1f, 0f), new Color(0.4f, 0.1f, 0.4f),
                new Vector2(-60f, 128f), new Vector2(130f, 44f));
            dbgBtn.onClick.AddListener(() => GameManager.Instance?.AddXP(50));
#endif
            ui.startWaveBtn = Btn(cvGo,"BtnStart","Start Wave",
                new Vector2(0.5f,0f),new Color(0.1f,0.65f,0.2f),new Vector2(0f,118f),new Vector2(200f,44f));

            // Game Over 패널
            ui.gameOverPanel = FullPanel(cvGo,"GOPanel",new Color(0,0,0,0.88f));
            Txt(ui.gameOverPanel,"TxtGO","GAME OVER",
                new Vector2(0.5f,0.58f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(320f,70f),42,Color.red);
            ui.restartBtn = Btn(ui.gameOverPanel,"BtnGOR","Restart",
                new Vector2(0.5f,0.4f),new Color(0.25f,0.25f,0.7f),Vector2.zero,new Vector2(180f,58f));
            ui.gameOverPanel.SetActive(false);

            // Victory 패널
            ui.victoryPanel = FullPanel(cvGo,"VicPanel",new Color(0,0,0,0.88f));
            Txt(ui.victoryPanel,"TxtVic","VICTORY!",
                new Vector2(0.5f,0.58f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(320f,70f),42,Color.yellow);
            ui.victoryRestartBtn = Btn(ui.victoryPanel,"BtnVicR","Restart",
                new Vector2(0.5f,0.4f),new Color(0.25f,0.25f,0.7f),Vector2.zero,new Vector2(180f,58f));
            ui.victoryPanel.SetActive(false);

            ui.InitButtons();
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────
        private GameObject MakeGO(string n, Color col, float scale, int order)
        {
            var go = new GameObject(n);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSquareStatic(); sr.color = col; sr.sortingOrder = order;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private GameObject BuildMonsterFallback()
        {
            var root = new GameObject("_MonsterPfb");
            root.transform.localScale = Vector3.one * 0.55f;
            var body = root.AddComponent<SpriteRenderer>();
            body.sprite = WhiteSquareStatic(); body.color = new Color(0.2f,0.8f,0.2f);
            body.sortingOrder = SLayer.Monster;
            var hpBg = new GameObject("HpBg"); hpBg.transform.SetParent(root.transform);
            hpBg.transform.localPosition = new Vector3(0f,0.80f,0f);
            hpBg.transform.localScale    = new Vector3(1.05f,0.20f,1f);
            var bgSr = hpBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = WhiteSquareStatic(); bgSr.color = new Color(0.15f,0.15f,0.15f);
            bgSr.sortingOrder = SLayer.MonsterHPBg;
            var hpFill = new GameObject("HpFill"); hpFill.transform.SetParent(root.transform);
            hpFill.transform.localPosition = new Vector3(-0.02f,0.80f,0f);
            hpFill.transform.localScale    = new Vector3(1.0f,0.16f,1f);
            var fillSr = hpFill.AddComponent<SpriteRenderer>();
            fillSr.sprite = WhiteSquareStatic(); fillSr.color = Color.green;
            fillSr.sortingOrder = SLayer.MonsterHPFill;
            var m = root.AddComponent<Monster>();
            m.bodyRenderer = body; m.hpBarFill = fillSr;
            root.SetActive(false); return root;
        }

        public static Sprite WhiteSquareStatic(int px = 32)
        {
            var tex = new Texture2D(px, px);
            var pix = new Color32[px * px];
            for (int i = 0; i < pix.Length; i++) pix[i] = Color.white;
            tex.SetPixels32(pix); tex.Apply();
            return Sprite.Create(tex, new Rect(0,0,px,px), new Vector2(0.5f,0.5f), px);
        }

        private TextMeshProUGUI Txt(GameObject p, string name, string text,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int fs, Color? col=null)
        {
            var go=new GameObject(name); go.transform.SetParent(p.transform,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=anchor; rt.anchorMax=anchor; rt.pivot=pivot;
            rt.anchoredPosition=pos; rt.sizeDelta=size;
            var tmp=go.AddComponent<TextMeshProUGUI>();
            tmp.text=text; tmp.fontSize=fs; tmp.color=col??Color.white;
            tmp.alignment=TextAlignmentOptions.Center;
            return tmp;
        }

        private Button Btn(GameObject p, string name, string label,
            Vector2 anchor, Color bg, Vector2? pos=null, Vector2? size=null)
        {
            var pp=pos??new Vector2(0f,12f); var ss=size??new Vector2(110f,72f);
            var go=new GameObject(name); go.transform.SetParent(p.transform,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=anchor; rt.anchorMax=anchor;
            rt.pivot=new Vector2(0.5f,0f); rt.anchoredPosition=pp; rt.sizeDelta=ss;
            go.AddComponent<Image>().color=bg;
            var btn=go.AddComponent<Button>();
            var lbl=new GameObject("Lbl"); lbl.transform.SetParent(go.transform,false);
            var lrt=lbl.AddComponent<RectTransform>();
            lrt.anchorMin=Vector2.zero; lrt.anchorMax=Vector2.one;
            lrt.offsetMin=Vector2.zero; lrt.offsetMax=Vector2.zero;
            var tmp=lbl.AddComponent<TextMeshProUGUI>();
            tmp.text=label; tmp.fontSize=14; tmp.color=Color.white;
            tmp.alignment=TextAlignmentOptions.Center;
            return btn;
        }

        private GameObject FullPanel(GameObject p, string name, Color col)
        {
            var go=new GameObject(name); go.transform.SetParent(p.transform,false);
            var rt=go.AddComponent<RectTransform>();
            rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one;
            rt.offsetMin=Vector2.zero; rt.offsetMax=Vector2.zero;
            go.AddComponent<Image>().color=col;
            return go;
        }
    }
}
