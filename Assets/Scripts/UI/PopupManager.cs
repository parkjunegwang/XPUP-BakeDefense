using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 팝업 스택 관리자.
    ///
    /// 사용법:
    ///   // 열기 (프리팹 경로: Assets/Prefabs/UI/Popups/PF_XXXPopup.prefab)
    ///   PopupManager.Instance.Open&lt;SettingsPopup&gt;("PF_SettingsPopup");
    ///
    ///   // 닫기 (팝업 내부에서)
    ///   Close();   // Popup.Close() 상속
    ///
    ///   // 닫기 (외부에서)
    ///   PopupManager.Instance.CloseTop();
    ///   PopupManager.Instance.CloseAll();
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [Tooltip("팝업을 올릴 Canvas Transform. null이면 자동 탐색.")]
        public Transform popupRoot;

        [Tooltip("팝업 기본 sortingOrder 시작값")]
        public int baseSortingOrder = 100;

        [Tooltip("팝업 사이 sortingOrder 간격")]
        public int orderStep = 10;

        // 현재 열려 있는 팝업 스택 (인덱스 0 = 가장 먼저 열린 것, Last = 최상단)
        private readonly List<Popup> _stack = new List<Popup>();

        // prefabId → 이미 생성된 팝업 인스턴스 캐시
        private readonly Dictionary<string, Popup> _cache = new Dictionary<string, Popup>();

        // ── 초기화 ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (popupRoot == null)
                popupRoot = FindOrCreateRoot();
        }

        private Transform FindOrCreateRoot()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null) return canvas.transform;

            var go = new GameObject("PopupCanvas");
            var cv = go.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 50;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            DontDestroyOnLoad(go);
            return go.transform;
        }

        // ── 공개 API ──────────────────────────────────────────────────

        /// <summary>
        /// 팝업 열기.
        /// - 캐시에 인스턴스가 없으면 Resources.Load로 프리팹을 생성해 캐시.
        /// - 스택에 이미 있으면 새로 생성하지 않고 최상단으로 끌어올림 (3번 요구사항).
        /// </summary>
        public T Open<T>(string prefabId) where T : Popup
        {
            // 스택에 이미 있으면 끌어올리기
            var existing = FindInStack(prefabId);
            if (existing != null)
            {
                BringToFront(existing);
                return existing as T;
            }

            // 캐시에 없으면 생성
            if (!_cache.TryGetValue(prefabId, out var popup) || popup == null)
            {
                var prefab = Resources.Load<GameObject>($"Popups/{prefabId}");
                if (prefab == null)
                {
                    Debug.LogError($"[PopupManager] 프리팹을 찾을 수 없음: Resources/Popups/{prefabId}");
                    return null;
                }
                var go = Instantiate(prefab, popupRoot);
                popup  = go.GetComponent<Popup>();
                if (popup == null)
                {
                    Debug.LogError($"[PopupManager] {prefabId}에 Popup 컴포넌트가 없음");
                    Destroy(go);
                    return null;
                }
                _cache[prefabId] = popup;
            }
            else
            {
                // 캐시에 있지만 스택에 없는 경우 (닫혔다가 다시 열기)
                popup.transform.SetParent(popupRoot, false);
            }

            popup.popupId = prefabId;
            _stack.Add(popup);
            popup.gameObject.SetActive(true);
            popup.SetVisible(true);
            ApplySortingOrder(popup, _stack.Count - 1);
            popup.OnOpen();

            return popup as T;
        }

        /// <summary>최상단 팝업 닫기</summary>
        public void CloseTop()
        {
            if (_stack.Count == 0) return;
            Close(_stack[_stack.Count - 1]);
        }

        /// <summary>모든 팝업 닫기</summary>
        public void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
                CloseInternal(_stack[i]);
            _stack.Clear();
        }

        /// <summary>특정 팝업 닫기 (Popup.Close()에서 호출)</summary>
        public void Close(Popup popup)
        {
            if (!_stack.Contains(popup)) return;
            _stack.Remove(popup);
            CloseInternal(popup);
            RebuildSortingOrders();
        }

        // ── 내부 ──────────────────────────────────────────────────────

        /// <summary>스택에서 prefabId로 팝업 검색</summary>
        private Popup FindInStack(string prefabId)
        {
            foreach (var p in _stack)
                if (p != null && p.popupId == prefabId)
                    return p;
            return null;
        }

        /// <summary>
        /// 스택에 이미 있는 팝업을 최상단으로 끌어올림.
        /// sibling order도 함께 올려서 시각적으로 맨 앞에 표시.
        /// </summary>
        private void BringToFront(Popup popup)
        {
            _stack.Remove(popup);
            _stack.Add(popup);

            popup.transform.SetAsLastSibling();
            RebuildSortingOrders();
            popup.OnBringToFront();
        }

        /// <summary>팝업을 숨기고 부모에서 분리 (Destroy 하지 않고 재사용 가능하게 캐시)</summary>
        private void CloseInternal(Popup popup)
        {
            if (popup == null) return;
            popup.OnClose();
            popup.SetVisible(false);
            popup.gameObject.SetActive(false);
            popup.transform.SetParent(null); // popupRoot 아래에서 제거
        }

        /// <summary>스택 순서대로 sortingOrder 및 sibling 인덱스 재정렬</summary>
        private void RebuildSortingOrders()
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i] == null) continue;
                ApplySortingOrder(_stack[i], i);
                _stack[i].transform.SetSiblingIndex(i);
            }
        }

        /// <summary>Canvas가 있으면 그 sortingOrder, 없으면 RectTransform sibling만 사용</summary>
        private void ApplySortingOrder(Popup popup, int stackIndex)
        {
            int order = baseSortingOrder + stackIndex * orderStep;
            var canvas = popup.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder    = order;
            }
            // sibling 인덱스도 맞춰줌 (Canvas 없을 때의 시각 순서 보장)
            popup.transform.SetSiblingIndex(stackIndex);
        }
    }
}
