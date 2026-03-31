using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    public class CardManager : MonoBehaviour
    {
        public static CardManager Instance { get; private set; }

        [Header("Card Pool")]
        public List<CardData> cardPool = new List<CardData>();

        private List<TurretType> _sessionTurrets = new List<TurretType>();
        private GameObject _panel;
        private System.Action _onDone;

private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; AutoLoadCards(); }

private void AutoLoadCards()
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Data/Cards" });
            cardPool = new List<CardData>();
            foreach (var g in guids)
            {
                var p    = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(p);
                if (card != null) cardPool.Add(card);
            }
            Debug.Log($"[CardManager] Auto-loaded {cardPool.Count} cards");
#else
            // 빌드: Resources/Cards 폴더에서 로드
            var cards = Resources.LoadAll<CardData>("Cards");
            if (cards == null || cards.Length == 0)
                cards = Resources.LoadAll<CardData>("");
            if (cards != null) cardPool = new List<CardData>(cards);
            Debug.Log($"[CardManager] Build loaded {cardPool.Count} cards");
#endif
        }

        // 이 세션에서 사용할 타워 타입 고정 (카드 필터 기준)
        public void SetSessionTurrets(IEnumerable<TurretType> types)
        {
            _sessionTurrets.Clear();
            foreach (var t in types)
                if (!_sessionTurrets.Contains(t))
                    _sessionTurrets.Add(t);
            Debug.Log($"[CardManager] Session turrets: {string.Join(", ", _sessionTurrets)}");
        }

        public IReadOnlyList<TurretType> SessionTurrets => _sessionTurrets;

public void ShowCards(System.Action onDone = null) { _onDone = onDone; if (cardPool == null || cardPool.Count == 0) { Debug.LogWarning("[CardManager] No cards in pool!"); onDone?.Invoke(); return; } var pool = FilteredPool(); Debug.Log($"[CardManager] ShowCards: pool={cardPool.Count}, filtered={pool.Count}, session=[{string.Join(",", _sessionTurrets)}]"); if (pool.Count == 0) { Debug.LogWarning("[CardManager] Filtered pool empty — using full pool"); pool = new List<CardData>(cardPool); } for (int i = pool.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (pool[i], pool[j]) = (pool[j], pool[i]); } BuildUI(pool.GetRange(0, Mathf.Min(3, pool.Count))); }

        private List<CardData> FilteredPool()
        {
            if (_sessionTurrets == null || _sessionTurrets.Count == 0)
                return new List<CardData>(cardPool);

            var filtered = new List<CardData>();
            foreach (var card in cardPool)
            {
                if (card == null) continue;
                switch (card.cardType)
                {
                    case CardType.GiveTurret:
                        if (_sessionTurrets.Contains(card.turretType))
                            filtered.Add(card);
                        break;
                    case CardType.GiveRandomWalls:
                        filtered.Add(card);
                        break;
                    case CardType.BuffDamage:
                    case CardType.BuffFireRate:
                    case CardType.BuffRange:
                        if (card.buffAllTypes || _sessionTurrets.Contains(card.buffTargetType))
                            filtered.Add(card);
                        break;
                }
            }
            return filtered;
        }

private void BuildUI(List<CardData> cards) { if (_panel != null) Destroy(_panel); var canvas = FindObjectOfType<Canvas>(); if (canvas == null) { Debug.LogError("[CardManager] No Canvas found! Retrying next frame..."); StartCoroutine(BuildUINextFrame(cards)); return; } BuildUIOnCanvas(canvas, cards); } private System.Collections.IEnumerator BuildUINextFrame(List<CardData> cards) { yield return null; var canvas = FindObjectOfType<Canvas>(); if (canvas == null) { Debug.LogError("[CardManager] Still no Canvas! Cards cannot be shown."); _onDone?.Invoke(); yield break; } BuildUIOnCanvas(canvas, cards); } private void BuildUIOnCanvas(Canvas canvas, List<CardData> cards) { if (_panel != null) Destroy(_panel); Time.timeScale = 0f; _panel = new GameObject("CardSelectPanel"); _panel.transform.SetParent(canvas.transform, false); var bg = _panel.AddComponent<Image>(); bg.color = new Color(0f, 0f, 0f, 0.85f); var bgRt = _panel.GetComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero; MakeText(_panel, "Title", "Choose a Card", new Vector2(0.5f, 0.88f), new Vector2(350f, 50f), 28, Color.white); float[] xAnchors = { 0.18f, 0.5f, 0.82f }; for (int i = 0; i < cards.Count; i++) { int idx = i; MakeCardButton(_panel, cards[i], new Vector2(xAnchors[i], 0.50f), () => OnCardSelected(cards[idx])); } _panel.transform.SetAsLastSibling(); Debug.Log($"[CardManager] Card panel built with {cards.Count} cards on canvas '{canvas.name}'"); }

        private void MakeCardButton(GameObject parent, CardData card, Vector2 anchor, System.Action onClick)
        {
            var go = new GameObject($"Card_{card.cardName}");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(108f, 200f);

            var img   = go.AddComponent<Image>();
            img.color = card.cardColor;
            var btn   = go.AddComponent<Button>();
            var col   = btn.colors;
            col.highlightedColor = Color.Lerp(card.cardColor, Color.white, 0.3f);
            col.pressedColor     = Color.Lerp(card.cardColor, Color.black, 0.2f);
            btn.colors = col;
            btn.onClick.AddListener(() => onClick());

            MakeText(go, "Name", card.cardName,
                new Vector2(0.5f, 0.84f), new Vector2(100f, 36f), 12, Color.white);

            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(go.transform, false);
            var descRt = descGo.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.05f, 0.22f); descRt.anchorMax = new Vector2(0.95f, 0.76f);
            descRt.offsetMin = descRt.offsetMax = Vector2.zero;
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = card.description; descTmp.fontSize = 10;
            descTmp.color = Color.white; descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.enableWordWrapping = true;

            var border   = new GameObject("Border");
            border.transform.SetParent(go.transform, false);
            border.transform.SetAsFirstSibling();
            var borderRt = border.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2, -2); borderRt.offsetMax = new Vector2(2, 2);
            border.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
        }

        private TextMeshProUGUI MakeText(GameObject parent, string name, string text,
            Vector2 anchor, Vector2 size, int fs, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fs; tmp.color = col;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void OnCardSelected(CardData card) { Time.timeScale = 1f; ApplyCard(card);
            if (_panel != null) Destroy(_panel);
            _onDone?.Invoke();
        }

        public void ApplyCard(CardData card)
        {
            switch (card.cardType)
            {
                case CardType.GiveTurret:
                    InventoryManager.Instance.Add(card.turretType, card.turretCount);
                    UIInventoryPanel.Instance?.Refresh();
                    UIManager.Instance?.ShowMessage($"+{card.turretCount} {card.cardName}");
                    break;
                case CardType.GiveRandomWalls:
                    GiveRandomWalls();
                    break;
                case CardType.BuffDamage:
                    ApplyBuff(card, t => t.damage *= card.buffMultiplier);
                    break;
                case CardType.BuffFireRate:
                    ApplyBuff(card, t => t.fireRate *= card.buffMultiplier);
                    break;
                case CardType.BuffRange:
                    ApplyBuff(card, t => t.range *= card.buffMultiplier);
                    break;
            }
        }

        private void GiveRandomWalls()
        {
            TurretType[] wallTypes = { TurretType.Wall, TurretType.Wall2x1, TurretType.Wall1x2, TurretType.Wall2x2 };
            var shuffled = new List<TurretType>(wallTypes);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            for (int i = 0; i < 3; i++) InventoryManager.Instance.Add(shuffled[i], 1);
            UIInventoryPanel.Instance?.Refresh();
            UIManager.Instance?.ShowMessage("Random Walls x3!");
        }

        private void ApplyBuff(CardData card, System.Action<TurretBase> buffAction)
        {
            var all = TurretManager.Instance?.GetAll();
            if (all == null) return;
            foreach (var t in all)
            {
                if (t == null) continue;
                if (!card.buffAllTypes && t.turretType != card.buffTargetType) continue;
                buffAction(t);
            }
            UIManager.Instance?.ShowMessage($"{card.cardName}!");
        }
    }
}
