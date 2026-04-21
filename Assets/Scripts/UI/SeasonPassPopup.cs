using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 시즌패스 팝업.
    /// 열기: PopupManager.Instance.Open&lt;SeasonPassPopup&gt;("PF_SeasonPassPopup");
    /// 보상 데이터: Resources/PassRewardTable.asset (PassRewardData ScriptableObject)
    ///   → Underdark > Pass Reward > Create Default Table 로 생성
    ///   → Inspector에서 레벨별 보상 편집
    /// </summary>
    public class SeasonPassPopup : Popup
    {
        [Header("UI 레퍼런스")]
        public Button closeButton;
        public TextMeshProUGUI levelText;       // "LV. 5 / 20"
        public TextMeshProUGUI passStatusText;  // "무료" or "패스 보유 중"
        public Button buyPassButton;
        public Transform rowContainer;          // 스크롤 Content

        [Header("스크롤")]
        public ScrollRect scrollRect;

        // 레벨 행 캐시
        private readonly List<GameObject> _rows = new List<GameObject>();

        // 보상 테이블 (Resources 자동 로드)
        private PassRewardData _table;

        // 색상
        private static readonly Color COL_LOCKED   = new Color(0.20f, 0.20f, 0.23f);
        private static readonly Color COL_UNLOCKED = new Color(0.15f, 0.45f, 0.20f);
        private static readonly Color COL_CLAIMED  = new Color(0.25f, 0.25f, 0.28f);
        private static readonly Color COL_CURRENT  = new Color(0.80f, 0.60f, 0.10f);
        private static readonly Color COL_PAID_BG  = new Color(0.30f, 0.15f, 0.05f);

        protected override void Awake()
        {
            base.Awake();

            _table = Resources.Load<PassRewardData>(PassRewardData.RESOURCE_PATH);
            if (_table == null)
                Debug.LogError("[SeasonPassPopup] PassRewardTable.asset 없음! " +
                    "Underdark > Pass Reward > Create Default Table 실행 필요");

            if (closeButton   != null) closeButton.onClick.AddListener(Close);
            if (buyPassButton != null) buyPassButton.onClick.AddListener(OnBuyPassClick);
        }

        public override void OnOpen()
        {
            Refresh();
            StartCoroutine(ScrollToCurrentLevel());
        }

        public override void OnClose() { }

        // ── 전체 갱신 ────────────────────────────────────────────────

        public void Refresh()
        {
            int  maxLv = _table != null ? _table.MaxLevel : SaveData.PASS_MAX_LEVEL;
            int  lv    = SaveData.PassLevel;
            bool owned = SaveData.PassOwned;

            if (levelText      != null) levelText.text = $"LV. {lv} / {maxLv}";
            if (passStatusText != null)
            {
                passStatusText.text  = owned ? "✓ 패스 보유 중" : "무료 패스";
                passStatusText.color = owned
                    ? new Color(1f, 0.85f, 0.2f)
                    : new Color(0.7f, 0.7f, 0.7f);
            }
            if (buyPassButton != null)
                buyPassButton.gameObject.SetActive(!owned);

            BuildRows(maxLv);
        }

        // ── 행 생성 ──────────────────────────────────────────────────

        private void BuildRows(int maxLv)
        {
            if (rowContainer == null) return;

            foreach (var r in _rows) if (r) Destroy(r);
            _rows.Clear();

            // Content에 VerticalLayoutGroup + ContentSizeFitter 보장
            var vlg = rowContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = rowContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing           = 2f;
                vlg.childControlWidth  = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth  = true;
                vlg.childForceExpandHeight = false;
                vlg.padding = new RectOffset(0, 0, 0, 0);
            }
            var csf = rowContainer.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = rowContainer.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            for (int i = 1; i <= maxLv; i++)
                _rows.Add(MakeRow(i));
        }

        private GameObject MakeRow(int level)
        {
            int  lv        = SaveData.PassLevel;
            bool passOwned = SaveData.PassOwned;
            bool isReached = level <= lv;
            bool isCurrent = level == lv;

            var entry = _table?.GetEntry(level);

            // 행 루트 - VerticalLayoutGroup이 배치하므로 고정 높이만 지정
            var row   = new GameObject($"Row_{level}");
            row.transform.SetParent(rowContainer, false);
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 0f);
            rowRt.anchorMax = new Vector2(1f, 0f);
            rowRt.sizeDelta = new Vector2(0f, 72f);

            var layoutElem = row.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 72f;
            layoutElem.flexibleWidth   = 1f;

            var rowBg = row.AddComponent<UnityEngine.UI.Image>();
            rowBg.color = isCurrent
                ? new Color(COL_CURRENT.r, COL_CURRENT.g, COL_CURRENT.b, 0.25f)
                : new Color(1f, 1f, 1f, isReached ? 0.06f : 0.03f);
            rowBg.raycastTarget = false;

            // 레벨 번호
            var lvGo  = new GameObject("LvNum");
            lvGo.transform.SetParent(row.transform, false);
            var lvTmp = lvGo.AddComponent<TextMeshProUGUI>();
            lvTmp.text          = level.ToString();
            lvTmp.fontSize      = isCurrent ? 22 : 18;
            lvTmp.fontStyle     = isCurrent ? FontStyles.Bold : FontStyles.Normal;
            lvTmp.alignment     = TextAlignmentOptions.Center;
            lvTmp.color         = isCurrent ? COL_CURRENT
                                : isReached ? Color.white
                                : new Color(0.5f, 0.5f, 0.5f);
            lvTmp.raycastTarget = false;
            var lvRt = lvGo.GetComponent<RectTransform>();
            lvRt.anchorMin = new Vector2(0f,    0f);
            lvRt.anchorMax = new Vector2(0.10f, 1f);
            lvRt.offsetMin = lvRt.offsetMax = Vector2.zero;

            // 무료 보상 칸
            string freeLabel = entry?.freeReward.GetDisplayName() ?? "?";
            MakeRewardCell(row.transform, level, paid: false, isReached, passOwned,
                left: 0.11f, right: 0.55f, label: freeLabel);

            // 유료 보상 칸
            string paidLabel = entry?.paidReward.GetDisplayName() ?? "?";
            MakeRewardCell(row.transform, level, paid: true, isReached, passOwned,
                left: 0.57f, right: 1.00f, label: paidLabel);

            return row;
        }

        private void MakeRewardCell(Transform parent, int level, bool paid,
            bool isReached, bool passOwned, float left, float right, string label)
        {
            bool claimed  = SaveData.IsPassClaimed(level, paid);
            bool canClaim = isReached && !claimed && (!paid || passOwned);
            bool locked   = paid && !passOwned;

            var cell   = new GameObject(paid ? "PaidCell" : "FreeCell");
            cell.transform.SetParent(parent, false);
            var cellRt = cell.AddComponent<RectTransform>();
            cellRt.anchorMin = new Vector2(left,   0.08f);
            cellRt.anchorMax = new Vector2(right,  0.92f);
            cellRt.offsetMin = new Vector2(4f,  0f);
            cellRt.offsetMax = new Vector2(-4f, 0f);

            var cellBg = cell.AddComponent<UnityEngine.UI.Image>();
            if      (claimed)  cellBg.color = COL_CLAIMED;
            else if (locked)   cellBg.color = COL_PAID_BG;
            else if (canClaim) cellBg.color = COL_UNLOCKED;
            else               cellBg.color = COL_LOCKED;

            // 보상 텍스트
            var textGo = new GameObject("RewardText");
            textGo.transform.SetParent(cell.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text             = claimed  ? "✓ 수령"
                                 : locked   ? "🔒 " + label
                                 : label;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin      = 9;
            tmp.fontSizeMax      = 14;
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.color            = claimed  ? new Color(0.5f, 0.5f, 0.5f)
                                 : locked   ? new Color(0.6f, 0.4f, 0.2f)
                                 : canClaim ? Color.white
                                 : new Color(0.55f, 0.55f, 0.55f);
            tmp.raycastTarget    = false;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0.35f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(4f, 0f);
            textRt.offsetMax = new Vector2(-4f, 0f);

            // 수령 버튼
            if (canClaim)
            {
                var btnGo  = new GameObject("ClaimBtn");
                btnGo.transform.SetParent(cell.transform, false);
                var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
                btnImg.color = paid
                    ? new Color(0.9f, 0.6f, 0.1f)
                    : new Color(0.2f, 0.75f, 0.3f);
                var btn   = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.05f, 0.02f);
                btnRt.anchorMax = new Vector2(0.95f, 0.38f);
                btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;

                var btnTextGo = new GameObject("Text");
                btnTextGo.transform.SetParent(btnGo.transform, false);
                var btnTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
                btnTmp.text          = "수령";
                btnTmp.fontSize      = 12;
                btnTmp.fontStyle     = FontStyles.Bold;
                btnTmp.alignment     = TextAlignmentOptions.Center;
                btnTmp.color         = Color.white;
                btnTmp.raycastTarget = false;
                var btnTextRt = btnTextGo.GetComponent<RectTransform>();
                btnTextRt.anchorMin = Vector2.zero;
                btnTextRt.anchorMax = Vector2.one;
                btnTextRt.offsetMin = btnTextRt.offsetMax = Vector2.zero;

                int  capLv   = level;
                bool capPaid = paid;
                btn.onClick.AddListener(() => OnClaimClick(capLv, capPaid));
            }
        }

        // ── 보상 수령 ────────────────────────────────────────────────

        private void OnClaimClick(int level, bool paid)
        {
            if (SaveData.IsPassClaimed(level, paid)) return;
            if (level > SaveData.PassLevel)          return;
            if (paid && !SaveData.PassOwned)         return;

            SaveData.SetPassClaimed(level, paid);

            var entry  = _table?.GetEntry(level);
            var reward = paid ? entry?.paidReward : entry?.freeReward;
            if (reward != null) GiveReward(reward);

            Refresh();
        }

        private void GiveReward(PassReward reward)
        {
            switch (reward.type)
            {
                case PassRewardType.Gold:
                    SaveData.Gold += reward.amount;
                    Debug.Log($"[SeasonPass] 골드 +{reward.amount}");
                    break;
                case PassRewardType.Gem:
                    SaveData.Gem += reward.amount;
                    Debug.Log($"[SeasonPass] 보석 +{reward.amount}");
                    break;
                case PassRewardType.TurretCard:
                    Debug.Log($"[SeasonPass] 터렛 카드 +{reward.amount}장" +
                        (reward.specificCard != null ? $" ({reward.specificCard.cardName})" : " (랜덤)"));
                    break;
                case PassRewardType.LootBox:
                    Debug.Log($"[SeasonPass] 상자 +{reward.amount}개");
                    break;
            }
        }

        private void OnBuyPassClick()
        {
            SaveData.PassOwned = true;
            Debug.Log("[SeasonPass] 패스 구매 완료");
            Refresh();
        }

        // ── 현재 레벨로 스크롤 ───────────────────────────────────────

        private IEnumerator ScrollToCurrentLevel()
        {
            // VerticalLayoutGroup이 레이아웃 계산을 완료할 때까지 두 프레임 대기
            yield return null;
            yield return new WaitForEndOfFrame();
            if (scrollRect == null || rowContainer == null || _rows.Count == 0) yield break;

            int lv = SaveData.PassLevel;
            if (lv <= 0) yield break;

            int idx = Mathf.Clamp(lv - 1, 0, _rows.Count - 1);
            if (_rows[idx] == null) yield break;

            var contentRt = rowContainer.GetComponent<RectTransform>();
            var rowRt     = _rows[idx].GetComponent<RectTransform>();
            if (contentRt == null || rowRt == null) yield break;

            float contentH = contentRt.rect.height;
            float viewH    = scrollRect.viewport != null
                ? scrollRect.viewport.rect.height
                : scrollRect.GetComponent<RectTransform>().rect.height;
            if (contentH <= viewH) yield break;

            float rowY    = -rowRt.anchoredPosition.y;
            float targetT = Mathf.Clamp01((rowY - viewH * 0.5f) / (contentH - viewH));
            scrollRect.verticalNormalizedPosition = 1f - targetT;
        }
    }
}
