using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 하단 인벤토리 패널.
    /// PointerDown → 드래그 시작 / 취소버튼 → CancelDrag
    /// </summary>
    public class UIInventoryPanel : MonoBehaviour
    {
        public static UIInventoryPanel Instance { get; private set; }

        private GameObject _container;
        private Dictionary<TurretType, GameObject> _buttons = new Dictionary<TurretType, GameObject>();
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

        // ── 취소 버튼 ────────────────────────────────────────────────
        private void BuildCancelButton()
        {
            if (_container == null) return;

            var parent = _container.transform.parent ?? _container.transform;

            _cancelBtn = new GameObject("CancelBtn");
            _cancelBtn.transform.SetParent(parent, false);

            var rt = _cancelBtn.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0.5f);
            rt.anchorMax        = new Vector2(1f, 0.5f);
            rt.pivot            = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-6f, 0f);
            rt.sizeDelta        = new Vector2(64f, 64f);

            _cancelBtn.AddComponent<Image>().color = new Color(0.75f, 0.12f, 0.12f, 0.92f);

            var btn = _cancelBtn.AddComponent<Button>();
            btn.onClick.AddListener(() => InputController.Instance?.CancelDrag());

            var txtGo = new GameObject("Txt");
            txtGo.transform.SetParent(_cancelBtn.transform, false);
            var trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "✕";
            tmp.fontSize  = 30f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            // 항상 표시 (드래그 중이 아니어도)
            _cancelBtn.SetActive(true);
        }

        // ── 슬롯 갱신 ────────────────────────────────────────────────
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
                var def = tm != null
                    ? tm.GetDef(kv.Key)
                    : new TurretDef { type = kv.Key, label = kv.Key.ToString(), color = Color.gray };
                CreateSlot(kv.Key, kv.Value, def, slot);
                slot++;
            }

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

            go.AddComponent<Image>().color = def.color;

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
            brt.anchorMax = Vector2.one;
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

            // PointerDown → 드래그 시작 (손가락 대자마자 시작)
            var trig    = go.AddComponent<EventTrigger>();
            var entry   = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            var capType = type;
            var capCol  = def.color;
            entry.callback.AddListener((_) =>
            {
                InputController.Instance?.StartDragFromUI(capType, capCol);
            });
            trig.triggers.Add(entry);

            _buttons[type] = go;
        }
    }
}
