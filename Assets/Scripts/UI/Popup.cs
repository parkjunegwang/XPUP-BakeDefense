using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 모든 팝업의 베이스 클래스.
    /// PopupManager.Open<T>() 로 열고, Close() 로 닫는다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class Popup : MonoBehaviour
    {
        // PopupManager가 열릴 때 자동으로 채워줌
        [HideInInspector] public string popupId;

        private CanvasGroup _cg;

        protected virtual void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
        }

        // ── 팝업 생명주기 ──────────────────────────────────────────────

        /// <summary>팝업이 처음 생성되어 열릴 때 호출</summary>
        public virtual void OnOpen() { }

        /// <summary>이미 열려 있는 팝업이 다시 최상단으로 올라올 때 호출</summary>
        public virtual void OnBringToFront() { }

        /// <summary>팝업이 닫힐 때 호출</summary>
        public virtual void OnClose() { }

        // ── 공개 API ──────────────────────────────────────────────────

        public void Close() => PopupManager.Instance?.Close(this);

        /// <summary>팝업 전체를 투명/불투명으로 전환 (레이캐스트 포함)</summary>
        public void SetVisible(bool visible)
        {
            if (_cg == null) _cg = GetComponent<CanvasGroup>();
            _cg.alpha          = visible ? 1f : 0f;
            _cg.interactable   = visible;
            _cg.blocksRaycasts = visible;
        }
    }
}
