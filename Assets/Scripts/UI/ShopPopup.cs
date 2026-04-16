using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 상점 팝업.
    /// 열기: PopupManager.Instance.Open<ShopPopup>("PF_ShopPopup");
    /// 프리팹 경로: Assets/Resources/Popups/PF_ShopPopup.prefab
    /// </summary>
    public class ShopPopup : Popup
    {
        [Header("UI 레퍼런스")]
        public TextMeshProUGUI titleText;
        public Button closeButton;

        // 탭 버튼들 (선택 사항)
        [Header("탭 (선택)")]
        public Button tabTurretBtn;
        public Button tabBuffBtn;
        public GameObject tabTurretPanel;
        public GameObject tabBuffPanel;

        protected override void Awake()
        {
            base.Awake();
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (tabTurretBtn != null)
                tabTurretBtn.onClick.AddListener(() => SwitchTab(0));
            if (tabBuffBtn != null)
                tabBuffBtn.onClick.AddListener(() => SwitchTab(1));
        }

        public override void OnOpen()
        {
            if (titleText != null)
                titleText.text = "상점";

            // 기본 탭: 터렛
            SwitchTab(0);
            Debug.Log("[ShopPopup] 상점 열림");
        }

        public override void OnBringToFront()
        {
            Debug.Log("[ShopPopup] 상점 최상단으로");
        }

        public override void OnClose()
        {
            Debug.Log("[ShopPopup] 상점 닫힘");
        }

        private void SwitchTab(int tab)
        {
            if (tabTurretPanel != null) tabTurretPanel.SetActive(tab == 0);
            if (tabBuffPanel   != null) tabBuffPanel.SetActive(tab == 1);
        }
    }
}
