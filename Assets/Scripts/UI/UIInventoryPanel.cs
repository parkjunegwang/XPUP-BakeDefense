using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 하단 인벤토리 패널 (탭-투-플레이스 방식).
    /// - 슬롯 탭 → 터렛 선택 (선택 중 테두리 표시)
    /// - 취소 버튼 → 선택 해제
    /// </summary>
    public class UIInventoryPanel : MonoBehaviour
    {
        public static UIInventoryPanel Instance { get; private set; }

        private GameObject _container;
        private Dictionary<TurretType, GameObject> _buttons = new Dictionary<TurretType, GameObject>();
        private TurretType _selectedType = TurretType.None;

        // 취소 버튼
        private GameObject _cancelBtn;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Init(GameObject container)
        {
            _container = container;
            InventoryManager.Instance.OnInventoryChanged += Refresh;
            BuildCancelButton();
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }

        // ── 취소 버튼 생성 ───────────────────────────────────────────
        private void BuildCancelButton()
        {
            if (_container == null) return;

            // _container의 부모(BotPanel) 안에 만들어야 함
            var parent = _container.transform.parent;
            if (parent == null) parent = _container.transform;

            _cancelBtn = new GameObject("CancelBtn");
            _cancelBtn.transform.SetParent(parent, false);

            var rt = _cancelBtn.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-6f, 0f);
            rt.sizeDelta        = new Vector2(64f, 64f);

            var img = _cancelBtn.AddComponent<Image>();
            img.color = new Color(0.7f, 0.15f, 0.15f, 0.9f);

            var btn = _cancelBtn.AddComponent<Button>();
            btn.onClick.AddListener(() => InputController.Instance?.CancelSelection());

            // X 텍스트
            var txtGo = new GameObject("Txt");
            txtGo.transform.SetParent(_cancelBtn.transform, false);
            var trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "✕";
            tmp.fontSize  = 28f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            _cancelBtn.SetActive(false); // 선택 중일 때만 표시
        }

        // ── 선택된 슬롯 하이라이트 ──────────────────────────────────
        public void SetSelectedSlot(TurretType type)
        {
            _selectedType = type;
            bool selecting = type != TurretType.None;

            // 취소 버튼 표시/숨김
            if (_cancelBtn != null) _cancelBtn.SetActive(selecting);

            // 슬롯 테두리 갱신
            foreach (var kv in _buttons)
            {
                var outline = kv.Value.GetComponent<Outline>();
                if (outline == null) continue;
                outline.effectColor    = kv.Key == type ? Color.white : Color.clear;
                outline.effectDistance = kv.Key == type ? new Vector2(3f, -3f) : Vector2.zero;

                // 선택 중인 슬롯 밝게
                var img = kv.Value.GetComponent<Image>();
                if (img != null)
                {
                    var def = TurretManager.Instance?.GetDef(kv.Key);
                    Color baseColor = def != null ? def.color : Color.gray;
                    img.color = kv.Key == type
                        ? Color.Lerp(baseColor, Color.white, 0.35f)
                        : baseColor;
                }
            }
        }

        public void Refresh()
        {
            if (_container == null) return;

            var stock = InventoryManager.Instance.GetAll();
            var tm    = TurretManager.Instance;

            foreach (Transform child in _container.transform)
                Destroy(child.gameObject);
            _buttons.Clear();

            int slot = 0;
            foreach (var kv in stock)
            {
                if (kv.Value <= 0) continue;
                var def = tm != null ? tm.GetDef(kv.Key) : new TurretDef { type = kv.Key, label = kv.Key.ToString(), color = Color.gray };
                CreateSlot(kv.Key, kv.Value, def, slot);
                slot++;
            }

            // 취소 버튼 위치 재배치
            if (_cancelBtn != null) _cancelBtn.transform.SetAsLastSibling();
        }

        private void CreateSlot(TurretType type, int count, TurretDef def, int slotIndex)
        {
            var go = new GameObject($"Slot_{type}");
            go.transform.SetParent(_container.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 0);
            rt.anchorMax        = new Vector2(0, 1);
            rt.pivot            = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(slotIndex * 80f + 8f, 0f);
            rt.sizeDelta        = new Vector2(72f, -8f);

            var img = go.AddComponent<Image>();
            img.color = def.color;

            // 선택 테두리
            var outline = go.AddComponent<Outline>();
            outline.effectColor    = Color.clear;
            outline.effectDistance = Vector2.zero;

            var btn = go.AddComponent<Button>();

            // 타워 이름
            var lbl = new GameObject("Lbl");
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = new Vector2(1, 0.6f);
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var lTmp = lbl.AddComponent<TextMeshProUGUI>();
            var def2 = TurretManager.Instance?.GetDef(type);
            string sz = def2 != null && (def2.sizeX > 1 || def2.sizeY > 1) ? $"{def2.sizeX}x{def2.sizeY}" : "1x1";
            lTmp.text      = $"{def.label}\n{sz}";
            lTmp.fontSize  = 11;
            lTmp.color     = Color.white;
            lTmp.alignment = TextAlignmentOptions.Center;

            // 수량 뱃지
            var badge = new GameObject("Badge");
            badge.transform.SetParent(go.transform, false);
            var brt = badge.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.55f, 0.55f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            badge.AddComponent<Image>().color = new Color(0.9f, 0.15f, 0.15f, 0.95f);
            var bLabel = new GameObject("Num");
            bLabel.transform.SetParent(badge.transform, false);
            var blrt = bLabel.AddComponent<RectTransform>();
            blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one;
            blrt.offsetMin = blrt.offsetMax = Vector2.zero;
            var bTmp = bLabel.AddComponent<TextMeshProUGUI>();
            bTmp.text      = count.ToString();
            bTmp.fontSize  = 14;
            bTmp.color     = Color.white;
            bTmp.fontStyle = FontStyles.Bold;
            bTmp.alignment = TextAlignmentOptions.Center;

            // 탭 이벤트 - PointerDown이 아닌 Button.onClick 사용 (터치 호환)
            var capType = type;
            var capCol  = def.color;
            btn.onClick.AddListener(() =>
            {
                InputController.Instance?.SelectTurretFromUI(capType, capCol);
            });

            _buttons[type] = go;
        }
    }
}
