using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 카드 3장 선택 화면.
    /// ShowCards()로 호출하면 게임을 일시정지하고 카드 UI 표시.
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        public static CardManager Instance { get; private set; }

        [Header("Card Pool (assign in Inspector or auto-loaded)")]
        public List<CardData> cardPool = new List<CardData>();

        // UI 루트 (GameSetup에서 생성)
        private GameObject _panel;
        private System.Action _onDone;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>카드 3장 선택 화면 표시. 선택 완료 시 onDone 호출.</summary>
        public void ShowCards(System.Action onDone = null)
        {
            _onDone = onDone;

            if (cardPool == null || cardPool.Count == 0)
            {
                Debug.LogWarning("[CardManager] No cards in pool!");
                onDone?.Invoke();
                return;
            }

            // 랜덤 3장 뽑기
            var shuffled = new List<CardData>(cardPool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            int count = Mathf.Min(3, shuffled.Count);
            var picks = shuffled.GetRange(0, count);

            BuildUI(picks);
        }

private void BuildUI(List<CardData> cards) { if (_panel != null) Destroy(_panel); var canvas = FindObjectOfType<Canvas>(); if (canvas == null) return; _panel = new GameObject("CardSelectPanel"); _panel.transform.SetParent(canvas.transform, false); var bg = _panel.AddComponent<Image>(); bg.color = new Color(0f, 0f, 0f, 0.85f); var bgRt = _panel.GetComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero; MakeText(_panel, "Title", "Choose a Card", new Vector2(0.5f, 0.88f), new Vector2(350f, 50f), 28, Color.white); float[] xAnchors = { 0.18f, 0.5f, 0.82f }; for (int i = 0; i < cards.Count; i++) { int idx = i; MakeCardButton(_panel, cards[i], new Vector2(xAnchors[i], 0.50f), () => OnCardSelected(cards[idx])); } _panel.transform.SetAsLastSibling(); }

private void MakeCardButton(GameObject parent, CardData card, Vector2 anchor, System.Action onClick) { var go = new GameObject($"Card_{card.cardName}"); go.transform.SetParent(parent.transform, false); var rt = go.AddComponent<RectTransform>(); rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(108f, 200f); var img = go.AddComponent<Image>(); img.color = card.cardColor; var btn = go.AddComponent<Button>(); var colors = btn.colors; colors.highlightedColor = Color.Lerp(card.cardColor, Color.white, 0.3f); colors.pressedColor = Color.Lerp(card.cardColor, Color.black, 0.2f); btn.colors = colors; btn.onClick.AddListener(() => onClick()); MakeText(go, "Name", card.cardName, new Vector2(0.5f, 0.84f), new Vector2(100f, 36f), 12, Color.white); var descGo = new GameObject("Desc"); descGo.transform.SetParent(go.transform, false); var descRt = descGo.AddComponent<RectTransform>(); descRt.anchorMin = new Vector2(0.05f, 0.22f); descRt.anchorMax = new Vector2(0.95f, 0.76f); descRt.offsetMin = Vector2.zero; descRt.offsetMax = Vector2.zero; var descTmp = descGo.AddComponent<TextMeshProUGUI>(); descTmp.text = card.description; descTmp.fontSize = 10; descTmp.color = Color.white; descTmp.alignment = TextAlignmentOptions.Center; descTmp.enableWordWrapping = true; var border = new GameObject("Border"); border.transform.SetParent(go.transform, false); border.transform.SetAsFirstSibling(); var borderRt = border.AddComponent<RectTransform>(); borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one; borderRt.offsetMin = new Vector2(-2,-2); borderRt.offsetMax = new Vector2(2,2); border.AddComponent<Image>().color = new Color(1f,1f,1f,0.2f); }

        private TextMeshProUGUI MakeText(GameObject parent, string name, string text,
            Vector2 anchor, Vector2 size, int fs, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fs; tmp.color = col;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void OnCardSelected(CardData card)
        {
            ApplyCard(card);

            if (_panel != null) Destroy(_panel);
            _onDone?.Invoke();
        }

public void ApplyCard(CardData card) { switch (card.cardType) { case CardType.GiveTurret: InventoryManager.Instance.Add(card.turretType, card.turretCount); UIInventoryPanel.Instance?.Refresh(); UIManager.Instance?.ShowMessage($"+{card.turretCount} {card.cardName}"); break; case CardType.GiveRandomWalls: GiveRandomWalls(); break; case CardType.BuffDamage: ApplyBuff(card, (t) => t.damage *= card.buffMultiplier); break; case CardType.BuffFireRate: ApplyBuff(card, (t) => t.fireRate *= card.buffMultiplier); break; case CardType.BuffRange: ApplyBuff(card, (t) => t.range *= card.buffMultiplier); break; } } private void GiveRandomWalls() { TurretType[] wallTypes = { TurretType.Wall, TurretType.Wall2x1, TurretType.Wall1x2, TurretType.Wall2x2 }; var shuffled = new System.Collections.Generic.List<TurretType>(wallTypes); for (int i = shuffled.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]); } for (int i = 0; i < 3; i++) InventoryManager.Instance.Add(shuffled[i], 1); UIInventoryPanel.Instance?.Refresh(); UIManager.Instance?.ShowMessage("Random Walls x3!"); }

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
