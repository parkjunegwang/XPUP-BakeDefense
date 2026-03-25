using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Underdark
{
    /// <summary>
    /// 하단 인벤토리 패널.
    /// 보유한 타워 종류별로 버튼 + 남은 수량 표시.
    /// 수량이 0이면 버튼 숨김.
    /// </summary>
    public class UIInventoryPanel : MonoBehaviour
    {
        public static UIInventoryPanel Instance { get; private set; }

        private GameObject _container;
        private Dictionary<TurretType, GameObject> _buttons = new Dictionary<TurretType, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Init(GameObject container)
        {
            _container = container;
            InventoryManager.Instance.OnInventoryChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }

        public void Refresh()
        {
            if (_container == null) return;

            var stock = InventoryManager.Instance.GetAll();
            var tm    = TurretManager.Instance;

            // 기존 버튼 전부 제거하고 재빌드 (수량 변할 때마다)
            foreach (Transform child in _container.transform)
                Destroy(child.gameObject);
            _buttons.Clear();

            // 보유량 > 0인 타워만 표시
            int slot = 0;
            foreach (var kv in stock)
            {
                if (kv.Value <= 0) continue;
                var def = tm != null ? tm.GetDef(kv.Key) : new TurretDef { type=kv.Key, label=kv.Key.ToString(), color=Color.gray };
                CreateSlot(kv.Key, kv.Value, def, slot);
                slot++;
            }
        }

        private void CreateSlot(TurretType type, int count, TurretDef def, int slotIndex)
        {
            var go = new GameObject($"Slot_{type}");
            go.transform.SetParent(_container.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(slotIndex * 80f + 8f, 0f);
            rt.sizeDelta = new Vector2(72f, -8f);

            go.AddComponent<Image>().color = def.color;
            go.AddComponent<Button>();

            // 타워 이름
            var lbl = new GameObject("Lbl");
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = new Vector2(1, 0.6f);
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var lTmp = lbl.AddComponent<TextMeshProUGUI>();

            var def2 = TurretManager.Instance?.GetDef(type);
            string sz = def2 != null && def2.sizeX > 1 ? $"{def2.sizeX}x1" : "1x1";
            lTmp.text = $"{def.label}\n{sz}";
            lTmp.fontSize = 11; lTmp.color = Color.white;
            lTmp.alignment = TextAlignmentOptions.Center;

            // 수량 뱃지 (우상단)
            var badge = new GameObject("Badge");
            badge.transform.SetParent(go.transform, false);
            var brt = badge.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.55f, 0.55f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            badge.AddComponent<Image>().color = new Color(0.9f, 0.15f, 0.15f, 0.95f);
            var bLabel = new GameObject("Num");
            bLabel.transform.SetParent(badge.transform, false);
            var blrt = bLabel.AddComponent<RectTransform>();
            blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one;
            blrt.offsetMin = Vector2.zero; blrt.offsetMax = Vector2.zero;
            var bTmp = bLabel.AddComponent<TextMeshProUGUI>();
            bTmp.text = count.ToString();
            bTmp.fontSize = 14; bTmp.color = Color.white;
            bTmp.fontStyle = FontStyles.Bold;
            bTmp.alignment = TextAlignmentOptions.Center;

            // 드래그 이벤트
            var trig = go.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            var capType = type; var capCol = def.color;
            entry.callback.AddListener((_) =>
            {
                if (InventoryManager.Instance.CanPlace(capType))
                    InputController.Instance?.StartDragFromUI(capType, capCol);
            });
            trig.triggers.Add(entry);

            _buttons[type] = go;
        }
    }
}
