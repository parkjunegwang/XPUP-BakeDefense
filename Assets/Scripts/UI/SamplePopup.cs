using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 팝업 사용 예시 — 실제 팝업 작성 시 이 패턴을 복사해서 사용.
    ///
    /// 1) 프리팹 생성:
    ///    - GameObject 생성 → Canvas 추가 (Override Sorting 체크)
    ///    - 이 스크립트(또는 자체 스크립트) 붙이기
    ///    - Assets/Resources/Popups/ 폴더에 PF_SamplePopup.prefab 저장
    ///
    /// 2) 열기:
    ///    PopupManager.Instance.Open&lt;SamplePopup&gt;("PF_SamplePopup");
    ///
    /// 3) 닫기 (팝업 내부):
    ///    Close();
    /// </summary>
    public class SamplePopup : Popup
    {
        [Header("UI 레퍼런스")]
        public TextMeshProUGUI titleText;
        public Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        public override void OnOpen()
        {
            if (titleText != null)
                titleText.text = "팝업 열림";
            Debug.Log("[SamplePopup] OnOpen");
        }

        public override void OnBringToFront()
        {
            Debug.Log("[SamplePopup] 최상단으로 올라옴");
        }

        public override void OnClose()
        {
            Debug.Log("[SamplePopup] OnClose");
        }
    }
}
