using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Underdark
{
    public class TowerSelectUI : MonoBehaviour
    {
        [Header("References")]
        public Transform cardContainer;
        public Button    confirmButton;
        public Button    backButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI countText;

        [Header("Settings")]
        public string gameSceneName  = "GameScene";
        public string lobbySceneName = "LobbyScene";
        public int    minSelect = 1;
        public int    maxSelect = 4;
        public List<StageData> stages = new List<StageData>();

        private TurretRegistry       _registry;
        private StageData            _stage;
        private List<TurretType>     _pool   = new List<TurretType>();
        private List<TurretType>     _picked = new List<TurretType>();
        private List<GameObject>     _cards  = new List<GameObject>();

        private void Start()
        {
            // Registry 로드 (Resources 폴백)
            _registry = Resources.Load<TurretRegistry>("TurretRegistry");
            if (_registry == null)
                Debug.LogError("[TowerSelectUI] TurretRegistry를 찾을 수 없습니다!");
            else
                _registry.RebuildCache();

            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (backButton    != null) backButton.onClick.AddListener(OnBack);

            int idx = SaveData.SelectedStageIndex;
            if (stages != null && stages.Count > 0)
                _stage = idx < stages.Count ? stages[idx] : stages[0];
            if (_stage != null)
                maxSelect = _stage.startTurretCount > 0 ? _stage.startTurretCount : maxSelect;

            // Registry에서 풀 자동 구성 (벽 타입 제외)
            BuildPoolFromRegistry();

            if (titleText != null)
                titleText.text = _stage != null ? _stage.stageName : "Select Towers";

            BuildCards();
            RefreshConfirm();
        }

        /// <summary>Registry entries에서 isWall=false인 것만 풀에 추가</summary>
        private void BuildPoolFromRegistry()
        {
            _pool.Clear();
            if (_registry == null) return;

            foreach (var entry in _registry.All)
            {
                if (entry.type == TurretType.None) continue;
                if (entry.isWall) continue; // 벽 타입 제외
                _pool.Add(entry.type);
            }
        }

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
            // Registry에서 label/color 자동 읽기
            string label = type.ToString();
            Color  col   = Color.gray;
            string desc  = "";

            if (_registry != null)
            {
                var entry = _registry.Get(type);
                if (entry != null)
                {
                    if (!string.IsNullOrEmpty(entry.label)) label = entry.label;
                    if (entry.color != default)             col   = entry.color;
                }
            }
            desc = GetDesc(type);

            var go = new GameObject($"Card_{type}");
            go.transform.SetParent(cardContainer, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor    = Color.clear;
            outline.effectDistance = new Vector2(4f, -4f);

            var btn = go.AddComponent<Button>();

            // Color strip (top 38%)
            var strip = new GameObject("Strip");
            strip.transform.SetParent(go.transform, false);
            var sr = strip.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0.62f); sr.anchorMax = Vector2.one;
            sr.offsetMin = sr.offsetMax = Vector2.zero;
            strip.AddComponent<Image>().color = col;

            // Tower name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(strip.transform, false);
            var nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            var namTmp = nameGo.AddComponent<TextMeshProUGUI>();
            namTmp.text = label; namTmp.fontSize = 40f;
            namTmp.fontStyle = FontStyles.Bold;
            namTmp.color = Color.white;
            namTmp.alignment = TextAlignmentOptions.Center;

            // Check mark (top-right corner of strip)
            var chkGo = new GameObject("Check");
            chkGo.transform.SetParent(strip.transform, false);
            var crt = chkGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.7f, 0f); crt.anchorMax = Vector2.one;
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var chkTmp = chkGo.AddComponent<TextMeshProUGUI>();
            chkTmp.text = "✓"; chkTmp.fontSize = 20f;
            chkTmp.fontStyle = FontStyles.Bold;
            chkTmp.color = Color.white;
            chkTmp.alignment = TextAlignmentOptions.MidlineRight;
            chkGo.SetActive(false);

            // Description
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(go.transform, false);
            var drt = descGo.AddComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = new Vector2(1f, 0.62f);
            drt.offsetMin = new Vector2(6f, 2f); drt.offsetMax = new Vector2(-6f, -2f);
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = desc; descTmp.fontSize = 30f;
            descTmp.color = new Color(0.85f, 0.85f, 0.92f);
            descTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var capType = type;
            btn.onClick.AddListener(() => ToggleCard(capType, go, outline, chkGo, bg, col));
            return go;
        }

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

        private void RefreshConfirm()
        {
            if (countText != null)
                countText.text = $"Select {_picked.Count} / {maxSelect}";
            if (confirmButton != null)
                confirmButton.interactable = _picked.Count >= minSelect;
        }

        public void OnConfirm()
        {
            var final = new List<TurretType>(_picked);
            while (final.Count < maxSelect && _pool.Count > 0)
                final.Add(_pool[Random.Range(0, _pool.Count)]);
            SaveData.SelectedTurrets = final.ToArray();
            SceneManager.LoadScene(gameSceneName);
        }

        public void OnBack()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        // 설명 텍스트 - 새 터렛 추가 시 여기에 case 추가 (선택사항, 없으면 빈 칸)
        private string GetDesc(TurretType t) => t switch
        {
            TurretType.RangedTurret     => "Basic ranged attack\nVersatile all-rounder",
            TurretType.MeleeTurret      => "Powerful melee strike\nSlams enemies in front",
            TurretType.CrossMeleeTurret => "4-way melee attack\nHits all adjacent enemies",
            TurretType.SpikeTrap        => "Deals damage on pass\nGreat on enemy paths",
            TurretType.ElectricGate     => "Shock + slow\nBest in narrow corridors",
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
