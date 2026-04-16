using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 로비 메인 UI 컨트롤러.
    /// StageCard 프리팹을 Instantiate해서 스테이지 카드를 동적 생성.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Scenes")]
        public string towerSelectScene = "TowerSelectScene";

        [Header("Stage Card Prefab")]
        [Tooltip("StageCard 프리팹 - 이걸 복제해서 스테이지 카드를 만듦")]
        public GameObject stageCardPrefab;

        [Header("References - TopHUD")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI gemText;

        [Header("References - Stage Info")]
        public TextMeshProUGUI stageNameText;
        public TextMeshProUGUI stageInfoText;

        [Header("References - Scroll")]
        public ScrollRect stageScroll;
        public RectTransform scrollContent;

        [Header("References - Bottom")]
        public Button          playButton;
        public TextMeshProUGUI playButtonLabel;

        // ── 색상 ────────────────────────────────────────────────────
        private static readonly Color COL_CLEAR = new Color(0.25f, 0.85f, 0.40f);
        private static readonly Color COL_AVAIL = new Color(0.30f, 0.65f, 1.00f);
        private static readonly Color COL_LOCK  = new Color(0.35f, 0.35f, 0.40f);
        private static readonly Color COL_PLAY  = new Color(0.15f, 0.70f, 0.25f);

        // ── 런타임 상태 ─────────────────────────────────────────────
        private List<StageData>  _stages = new List<StageData>();
        private int              _selectedIndex;
        private bool             _isDragging;
        private List<GameObject> _cards = new List<GameObject>();

        // ────────────────────────────────────────────────────────────

        private void Start()
        {
            LoadStagesFromRegistry();

            _selectedIndex = GetCurrentPlayableIndex();

            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClick);

            // 프리팹으로 카드 생성
            BuildCards();

            // 드래그 이벤트 감지
            if (stageScroll != null)
            {
                var trigger = stageScroll.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

                var beginEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.BeginDrag };
                beginEntry.callback.AddListener(_ => _isDragging = true);
                trigger.triggers.Add(beginEntry);

                var endEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                    { eventID = UnityEngine.EventSystems.EventTriggerType.EndDrag };
                endEntry.callback.AddListener(_ => OnEndDrag());
                trigger.triggers.Add(endEntry);
            }

           RefreshAll();

            // 레이아웃 계산이 끝난 뒤 스냅 (1프레임 대기)
            StartCoroutine(InitAfterLayout());
        }

        // ── 스테이지 로드 ────────────────────────────────────────────

        private void LoadStagesFromRegistry()
        {
            var registry = Resources.Load<StageRegistry>("StageRegistry");
            if (registry != null)
            {
                _stages = registry.ValidStages();
                return;
            }

            // 폴백: Resources/Stages/*.asset 직접 로드
            _stages = new List<StageData>();
            var loaded = Resources.LoadAll<StageData>("Stages");
            var sorted = new List<StageData>(loaded);
            sorted.Sort((a, b) => string.Compare(a.stageName, b.stageName, System.StringComparison.Ordinal));
            _stages.AddRange(sorted);
        }

        // ── 카드 생성 ────────────────────────────────────────────────

        private void BuildCards()
        {
            if (scrollContent == null) return;

            // 기존 카드 제거
            foreach (Transform c in scrollContent)
                Destroy(c.gameObject);
            _cards.Clear();

            if (stageCardPrefab == null)
            {
                Debug.LogError("[LobbyUI] stageCardPrefab이 연결되지 않았습니다!");
                return;
            }

            for (int i = 0; i < _stages.Count; i++)
            {
                var card = Instantiate(stageCardPrefab, scrollContent);
                card.name = $"Card_{i}";
                SetupCard(card, i, _stages[i]);
                _cards.Add(card);
            }
        }

        /// <summary>카드 프리팹에 스테이지 데이터를 채움</summary>
        private void SetupCard(GameObject card, int idx, StageData sd)
        {
            bool cleared  = SaveData.IsCleared(idx);
            bool unlocked = SaveData.IsUnlocked(idx);

            // Outline 색상
            var ol = card.GetComponent<Outline>();
            if (ol != null)
                ol.effectColor = cleared ? COL_CLEAR : unlocked ? COL_AVAIL : COL_LOCK;

            // 썸네일
            var thumbImg = card.transform.Find("Thumb")?.GetComponent<Image>();
            if (thumbImg != null)
            {
                if (!unlocked)
                {
                    thumbImg.color  = new Color(0.1f, 0.1f, 0.15f);
                    thumbImg.sprite = null;
                }
                else if (sd.thumbnail != null)
                {
                    thumbImg.sprite         = sd.thumbnail;
                    thumbImg.preserveAspect = true;
                    thumbImg.color          = Color.white;
                }
                else
                {
                    thumbImg.color  = new Color(0.18f, 0.20f, 0.35f);
                    thumbImg.sprite = null;
                }
            }

            // 잠금 텍스트 (Thumb/Lock)
            var lockTmp = card.transform.Find("Thumb/Lock")?.GetComponent<TextMeshProUGUI>();
            if (lockTmp != null)
                lockTmp.gameObject.SetActive(!unlocked);

            // 스테이지 번호 텍스트 (Thumb/StageNum)
            var numTmp = card.transform.Find("Thumb/StageNum")?.GetComponent<TextMeshProUGUI>();
            if (numTmp != null)
            {
                numTmp.gameObject.SetActive(unlocked && sd.thumbnail == null);
                numTmp.text = $"STAGE\n{idx + 1}";
            }

            // 이름
            var nameTmp = card.transform.Find("Info/Name")?.GetComponent<TextMeshProUGUI>();
            if (nameTmp != null)
                nameTmp.text = unlocked ? sd.stageName : $"Stage {idx + 1}";

            // 웨이브 수
            var waveTmp = card.transform.Find("Info/Waves")?.GetComponent<TextMeshProUGUI>();
            if (waveTmp != null)
                waveTmp.text = unlocked ? $"{sd.waves?.Count ?? 0} Waves" : "???";

            // 상태 뱃지
            var statusTmp = card.transform.Find("Info/Status")?.GetComponent<TextMeshProUGUI>();
            if (statusTmp != null)
            {
                statusTmp.text  = cleared ? "✓ CLEAR" : unlocked ? "PLAY" : "🔒";
                statusTmp.color = cleared ? COL_CLEAR : unlocked ? COL_AVAIL : COL_LOCK;
            }

            // 버튼 클릭
            var btn = card.GetComponent<Button>();
            if (btn != null)
            {
                int captured = idx;
                btn.onClick.AddListener(() => SelectCard(captured));
            }

           
        }

        // ── 초기화 ──────────────────────────────────────────────────

        private IEnumerator InitAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            if (scrollContent != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

            ApplyScrollPadding();
            UpdateCardVisuals(_selectedIndex);
            UpdateStageInfo(_selectedIndex);
            SnapImmediate(_selectedIndex);
        }

        private void ApplyScrollPadding()
        {
            if (scrollContent == null || _cards.Count == 0) return;

            float cardStride = GetCardStride();
            float cardW      = cardStride - 16f;
            float viewW      = GetViewportWidth();
            float padding    = Mathf.Max(0f, (viewW - cardW) * 0.5f);

            float cardAreaW = _cards.Count * cardStride + 16f;
            scrollContent.sizeDelta = new Vector2(cardAreaW + padding * 2f, scrollContent.sizeDelta.y);

            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                var rt = _cards[i].GetComponent<RectTransform>();
                if (rt == null) continue;
                float origX = 16f + i * cardStride;
                rt.anchoredPosition = new Vector2(padding + origX, rt.anchoredPosition.y);
            }
        }

        // ── 카드 선택 & 스냅 ────────────────────────────────────────

        private int GetCurrentPlayableIndex()
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                if (!SaveData.IsCleared(i))
                    return i;
            }
            return Mathf.Max(0, _stages.Count - 1);
        }

        public void SelectCard(int idx, bool animate = true)
        {
            if (_stages.Count == 0) return;
            idx = Mathf.Clamp(idx, 0, _stages.Count - 1);
            _selectedIndex = idx;
            SaveData.SelectedStageIndex = idx;

            UpdateCardVisuals(idx);
            UpdateStageInfo(idx);

            if (!_isDragging)
            {
                if (animate) StartCoroutine(SnapToCard(idx));
                else         SnapImmediate(idx);
            }
        }

        private void UpdateCardVisuals(int idx)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                var ol = _cards[i].GetComponent<Outline>();
                if (ol != null)
                    ol.effectDistance = (i == idx) ? new Vector2(3, -3) : new Vector2(1, -1);
            }
        }

        private void UpdateStageInfo(int idx)
        {
            if (_stages.Count == 0) return;
            var sd       = _stages[idx];
            bool cleared  = SaveData.IsCleared(idx);
            bool unlocked = SaveData.IsUnlocked(idx);

            if (stageNameText != null)
                stageNameText.text = unlocked ? sd.stageName : $"Stage {idx + 1}";
            if (stageInfoText != null)
                stageInfoText.text = unlocked
                    ? $"{sd.waves?.Count ?? 0} Waves  {(cleared ? "✓ Cleared" : "")}"
                    : "???";

            if (playButton != null)
            {
                playButton.interactable = unlocked;
                var img = playButton.GetComponent<Image>();
                if (img != null) img.color = unlocked ? COL_PLAY : COL_LOCK;
            }
            if (playButtonLabel != null)
                playButtonLabel.text = !unlocked ? "🔒  LOCKED"
                    : cleared         ? "▶  REPLAY"
                                      : "▶  PLAY";
        }

        // ── 전체 갱신 ────────────────────────────────────────────────

        private void RefreshAll()
        {
            if (goldText != null) goldText.text = "0";
            if (gemText  != null) gemText.text  = "0";
        }

        // ── 드래그 끝 → 스냅 ────────────────────────────────────────

        private void OnEndDrag()
        {
            _isDragging = false;
            int nearest = GetNearestCardIndex();
            if (nearest != _selectedIndex)
            {
                _selectedIndex = nearest;
                SaveData.SelectedStageIndex = nearest;
                UpdateCardVisuals(nearest);
                UpdateStageInfo(nearest);
            }
            StartCoroutine(SnapToCard(_selectedIndex));
        }

        private int GetNearestCardIndex()
        {
            if (stageScroll == null || scrollContent == null || _stages.Count == 0) return 0;

            float cardStride       = GetCardStride();
            float contentW         = GetContentWidth();
            float viewW            = GetViewportWidth();

            if (contentW <= viewW) return 0;

            float scrolledX        = stageScroll.horizontalNormalizedPosition * (contentW - viewW);
            float padding          = GetCurrentPadding();
            float cardCenterOffset = padding + 16f + (cardStride - 16f) * 0.5f;
            int   nearest          = Mathf.RoundToInt((scrolledX + viewW * 0.5f - cardCenterOffset) / cardStride);
            return Mathf.Clamp(nearest, 0, _stages.Count - 1);
        }

        // ── 스냅 ────────────────────────────────────────────────────

        private float GetContentWidth()
        {
            float w = scrollContent.rect.width;
            return w > 0 ? w : scrollContent.sizeDelta.x;
        }

        private float GetViewportWidth()
        {
            var vp = stageScroll.viewport != null
                ? stageScroll.viewport
                : stageScroll.GetComponent<RectTransform>();
            float w = vp.rect.width;
            return w > 0 ? w : vp.sizeDelta.x;
        }

        private float GetCurrentPadding()
        {
            float cardStride = GetCardStride();
            float cardW      = cardStride - 16f;
            float viewW      = GetViewportWidth();
            return Mathf.Max(0f, (viewW - cardW) * 0.5f);
        }

        private float GetCardCenterX(int idx)
        {
            float cardStride = GetCardStride();
            float padding    = GetCurrentPadding();
            float cardW      = cardStride - 16f;
            return padding + 16f + idx * cardStride + cardW * 0.5f;
        }

        private void SnapImmediate(int idx)
        {
            if (stageScroll == null || scrollContent == null) return;

            float contentW = GetContentWidth();
            float viewW    = GetViewportWidth();

            if (contentW <= viewW)
            {
                stageScroll.horizontalNormalizedPosition = 0;
                return;
            }

            float cardCenterX = GetCardCenterX(idx);
            float scrolledX   = cardCenterX - viewW * 0.5f;
            float t = Mathf.Clamp01(scrolledX / (contentW - viewW));
            stageScroll.horizontalNormalizedPosition = t;
        }

        private IEnumerator SnapToCard(int idx)
        {
            if (stageScroll == null || scrollContent == null) yield break;

            yield return null;

            float contentW = GetContentWidth();
            float viewW    = GetViewportWidth();

            if (contentW <= viewW) yield break;

            float cardCenterX = GetCardCenterX(idx);
            float scrolledX   = cardCenterX - viewW * 0.5f;
            float targetT     = Mathf.Clamp01(scrolledX / (contentW - viewW));

            bool prevInertia = stageScroll.inertia;
            stageScroll.inertia = false;

            float cur = stageScroll.horizontalNormalizedPosition;
            float elapsed = 0f, dur = 0.20f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                stageScroll.horizontalNormalizedPosition = Mathf.Lerp(cur, targetT, elapsed / dur);
                yield return null;
            }
            stageScroll.horizontalNormalizedPosition = targetT;
            stageScroll.inertia = prevInertia;
        }

        private float GetCardStride()
        {
            if (_cards.Count > 0 && _cards[0] != null)
            {
                var rt = _cards[0].GetComponent<RectTransform>();
                if (rt != null)
                {
                    float w = rt.rect.width > 0 ? rt.rect.width : rt.sizeDelta.x;
                    return w + 16f;
                }
            }
            return 316f;
        }

        // ── 버튼 콜백 ────────────────────────────────────────────────

        private void OnPlayClick()
        {
            if (!SaveData.IsUnlocked(_selectedIndex)) return;
            SaveData.SelectedStageIndex = _selectedIndex;
            SceneManager.LoadScene(towerSelectScene);
        }

        public void OnShopClick()
        {
            PopupManager.Instance?.Open<ShopPopup>("PF_ShopPopup");
        }

        public void OnCollectionClick()
        {
            Debug.Log("[LobbyUI] 컬렉션 (미구현)");
        }
    }
}
