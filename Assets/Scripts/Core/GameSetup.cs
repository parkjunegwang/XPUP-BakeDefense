using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    [DefaultExecutionOrder(100)]
    public class GameSetup : MonoBehaviour
    {
        private bool _init;

        [Header("=== Fallback Prefabs (Registry 연결 안 됐을 때만 사용) ===")]
        public GameObject projectilePrefab;

        [Header("=== Tile / Monster Prefab ===")]
        public GameObject tilePrefab;
        public GameObject monsterPrefab;

        private void Start()
        {
            if (_init) return;
            _init = true;
            EnsureManagers();
            EnsurePrefabs();
            ConnectPrefabsToManagers();
            BuildUI();
            BuildMap();
            FindObjectOfType<MapManager>()?.ApplyWallsFromMapData();
            new GameObject("InputController").AddComponent<InputController>();
            Debug.Log("[GameSetup] Done");
            StartCoroutine(DoInitialSetup());
        }

        private System.Collections.IEnumerator DoInitialSetup()
        {
            yield return null; // 한 프레임 대기 (StageManager.Start 완료 후)

            // 카메라 인트로가 있으면 완전히 끝날 때까지 대기
            var intro = FindObjectOfType<GameSceneIntro>();
            if (intro != null)
                yield return new WaitUntil(() => GameSceneIntro.IsComplete);

            // CardManager 세션 타워 고정 (인벤에는 추가 안 함 - 카드 선택으로만 받음)
            var selected = SaveData.SelectedTurrets;
            if (selected != null && selected.Length > 0)
            {
                CardManager.Instance?.SetSessionTurrets(selected);
                SaveData.SelectedTurrets = null;
            }
            else
            {
                // 타워선택 없이 직접 진입한 경우 (디버그/에디터)
                var stage = StageManager.Instance?.CurrentStage;
                if (stage?.startTurretPool != null && stage.startTurretPool.Length > 0)
                {
                    CardManager.Instance?.SetSessionTurrets(stage.startTurretPool);
                }
                else
                {
                    // Registry의 모든 일반 터렛을 세션으로 사용 (에디터 직진입 시 전체 카드 풀 사용)
                    var tm = FindObjectOfType<TurretManager>();
                    var reg = tm?.registry ?? Resources.Load<TurretRegistry>("TurretRegistry");
                    if (reg != null)
                    {
                        var allTypes = new System.Collections.Generic.List<TurretType>();
                        foreach (var e in reg.All)
                            if (e != null && e.type != TurretType.None && !e.isWall)
                                allTypes.Add(e.type);
                        CardManager.Instance?.SetSessionTurrets(allTypes);
                    }
                    else
                    {
                        CardManager.Instance?.SetSessionTurrets(new TurretType[] {
                            TurretType.RangedTurret, TurretType.ExplosiveCannon,
                            TurretType.PulseSlower, TurretType.BoomerangTurret
                        });
                    }
                }
            }

            // 2. 초기 카드 선택 (기본 2번)
            if (CardManager.Instance == null ||
                CardManager.Instance.cardPool == null ||
                CardManager.Instance.cardPool.Count == 0)
                yield break;

            var stageData = StageManager.Instance?.CurrentStage;
            int picks = (stageData != null && stageData.initialCardPicks > 0)
                ? stageData.initialCardPicks
                : 2;

            int done = 0;
            System.Action pickNext = null;
            pickNext = () =>
            {
                done++;
                if (done < picks)
                    CardManager.Instance.ShowCards(() => pickNext());
            };
            CardManager.Instance.ShowCards(() => pickNext());
        }

        private void EnsureManagers()
        {
            EnsureManager<InventoryManager>("InventoryManager");
            EnsureManager<CardManager>("CardManager");
        }

        private void EnsureManager<T>(string n) where T : MonoBehaviour
        {
            if (FindObjectOfType<T>() == null)
                new GameObject(n).AddComponent<T>();
        }

        private void EnsurePrefabs()
        {
            var tm = FindObjectOfType<TurretManager>();

            // 타일 프리팹
            if (tilePrefab == null)
            {
                tilePrefab = MakeGO("_TilePfb", new Color(0.18f,0.18f,0.28f), 1.0f, SLayer.Tile);
                tilePrefab.AddComponent<BoxCollider2D>();
                tilePrefab.AddComponent<Tile>();
                tilePrefab.SetActive(false);
            }

            // 프로젝타일 프리팹
            if (projectilePrefab == null)
            {
                projectilePrefab = MakeGO("_ProjPfb", new Color(1f,1f,0.2f), 0.18f, SLayer.Projectile);
                projectilePrefab.AddComponent<Projectile>();
                projectilePrefab.SetActive(false);
            }

            // 몬스터 프리팹
            if (monsterPrefab == null)
                monsterPrefab = BuildMonsterFallback();

            // Registry가 있으면 빠진 prefab만 fallback 생성
            if (tm?.registry != null)
            {
                tm.registry.RebuildCache();
                EnsureRegistryFallbacks(tm);
            }
            else if (tm != null)
            {
                Debug.LogWarning("[GameSetup] TurretManager에 Registry가 없습니다.");
                Debug.LogError("[GameSetup] TurretRegistry가 없습니다! Assets/Resources/TurretRegistry.asset을 확인하세요.");
            }
        }

        /// <summary>Registry는 있으나 일부 prefab이 null인 항목만 fallback 생성</summary>
        private void EnsureRegistryFallbacks(TurretManager tm)
        {
            var defs = new (TurretType type, System.Type script, Color col)[]
            {
                (TurretType.RangedTurret,        typeof(RangedTurret),          new Color(0.3f,0.6f,1f)),
                (TurretType.MeleeTurret,         typeof(MeleeTurret),           new Color(0.9f,0.7f,0.2f)),
                (TurretType.CrossMeleeTurret,    typeof(CrossMeleeTurret),      new Color(0.9f,0.7f,0.2f)),
                (TurretType.SpikeTrap,           typeof(SpikeTrap),             new Color(0.4f,0.35f,0.3f)),
                (TurretType.Wall,                typeof(WallTurret),            new Color(0.55f,0.45f,0.35f)),
                (TurretType.Wall2x1,             typeof(WallTurret),            new Color(0.50f,0.40f,0.30f)),
                (TurretType.Wall1x2,             typeof(WallTurret),            new Color(0.50f,0.40f,0.30f)),
                (TurretType.Wall2x2,             typeof(WallTurret),            new Color(0.45f,0.35f,0.25f)),
                (TurretType.AreaDamage,          typeof(AreaDamageTurret),      new Color(0.7f,0.2f,0.85f)),
                (TurretType.ExplosiveCannon,     typeof(ExplosiveCannon),       new Color(1f,0.4f,0.1f)),
                (TurretType.SlowShooter,         typeof(SlowShooterTurret),     new Color(0.3f,0.6f,1f)),
                (TurretType.RapidFire,           typeof(RapidFireTurret),       new Color(1f,0.85f,0.2f)),
                (TurretType.Tornado,             typeof(TornadoTurret),         new Color(0.5f,0.85f,1f)),
                (TurretType.LavaRain,            typeof(LavaRainTurret),        new Color(1f,0.3f,0f)),
                (TurretType.ChainLightning,      typeof(ChainLightningTurret),  new Color(0.4f,0.8f,1f)),
                (TurretType.BlackHole,           typeof(BlackHoleTurret),       new Color(0.5f,0f,0.8f)),
                (TurretType.PrecisionStrike,     typeof(PrecisionStrikeTurret), new Color(1f,0.95f,0.2f)),
                (TurretType.GambleBat,           typeof(GambleBatTurret),       new Color(0.9f,0.3f,0.9f)),
                (TurretType.PulseSlower,         typeof(PulseSlower),           new Color(0.4f,0.8f,1f)),
                (TurretType.DragonStatue,        typeof(DragonStatue),          new Color(1f,0.45f,0.1f)),
                (TurretType.HasteTower,          typeof(HasteTower),            new Color(0.9f,1f,0.3f)),
                (TurretType.PinballCannon,       typeof(PinballCannon),         new Color(1f,0.85f,0.1f)),
                (TurretType.BoomerangTurret,     typeof(BoomerangTurret),       new Color(0.5f,1f,0.3f)),
            };

            foreach (var (type, script, col) in defs)
            {
                var entry = tm.registry.Get(type);
                if (entry != null && entry.prefab == null)
                {
                    entry.prefab = MakeTurretFallbackByType(script, col);
                    Debug.LogWarning($"[GameSetup] {type} 프리팹 없음 - 런타임 fallback 생성됨");
                }
            }
        }

        private GameObject MakeTurretFallbackByType(System.Type turretScript, Color col)
        {
            var go = MakeGO($"_Pfb_{turretScript.Name}", col, 0.70f, SLayer.Turret);
            go.AddComponent(turretScript);
            go.SetActive(false);
            return go;
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

            // 타일/몬스터 연결
            if (map != null) map.tilePrefab = tilePrefab;
            if (mm  != null && mm.monsterPrefab == null) mm.monsterPrefab = monsterPrefab;

            if (tm != null)
            {
                // Registry가 없으면 Resources에서 로드 시도
                if (tm.registry == null)
                {
                    tm.registry = Resources.Load<TurretRegistry>("TurretRegistry");
                    if (tm.registry != null)
                    {
                        tm.registry.RebuildCache();
                        Debug.Log("[GameSetup] TurretRegistry Resources 로드 완료");
                    }
                    else
                    {
                        Debug.LogError("[GameSetup] TurretRegistry를 찾을 수 없습니다! 인스펙터 또는 Assets/Resources에 추가하세요.");
                    }
                }

                // 프로젝타일 연결
                if (tm.projectilePrefab == null)
                    tm.projectilePrefab = projectilePrefab;
            }
        }

        private void BuildMap()
        {
            var map = FindObjectOfType<MapManager>();
            if (map == null) return;
            var defSpawns = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,10) };
            var defEnds   = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,0)  };
            map.GenerateMap(defSpawns, defEnds);
        }

        public void BuildUI()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            var ui = FindObjectOfType<UIManager>();
            if (ui == null) return;

            var existing = GameObject.Find("GameCanvas");
            if (existing != null)
            {
                ConnectUIRefs(existing, ui);
                ui.InitButtons();
                return;
            }

            var cvGo = new GameObject("GameCanvas");
            var cv   = cvGo.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 10;
            var cvs = cvGo.AddComponent<CanvasScaler>();
            cvs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cvs.referenceResolution = new Vector2(390, 844);
            cvs.matchWidthOrHeight  = 0.5f;
            cvGo.AddComponent<GraphicRaycaster>();

            BuildUIContents(cvGo, ui);
            ui.InitButtons();
        }

        public void BuildUIContents(GameObject cvGo, UIManager ui)
        {
            ui.waveText  = Txt(cvGo, "WaveText", "Ready... (Wave 1)",
                new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0f,-14f), new Vector2(260f,40f), 19);
            ui.levelText = Txt(cvGo, "LevelText", "Lv.1",
                new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(10f,-14f), new Vector2(80f,40f), 20, Color.yellow);

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

            ui.xpText = Txt(xpBg, "XPText", "0/100",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(200f,14f), 9);

            ui.messageText = Txt(cvGo, "MsgText", "",
                new Vector2(0.5f,0.65f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(340f,54f), 18);
            ui.messageText.gameObject.SetActive(false);

            var bot = new GameObject("BotPanel");
            bot.transform.SetParent(cvGo.transform, false);
            bot.AddComponent<Image>().color = new Color(0.05f,0.05f,0.1f,0.92f);
            var botRt = bot.GetComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0,0); botRt.anchorMax = new Vector2(1,0);
            botRt.pivot = new Vector2(0.5f,0); botRt.anchoredPosition = Vector2.zero;
            botRt.sizeDelta = new Vector2(0,110f);

            var invContainer = new GameObject("InvContainer");
            invContainer.transform.SetParent(bot.transform, false);
            var invRt = invContainer.AddComponent<RectTransform>();
            invRt.anchorMin = Vector2.zero; invRt.anchorMax = Vector2.one;
            invRt.offsetMin = new Vector2(4,4); invRt.offsetMax = new Vector2(-4,-4);
            invContainer.AddComponent<UIInventoryPanel>();

            ui.startWaveBtn = Btn(cvGo, "BtnStart", "Start Wave",
                new Vector2(0.5f,0f), new Color(0.1f,0.65f,0.2f), new Vector2(0f,118f), new Vector2(200f,44f));

            ui.gameOverPanel = FullPanel(cvGo, "GOPanel", new Color(0,0,0,0.88f));
            Txt(ui.gameOverPanel, "TxtGO", "GAME OVER",
                new Vector2(0.5f,0.62f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(320f,70f), 42, Color.red);
            ui.lobbyBtn = Btn(ui.gameOverPanel, "BtnGOL", "Lobby",
                new Vector2(0.5f, 0.45f), new Color(0.2f, 0.5f, 0.2f), Vector2.zero, new Vector2(200f, 54f));
            ui.gameOverPanel.SetActive(false);

            ui.victoryPanel = FullPanel(cvGo, "VicPanel", new Color(0,0,0,0.88f));
            Txt(ui.victoryPanel, "TxtVic", "STAGE CLEAR!",
                new Vector2(0.5f,0.62f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(320f,70f), 42, Color.yellow);
            ui.victoryLobbyBtn = Btn(ui.victoryPanel, "BtnVicL", "Lobby",
                new Vector2(0.5f, 0.45f), new Color(0.2f, 0.5f, 0.2f), Vector2.zero, new Vector2(200f, 54f));
            ui.victoryPanel.SetActive(false);
        }

        public void ConnectUIRefs(GameObject cvGo, UIManager ui)
        {
            ui.waveText          = FindChildTMP(cvGo, "WaveText");
            ui.levelText         = FindChildTMP(cvGo, "LevelText");
            ui.xpText            = FindChildTMP(cvGo, "XPText");
            ui.messageText       = FindChildTMP(cvGo, "MsgText");
            var xpFillGo = FindChildGO(cvGo, "XPBarFill");
            if (xpFillGo != null) ui.xpBarFill = xpFillGo.GetComponent<Image>();
            ui.startWaveBtn      = FindChildGO(cvGo, "BtnStart")?.GetComponent<Button>();
            ui.gameOverPanel     = FindChildGO(cvGo, "GOPanel");
            ui.restartBtn        = FindChildGO(cvGo, "BtnGOR")?.GetComponent<Button>();
            ui.lobbyBtn          = FindChildGO(cvGo, "BtnGOL")?.GetComponent<Button>();
            ui.victoryPanel      = FindChildGO(cvGo, "VicPanel");
            ui.victoryRestartBtn = FindChildGO(cvGo, "BtnVicR")?.GetComponent<Button>();
            ui.victoryLobbyBtn   = FindChildGO(cvGo, "BtnVicL")?.GetComponent<Button>();

            var invContainer = FindChildGO(cvGo, "InvContainer");
            if (invContainer != null)
            {
                var invPanel = invContainer.GetComponent<UIInventoryPanel>()
                            ?? invContainer.AddComponent<UIInventoryPanel>();
                invPanel.Init(invContainer);
                invPanel.Refresh();
            }
        }

        private TextMeshProUGUI FindChildTMP(GameObject root, string childName)
            => FindDeep(root.transform, childName)?.GetComponent<TextMeshProUGUI>();

        private GameObject FindChildGO(GameObject root, string childName)
            => FindDeep(root.transform, childName)?.gameObject;

        private Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform c in parent) { var r = FindDeep(c, name); if (r != null) return r; }
            return null;
        }

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
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int fs, Color? col = null)
        {
            var go = new GameObject(name); go.transform.SetParent(p.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fs; tmp.color = col ?? Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button Btn(GameObject p, string name, string label,
            Vector2 anchor, Color bg, Vector2? pos = null, Vector2? size = null)
        {
            var pp = pos  ?? new Vector2(0f, 12f);
            var ss = size ?? new Vector2(110f, 72f);
            var go = new GameObject(name); go.transform.SetParent(p.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = pp; rt.sizeDelta = ss;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var lbl = new GameObject("Lbl"); lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = lbl.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 14; tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        private GameObject FullPanel(GameObject p, string name, Color col)
        {
            var go = new GameObject(name); go.transform.SetParent(p.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = col;
            return go;
        }
    }
}
