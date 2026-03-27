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
        public string gameSceneName = "GameScene";
        public string lobbySceneName = "LobbyScene";
        public int    minSelect = 1;
        public int    maxSelect = 4;
        public List<StageData> stages = new List<StageData>();

        // All available turret types (shown when stage has no pool)
        private static readonly TurretType[] ALL_TYPES = new TurretType[]
        {
            TurretType.RangedTurret,
            TurretType.MeleeTurret,
            TurretType.SpikeTrap,
            TurretType.ElectricGate,
            TurretType.AreaDamage,
            TurretType.ExplosiveCannon,
            TurretType.SlowShooter,
            TurretType.RapidFire,
            TurretType.Tornado,
            TurretType.LavaRain,
            TurretType.ChainLightning,
            TurretType.BlackHole,
            TurretType.PrecisionStrike,
            TurretType.GambleBat,
        };

        private static readonly Dictionary<TurretType, (string label, Color color)> _info
            = new Dictionary<TurretType, (string, Color)>
        {
            { TurretType.RangedTurret,    ("Ranged",      new Color(0.3f,0.6f,1f))     },
            { TurretType.MeleeTurret,     ("Melee",       new Color(0.9f,0.7f,0.2f))   },
            { TurretType.SpikeTrap,       ("Spikes",      new Color(0.4f,0.35f,0.3f))  },
            { TurretType.ElectricGate,    ("Elec Gate",   new Color(0.9f,0.8f,0.1f))   },
            { TurretType.Wall,            ("Wall 1x1",    new Color(0.55f,0.45f,0.35f)) },
            { TurretType.Wall2x1,         ("Wall 2x1",    new Color(0.5f,0.4f,0.3f))   },
            { TurretType.Wall1x2,         ("Wall 1x2",    new Color(0.5f,0.4f,0.3f))   },
            { TurretType.Wall2x2,         ("Wall 2x2",    new Color(0.45f,0.35f,0.25f)) },
            { TurretType.AreaDamage,      ("Area Dmg",    new Color(0.7f,0.2f,0.85f))  },
            { TurretType.ExplosiveCannon, ("Cannon",      new Color(1f,0.4f,0.1f))      },
            { TurretType.SlowShooter,     ("Slow",        new Color(0.3f,0.6f,1f))      },
            { TurretType.RapidFire,       ("Rapid",       new Color(1f,0.85f,0.2f))     },
            { TurretType.Tornado,         ("Tornado",     new Color(0.5f,0.85f,1f))     },
            { TurretType.LavaRain,        ("Lava Rain",   new Color(1f,0.3f,0f))        },
            { TurretType.ChainLightning,  ("Chain Bolt",  new Color(0.4f,0.8f,1f))      },
            { TurretType.BlackHole,       ("Black Hole",  new Color(0.5f,0f,0.8f))      },
            { TurretType.PrecisionStrike, ("Precision",   new Color(1f,0.95f,0.2f))     },
            { TurretType.GambleBat,       ("Gamble Bat",  new Color(0.9f,0.3f,0.9f))   },
        };

        private StageData        _stage;
        private List<TurretType> _pool   = new List<TurretType>();
        private List<TurretType> _picked = new List<TurretType>();
        private List<GameObject> _cards  = new List<GameObject>();

private void Start() { if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm); if (backButton != null) backButton.onClick.AddListener(OnBack); int idx = SaveData.SelectedStageIndex; if (stages != null && stages.Count > 0) _stage = idx < stages.Count ? stages[idx] : stages[0]; if (_stage != null) maxSelect = _stage.startTurretCount > 0 ? _stage.startTurretCount : maxSelect; _pool.AddRange(ALL_TYPES); if (titleText != null) titleText.text = _stage != null ? _stage.stageName : "Select Towers"; BuildCards(); RefreshConfirm(); }

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
            _info.TryGetValue(type, out var info);
            string label = !string.IsNullOrEmpty(info.label) ? info.label : type.ToString();
            Color  col   = info.color != default ? info.color : Color.gray;

            var go = new GameObject($"Card_{type}");
            go.transform.SetParent(cardContainer, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor    = Color.clear;
            outline.effectDistance = new Vector2(4f, -4f);

            var btn = go.AddComponent<Button>();

            // Color strip (top 35%)
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
            namTmp.text = label; namTmp.fontSize = 18f;
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
            descTmp.text = GetDesc(type); descTmp.fontSize = 11f;
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
            var go = _cards[idx];
            _info.TryGetValue(type, out var info);
            Color col = info.color != default ? info.color : Color.gray;
            var bg = go.GetComponent<Image>();
            if (bg != null) bg.color = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f);
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

        private string GetDesc(TurretType t)
        {
            switch (t)
            {
                case TurretType.RangedTurret:    return "Basic ranged attack\nVersatile all-rounder";
                case TurretType.MeleeTurret:     return "Powerful melee strike\nSlams enemies in front";
                case TurretType.SpikeTrap:       return "Deals damage on pass\nGreat on enemy paths";
                case TurretType.ElectricGate:    return "Shock + slow\nBest in narrow corridors";
                case TurretType.Wall:            return "Blocks path (1x1)\nForces enemies to reroute";
                case TurretType.Wall2x1:         return "Blocks path (2x1)\nWide horizontal wall";
                case TurretType.Wall1x2:         return "Blocks path (1x2)\nTall vertical wall";
                case TurretType.Wall2x2:         return "Blocks path (2x2)\nLarge area wall";
                case TurretType.SlowShooter:     return "Slows enemy speed\nSupports ally firepower";
                case TurretType.RapidFire:       return "Fast attack speed\nEffective vs many enemies";
                case TurretType.ExplosiveCannon: return "Area explosion\nDevastating vs groups";
                case TurretType.AreaDamage:      return "AOE damage nearby\nBest placed centrally";
                case TurretType.Tornado:         return "Crowd control vortex\nDisrupts enemy movement";
                case TurretType.LavaRain:        return "Sustained fire damage\nWide area coverage";
                case TurretType.ChainLightning:  return "Chains to multiple foes\nHits several at once";
                case TurretType.BlackHole:       return "Pulls + damages\nAdvanced crowd control";
                case TurretType.PrecisionStrike: return "Single high-damage snipe\nEliminate priority targets";
                case TurretType.GambleBat:       return "Random effects\nHigh risk, high reward";
                default:                         return "";
            }
        }
    }
}
