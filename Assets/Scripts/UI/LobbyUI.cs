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
    /// LobbySceneBaker가 에디터에서 씬을 구성하며, 이 스크립트는
    /// 인스펙터에서 연결된 참조로 런타임 로직(스테이지 선택, 버튼 등)만 담당.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Scenes")]
        public string towerSelectScene = "TowerSelectScene";

        [Header("Stage Data")]
        public List<StageData> stages = new List<StageData>();

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
        private int  _selectedIndex;
        private bool _isDragging;
        private List<GameObject> _cards = new List<GameObject>();

        // ────────────────────────────────────────────────────────────

        private void Start()
        {
            if (stages == null || stages.Count == 0)
                LoadStagesFromResources();

            _selectedIndex = GetCurrentPlayableIndex();

            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClick);

            // scrollContent 하위 Card_* 오브젝트 수집
            if (scrollContent != null)
            {
                _cards.Clear();
                foreach (Transform c in scrollContent)
                    _cards.Add(c.gameObject);
            }

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

        private IEnumerator InitAfterLayout()
        {
            // Canvas 레이아웃이 완전히 계산될 때까지 대기
            yield return new WaitForEndOfFrame();
            // 강제 레이아웃 갱신
            if (scrollContent != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

            // 첫/마지막 카드가 뷰포트 중앙에 올 수 있도록 Content 양쪽에 패딩 추가
            ApplyScrollPadding();

            UpdateCardVisuals(_selectedIndex);
            UpdateStageInfo(_selectedIndex);
            SnapImmediate(_selectedIndex);
        }

        /// <summary>
        /// Content 양 끝에 패딩을 줘서 첫/마지막 카드도 뷰포트 중앙에 스냅될 수 있게 함.
        /// 패딩 = (viewW - cardW) / 2
        /// 카드들의 anchoredPosition.x를 padding만큼 오른쪽으로 이동하고
        /// Content sizeDelta.x를 padding * 2 만큼 늘림.
        /// </summary>
        private void ApplyScrollPadding()
        {
            if (scrollContent == null || _cards.Count == 0) return;

            float cardStride = GetCardStride();
            float cardW      = cardStride - 16f;
            float viewW      = GetViewportWidth();
            float padding    = Mathf.Max(0f, (viewW - cardW) * 0.5f);

            // Content 너비 = 기존 카드 영역 + 양쪽 패딩
            float cardAreaW = _cards.Count * cardStride + 16f; // CARD_GAP + count*(W+GAP)
            scrollContent.sizeDelta = new Vector2(cardAreaW + padding * 2f, scrollContent.sizeDelta.y);

            // 카드 x 위치를 padding만큼 오른쪽으로 이동
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] == null) continue;
                var rt = _cards[i].GetComponent<RectTransform>();
                if (rt == null) continue;
                float origX = 16f + i * cardStride; // 원래 x
                rt.anchoredPosition = new Vector2(padding + origX, rt.anchoredPosition.y);
            }
        }

        /// <summary>
        /// 클리어 안 한 스테이지 중 가장 앞 인덱스를 반환.
        /// 전부 클리어했으면 마지막 스테이지.
        /// </summary>
        private int GetCurrentPlayableIndex()
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (!SaveData.IsCleared(i))
                    return i;
            }
            // 전부 클리어 → 마지막 스테이지
            return Mathf.Max(0, stages.Count - 1);
        }

        private void LoadStagesFromResources()
        {
            // StageRegistry 우선 로드
            var registry = Resources.Load<StageRegistry>("StageRegistry");
            if (registry != null)
            {
                stages = registry.ValidStages();
                return;
            }

            // 폴백: Resources/Stages/*.asset 직접 로드
            stages = new List<StageData>();
            var loaded = Resources.LoadAll<StageData>("Stages");
            var sorted = new List<StageData>(loaded);
            sorted.Sort((a, b) => string.Compare(a.stageName, b.stageName, System.StringComparison.Ordinal));
            stages.AddRange(sorted);
        }

        // ── 전체 갱신 ────────────────────────────────────────────────

        private void RefreshAll()
        {
            if (goldText != null) goldText.text = "0";
            if (gemText  != null) gemText.text  = "0";
        }

        // ── 카드 선택 & 스냅 ────────────────────────────────────────

        public void SelectCard(int idx, bool animate = true)
        {
            if (stages.Count == 0) return;
            idx = Mathf.Clamp(idx, 0, stages.Count - 1);
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
                float scale = (i == idx) ? 1.04f : 0.96f;
                _cards[i].transform.localScale = Vector3.one ;

                var ol = _cards[i].GetComponent<Outline>();
                if (ol != null)
                    ol.effectDistance = (i == idx) ? new Vector2(3, -3) : new Vector2(1, -1);
            }
        }

        private void UpdateStageInfo(int idx)
        {
            if (stages.Count == 0) return;
            var sd       = stages[idx];
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

        // ── 드래그 끝 → 스냅 ────────────────────────────────────────

        private void OnEndDrag()
        {
            _isDragging = false;
            // 현재 스크롤 위치에서 가장 가까운 카드 찾아 스냅
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
            if (stageScroll == null || scrollContent == null || stages.Count == 0) return 0;

            float cardStride = GetCardStride();
            float contentW   = GetContentWidth();
            float viewW      = GetViewportWidth();

            if (contentW <= viewW) return 0;

            float scrolledX = stageScroll.horizontalNormalizedPosition * (contentW - viewW);
            float padding   = GetCurrentPadding();
            float cardCenterOffset = padding + 16f + (cardStride - 16f) * 0.5f;
            int nearest = Mathf.RoundToInt((scrolledX + viewW * 0.5f - cardCenterOffset) / cardStride);
            return Mathf.Clamp(nearest, 0, stages.Count - 1);
        }

        // ── 스냅 ────────────────────────────────────────────────────

        // contentW, viewW 를 안정적으로 읽기 (rect.width는 레이아웃 전 0일 수 있음)
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

        // 현재 적용된 패딩 계산 (ApplyScrollPadding과 동일 공식)
        private float GetCurrentPadding()
        {
            float cardStride = GetCardStride();
            float cardW      = cardStride - 16f;
            float viewW      = GetViewportWidth();
            return Mathf.Max(0f, (viewW - cardW) * 0.5f);
        }

        // 카드 idx 의 content 내 중심 x (패딩 포함)
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

            yield return null; // EndDrag 직후 ScrollRect 관성 초기화 대기

            float contentW = GetContentWidth();
            float viewW    = GetViewportWidth();

            if (contentW <= viewW) yield break;

            float cardCenterX = GetCardCenterX(idx);
            float scrolledX   = cardCenterX - viewW * 0.5f;
            float targetT     = Mathf.Clamp01(scrolledX / (contentW - viewW));

            // 관성 끄고 Lerp 스냅
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

        // stride = 카드 너비 + 간격 (CARD_W + CARD_GAP = 300 + 16 = 316)
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
            Debug.Log("[LobbyUI] 상점 (미구현)");
        }

        public void OnCollectionClick()
        {
            Debug.Log("[LobbyUI] 컬렉션 (미구현)");
        }
    }
}
