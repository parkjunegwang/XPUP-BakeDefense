using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 터렛 선택 팝업. GameScene의 PopupCanvas에서 PopupManager를 통해 열린다.
    /// 확인 후 GameSetup.StartGame()을 호출해 맵 생성 + 카메라 슬라이드 시작.
    /// </summary>
    public class TowerSelectPopup : Popup
    {
        [Header("References")]
        public Transform       cardContainer;
        public Transform       slotContainer;
        public Button          confirmButton;
        public Button          backButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI countText;

        [Header("Settings")]
        public string gameSceneName = "GameScene";
        public int    minSelect = 1;
        public int    maxSelect = 4;

        // ── 런타임 상태 ─────────────────────────────────────────────
        private TurretRegistry   _registry;
        private StageData        _stage;
        private List<TurretType> _pool   = new List<TurretType>();
        private List<TurretType> _picked = new List<TurretType>();
        private List<GameObject> _cards  = new List<GameObject>();
        private GameObject[]     _slots;

        // ── 색상 상수 ────────────────────────────────────────────────
        private static readonly Color COL_SLOT_EMPTY  = new Color(0.15f, 0.15f, 0.20f, 1f);
        private static readonly Color COL_SLOT_FILLED = new Color(0.25f, 0.55f, 0.95f, 1f);
        private static readonly Color COL_LOCKED_BG   = new Color(0.12f, 0.12f, 0.15f, 1f);
        private static readonly Color COL_LOCKED_TEXT = new Color(0.40f, 0.40f, 0.45f, 1f);

        // ─────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (backButton    != null) backButton.onClick.AddListener(Close);
        }

        public override void OnOpen()
        {
            // TurretRegistry 로드
            _registry = Resources.Load<TurretRegistry>("TurretRegistry");
            if (_registry == null)
                Debug.LogError("[TowerSelectPopup] TurretRegistry를 찾을 수 없습니다!");
            else
                _registry.RebuildCache();

            SaveData.InitDefaultOwnership(_registry);

            // 스테이지 데이터 로드
            LoadStageData();

            if (_stage != null && _stage.startTurretCount > 0)
                maxSelect = _stage.startTurretCount;

            if (titleText != null)
                titleText.text = _stage != null ? _stage.stageName : "Select Towers";

            // 선택 초기화
            _picked.Clear();

            BuildSlots();
            BuildPoolFromRegistry();
            BuildCards();
            RefreshConfirm();
        }

        public override void OnClose() { }

        // ── 스테이지 데이터 로드 ─────────────────────────────────────

        private void LoadStageData()
        {
            var stageRegistry = Resources.Load<StageRegistry>("StageRegistry");
            if (stageRegistry == null) return;

            var stages = stageRegistry.ValidStages();
            int idx    = SaveData.SelectedStageIndex;
            if (stages != null && stages.Count > 0)
                _stage = idx < stages.Count ? stages[idx] : stages[0];
        }

        // ── 상단 슬롯 ────────────────────────────────────────────────

        private void BuildSlots()
        {
            if (slotContainer == null) return;
            foreach (Transform c in slotContainer) Destroy(c.gameObject);

            _slots = new GameObject[maxSelect];

            for (int i = 0; i < maxSelect; i++)
            {
                int slotIdx = i;

                var slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(slotContainer, false);

                var bg = slot.AddComponent<Image>();
                bg.color = COL_SLOT_EMPTY;

                var ol = slot.AddComponent<Outline>();
                ol.effectColor    = new Color(0.35f, 0.35f, 0.45f, 1f);
                ol.effectDistance = new Vector2(2f, -2f);

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(slot.transform, false);
                var lrt = labelGo.AddComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                var lTmp = labelGo.AddComponent<TextMeshProUGUI>();
                lTmp.text      = "?";
                lTmp.fontSize  = 32f;
                lTmp.fontStyle = FontStyles.Bold;
                lTmp.color     = new Color(0.35f, 0.35f, 0.40f, 1f);
                lTmp.alignment = TextAlignmentOptions.Center;

                var btn = slot.AddComponent<Button>();
                btn.onClick.AddListener(() => RemoveSlot(slotIdx));

                _slots[i] = slot;
            }
        }

        private void RefreshSlots()
        {
            if (_slots == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot == null) continue;

                var bg   = slot.GetComponent<Image>();
                var lTmp = slot.GetComponentInChildren<TextMeshProUGUI>();

                if (i < _picked.Count)
                {
                    var type  = _picked[i];
                    Color col = Color.gray;
                    string label = type.ToString();

                    if (_registry != null)
                    {
                        var entry = _registry.Get(type);
                        if (entry != null)
                        {
                            if (entry.color != default)             col   = entry.color;
                            if (!string.IsNullOrEmpty(entry.label)) label = entry.label;
                        }
                    }

                    if (bg   != null) bg.color  = new Color(col.r * 0.6f, col.g * 0.6f, col.b * 0.6f, 1f);
                    if (lTmp != null)
                    {
                        lTmp.text     = label;
                        lTmp.color    = Color.white;
                        lTmp.fontSize = 28f;
                    }
                }
                else
                {
                    if (bg   != null) bg.color = COL_SLOT_EMPTY;
                    if (lTmp != null)
                    {
                        lTmp.text     = "?";
                        lTmp.color    = new Color(0.35f, 0.35f, 0.40f, 1f);
                        lTmp.fontSize = 32f;
                    }
                }
            }
        }

        private void RemoveSlot(int slotIdx)
        {
            if (slotIdx >= _picked.Count) return;

            var type = _picked[slotIdx];
            _picked.RemoveAt(slotIdx);

            ResetCardVisual(type);
            RefreshSlots();
            RefreshConfirm();
        }

        // ── 카드 풀 ──────────────────────────────────────────────────

        private void BuildPoolFromRegistry()
        {
            _pool.Clear();
            if (_registry == null) return;

            var owned   = new List<TurretType>();
            var unowned = new List<TurretType>();

            foreach (var entry in _registry.All)
            {
                if (entry.type == TurretType.None) continue;
                if (entry.isWall) continue;

                if (SaveData.IsOwned(entry.type))
                    owned.Add(entry.type);
                else
                    unowned.Add(entry.type);
            }

            _pool.AddRange(owned);
            _pool.AddRange(unowned);
        }

        // ── 카드 생성 ────────────────────────────────────────────────

        private void BuildCards()
        {
            if (cardContainer == null) return;
            foreach (Transform c in cardContainer) Destroy(c.gameObject);
            _cards.Clear();
            foreach (var type in _pool)
                _cards.Add(CreateCard(type));
        }

        private GameObject CreateCard(TurretType type)
        {
            bool   isOwned = SaveData.IsOwned(type);
            string label   = type.ToString();
            Color  col     = Color.gray;
            string desc    = GetDesc(type);

            if (_registry != null)
            {
                var entry = _registry.Get(type);
                if (entry != null)
                {
                    if (!string.IsNullOrEmpty(entry.label)) label = entry.label;
                    if (entry.color != default)             col   = entry.color;
                }
            }

            var go = new GameObject($"Card_{type}");
            go.transform.SetParent(cardContainer, false);
            
            var bg   = go.AddComponent<Image>();
            bg.color = isOwned
                ? new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f)
                : COL_LOCKED_BG;
            bg.rectTransform.sizeDelta = new Vector2(280,0f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor    = Color.clear;
            outline.effectDistance = new Vector2(4f, -4f);
            
            var btn = go.AddComponent<Button>();
            btn.interactable = isOwned;

            // 컬러 스트립 (상단 38%)
            var strip = new GameObject("Strip");
            strip.transform.SetParent(go.transform, false);
            var sr = strip.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0.62f); sr.anchorMax = Vector2.one;
            sr.offsetMin = sr.offsetMax = Vector2.zero;
            var stripImg = strip.AddComponent<Image>();
            stripImg.color = isOwned ? col : new Color(col.r * 0.25f, col.g * 0.25f, col.b * 0.25f, 1f);

            // 이름
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(strip.transform, false);
            var nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            var namTmp = nameGo.AddComponent<TextMeshProUGUI>();
            namTmp.text      = label;
            namTmp.fontSize  = 40f;
            namTmp.fontStyle = FontStyles.Bold;
            namTmp.color     = isOwned ? Color.white : COL_LOCKED_TEXT;
            namTmp.alignment = TextAlignmentOptions.Center;

            // 체크마크
            var chkGo = new GameObject("Check");
            chkGo.transform.SetParent(strip.transform, false);
            var crt = chkGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.7f, 0f); crt.anchorMax = Vector2.one;
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var chkTmp = chkGo.AddComponent<TextMeshProUGUI>();
            chkTmp.text      = "✓";
            chkTmp.fontSize  = 20f;
            chkTmp.fontStyle = FontStyles.Bold;
            chkTmp.color     = Color.white;
            chkTmp.alignment = TextAlignmentOptions.MidlineRight;
            chkGo.SetActive(false);

            // 설명
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(go.transform, false);
            var drt = descGo.AddComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = new Vector2(1f, 0.62f);
            drt.offsetMin = new Vector2(6f, 2f); drt.offsetMax = new Vector2(-6f, -2f);
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text      = isOwned ? desc : "🔒 미획득";
            descTmp.fontSize  = isOwned ? 30f : 36f;
            descTmp.color     = isOwned ? new Color(0.85f, 0.85f, 0.92f) : COL_LOCKED_TEXT;
            descTmp.alignment = isOwned ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;

            if (isOwned)
            {
                var capType = type;
                btn.onClick.AddListener(() => ToggleCard(capType, go, outline, chkGo, bg, col));
            }

            return go;
        }

        // ── 카드 토글 ────────────────────────────────────────────────

        private void ToggleCard(TurretType type, GameObject go, Outline outline, GameObject chk, Image bg, Color col)
        {
            if (_picked.Contains(type))
            {
                _picked.Remove(type);
                outline.effectColor = Color.clear;
                chk.SetActive(false);
                bg.color = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f);
            }
            else
            {
                if (_picked.Count >= maxSelect)
                {
                    var oldest = _picked[0];
                    _picked.RemoveAt(0);
                    ResetCardVisual(oldest);
                }
                _picked.Add(type);
                outline.effectColor = Color.white;
                chk.SetActive(true);
                bg.color = new Color(col.r * 0.75f, col.g * 0.75f, col.b * 0.75f, 1f);
            }

            RefreshSlots();
            RefreshConfirm();
        }

        private void ResetCardVisual(TurretType type)
        {
            int idx = _pool.IndexOf(type);
            if (idx < 0 || idx >= _cards.Count || _cards[idx] == null) return;

            var go  = _cards[idx];
            Color col = Color.gray;
            if (_registry != null)
            {
                var entry = _registry.Get(type);
                if (entry != null && entry.color != default) col = entry.color;
            }

            var bgImg = go.GetComponent<Image>();
            if (bgImg != null) bgImg.color = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f);

            var ol = go.GetComponent<Outline>();
            if (ol != null) ol.effectColor = Color.clear;

            go.transform.Find("Strip/Check")?.gameObject.SetActive(false);
        }

        // ── 확인 버튼 갱신 ───────────────────────────────────────────

        private void RefreshConfirm()
        {
            if (countText != null)
                countText.text = $"Select {_picked.Count} / {maxSelect}";
            if (confirmButton != null)
                confirmButton.interactable = _picked.Count >= minSelect;
        }

        // ── 확인 버튼 콜백 ───────────────────────────────────────────

        private void OnConfirm()
        {
            var final = new List<TurretType>(_picked);

            // 부족하면 소유 터렛에서 랜덤 보충
            var ownedPool = new List<TurretType>();
            foreach (var t in _pool)
                if (SaveData.IsOwned(t) && !final.Contains(t))
                    ownedPool.Add(t);

            while (final.Count < maxSelect && ownedPool.Count > 0)
            {
                int ri = Random.Range(0, ownedPool.Count);
                final.Add(ownedPool[ri]);
                ownedPool.RemoveAt(ri);
            }

            SaveData.SelectedTurrets = final.ToArray();

            // 팝업 전부 닫기
            PopupManager.Instance?.CloseAllAndClearCache();

            // 로비 UI 찾아서 GameSetup에 전달
            var lobbyUI = GameObject.Find("LobbyUI");
            if (GameSetup.Instance != null)
            {
                GameSetup.Instance.StartGame(lobbyUI);
            }
            else
            {
                // 폴백: 직접 씬 로드
                SceneManager.LoadScene(gameSceneName);
            }
        }

        // ── 설명 텍스트 ──────────────────────────────────────────────

        private string GetDesc(TurretType t) => t switch
        {
            TurretType.RangedTurret     => "Basic ranged attack\nVersatile all-rounder",
            TurretType.MeleeTurret      => "Powerful melee strike\nSlams enemies in front",
            TurretType.CrossMeleeTurret => "4-way melee attack\nHits all adjacent enemies",
            TurretType.SpikeTrap        => "Deals damage on pass\nGreat on enemy paths",
            TurretType.AreaDamage       => "AOE damage nearby\nBest placed centrally",
            TurretType.ExplosiveCannon  => "Area explosion\nDevastating vs groups",
            TurretType.SlowShooter      => "Slows enemy speed\nSupports ally firepower",
            TurretType.RapidFire        => "Fast attack speed\nEffective vs many enemies",
            TurretType.Tornado          => "Crowd control vortex\nDisrupts enemy movement",
            TurretType.LavaRain         => "Sustained fire damage\nWide area coverage",
            TurretType.ChainLightning   => "Chains to multiple foes\nHits several at once",
            TurretType.BlackHole        => "Pulls + damages\nAdvanced crowd control",
            TurretType.PrecisionStrike  => "Single high-damage snipe\nEliminate priority targets",
            TurretType.GambleBat        => "Random effects\nHigh risk, high reward",
            TurretType.PulseSlower      => "Periodic pulse slow\nWeakens all nearby enemies",
            TurretType.DragonStatue     => "Flame breath left+right\nBurns enemies over time",
            TurretType.HasteTower       => "Buffs nearby turret speed\nSupport specialist",
            TurretType.PinballCannon    => "Bouncing cannonball\nRicochet chain damage",
            TurretType.BoomerangTurret  => "Returns boomerang\nPenetrates on both passes",
            _                           => "",
        };
    }
}
