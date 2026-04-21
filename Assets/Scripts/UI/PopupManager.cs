using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Underdark
{
    /// <summary>
    /// 팝업 스택 관리자.
    ///
    /// 사용법:
    ///   PopupManager.Instance.Open&lt;ShopPopup&gt;("PF_ShopPopup");
    ///   PopupManager.Instance.CloseTop();
    ///   PopupManager.Instance.CloseAll();
    ///
    /// 팝업 프리팹 경로: Assets/Resources/Popups/PF_XXXPopup.prefab
    /// 팝업 프리팹 구조: Canvas 없는 순수 Panel (RectTransform 루트)
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [Tooltip("팝업 기본 sortingOrder 시작값")]
        public int baseSortingOrder = 200;

        [Tooltip("팝업 사이 sortingOrder 간격")]
        public int orderStep = 10;

        // 팝업 전용 Canvas (DontDestroyOnLoad, 씬 Canvas와 분리)
        private Canvas _popupCanvas;
        private Transform _popupRoot;

        private readonly List<Popup> _stack = new List<Popup>();
        private readonly Dictionary<string, Popup> _cache = new Dictionary<string, Popup>();

        // ── 초기화 ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreatePopupCanvas();
        }

        private void CreatePopupCanvas()
        {
            var existing = GameObject.Find("PopupCanvas");
            if (existing != null)
            {
                _popupCanvas = existing.GetComponent<Canvas>();
                _popupRoot   = existing.transform;
                return;
            }

            var go = new GameObject("PopupCanvas");
            DontDestroyOnLoad(go);

            _popupCanvas = go.AddComponent<Canvas>();
            _popupCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _popupCanvas.sortingOrder = baseSortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            _popupRoot = go.transform;
        }

        // ── 공개 API ──────────────────────────────────────────────────

        public T Open<T>(string prefabId) where T : Popup
        {
            // 스택에 이미 있으면 최상단으로
            var existing = FindInStack(prefabId);
            if (existing != null)
            {
                BringToFront(existing);
                return existing as T;
            }

            // 캐시에 없으면 새로 생성
            if (!_cache.TryGetValue(prefabId, out var popup) || popup == null)
            {
                var prefab = Resources.Load<GameObject>($"Popups/{prefabId}");
                if (prefab == null)
                {
                    Debug.LogError($"[PopupManager] 프리팹 없음: Resources/Popups/{prefabId}");
                    return null;
                }
                var go = Instantiate(prefab, _popupRoot);
                popup = go.GetComponent<Popup>();
                if (popup == null)
                {
                    Debug.LogError($"[PopupManager] {prefabId}에 Popup 컴포넌트 없음");
                    Destroy(go);
                    return null;
                }
                // 처음 생성 시에만 전체화면 stretch 설정
                var rt = popup.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin        = Vector2.zero;
                    rt.anchorMax        = Vector2.one;
                    rt.sizeDelta        = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
                _cache[prefabId] = popup;
            }
            // 캐시에 있으면 그냥 재활성화 (transform 절대 건드리지 않음)

            popup.popupId = prefabId;
            _stack.Add(popup);
            popup.gameObject.SetActive(true);
            popup.SetVisible(true);
            ApplySortingOrder(popup, _stack.Count - 1);
            popup.OnOpen();

            return popup as T;
        }

        public void CloseTop()
        {
            if (_stack.Count == 0) return;
            Close(_stack[_stack.Count - 1]);
        }

        public void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
                CloseInternal(_stack[i]);
            _stack.Clear();
        }

        public void Close(Popup popup)
        {
            if (!_stack.Contains(popup)) return;
            _stack.Remove(popup);
            CloseInternal(popup);
            RebuildSortingOrders();
        }

        // ── 내부 ──────────────────────────────────────────────────────

        private Popup FindInStack(string prefabId)
        {
            foreach (var p in _stack)
                if (p != null && p.popupId == prefabId)
                    return p;
            return null;
        }

        private void BringToFront(Popup popup)
        {
            _stack.Remove(popup);
            _stack.Add(popup);
            popup.transform.SetAsLastSibling();
            RebuildSortingOrders();
            popup.OnBringToFront();
        }

        private void CloseInternal(Popup popup)
        {
            if (popup == null) return;
            popup.OnClose();
            popup.SetVisible(false);
            popup.gameObject.SetActive(false);
            // SetParent(null) 금지 - 부모 분리 시 RectTransform 좌표계가 바뀌어
            // 재열기 때 크기/위치가 누적 변형됨. 그냥 PopupCanvas 안에 비활성으로 둠.
        }

        private void RebuildSortingOrders()
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i] == null) continue;
                ApplySortingOrder(_stack[i], i);
                _stack[i].transform.SetSiblingIndex(i);
            }
        }

        private void ApplySortingOrder(Popup popup, int stackIndex)
        {
            // 팝업 프리팹에 Canvas가 붙어있으면 제거 (중첩 Canvas 문제 방지)
            var popupCanvas = popup.GetComponent<Canvas>();
            if (popupCanvas != null)
                Destroy(popupCanvas);

            popup.transform.SetSiblingIndex(stackIndex);
        }
    }
}
