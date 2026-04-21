using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Underdark
{
    /// <summary>셀에 붙는 꾹 누르기 핸들러 - ScrollRect 이벤트 간섭 없이 동작</summary>
    internal class CellHoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action onDown;
        public System.Action onUp;

        public void OnPointerDown(PointerEventData e) => onDown?.Invoke();
        public void OnPointerUp(PointerEventData e)   => onUp?.Invoke();
        public void OnPointerExit(PointerEventData e) => onUp?.Invoke();
    }

    /// <summary>
    /// 컬렉션 팝업 - 터렛 도감.
    /// 열기: PopupManager.Instance.Open&lt;CollectionPopup&gt;("PF_CollectionPopup");
    /// </summary>
    public class CollectionPopup : Popup
    {
        [Header("UI 레퍼런스")]
        public TextMeshProUGUI titleText;
        public Button closeButton;
        public Transform gridParent;

        [Header("상세 토글 패널 (꾹 누르면 표시)")]
        public GameObject detailPanel;
        public TextMeshProUGUI detailIcon;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailDesc;
        public TextMeshProUGUI detailStatus;

        // 터렛 타입별 아이콘 (emoji가 전부 같으므로 코드에서 매핑)
        private static readonly Dictionary<TurretType, string> _icons = new Dictionary<TurretType, string>
        {
            { TurretType.RangedTurret,    "🏹" },
            { TurretType.MeleeTurret,     "⚔️" },
            { TurretType.CrossMeleeTurret,"✚" },
            { TurretType.SpikeTrap,       "🗡️" },
            { TurretType.AreaDamage,      "💥" },
            { TurretType.ExplosiveCannon, "💣" },
            { TurretType.SlowShooter,     "🧊" },
            { TurretType.RapidFire,       "⚡" },
            { TurretType.Tornado,         "🌪️" },
            { TurretType.LavaRain,        "🌋" },
            { TurretType.ChainLightning,  "🔗" },
            { TurretType.BlackHole,       "🕳️" },
            { TurretType.PrecisionStrike, "🎯" },
            { TurretType.GambleBat,       "🦇" },
            { TurretType.PulseSlower,     "📡" },
            { TurretType.DragonStatue,    "🐉" },
            { TurretType.HasteTower,      "💨" },
            { TurretType.PinballCannon,   "🎱" },
            { TurretType.BoomerangTurret, "🪃" },
        };

        private TurretRegistry _registry;
        private readonly List<GameObject> _cells = new List<GameObject>();

        // 현재 꾹 눌린 셀 추적
        private Coroutine _holdCoroutine;
        private bool _detailVisible;

        protected override void Awake()
        {
            base.Awake();
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        public override void OnOpen()
        {
            if (titleText != null) titleText.text = "컬렉션";
            HideDetail();
            FixViewport();
            LoadRegistry();
            StartCoroutine(BuildGridAfterLayout());
        }

        private IEnumerator BuildGridAfterLayout()
        {
            // 1프레임 대기 후 레이아웃이 완료되면 cellSize 계산
            yield return null;
            AdjustCellSize();
            BuildGrid();
        }

        // Viewport Image가 alpha=0이면 Mask가 자식을 클리핑 못함 → 강제로 white로
        private void FixViewport()
        {
            if (gridParent == null) return;
            // gridParent = Content, 부모 = Viewport
            var viewport = gridParent.parent;
            if (viewport == null) return;
            var vpImg = viewport.GetComponent<UnityEngine.UI.Image>();
            if (vpImg != null && vpImg.color.a < 0.5f)
            {
                vpImg.color = Color.white;
                var mask = viewport.GetComponent<UnityEngine.UI.Mask>();
                if (mask != null) mask.showMaskGraphic = false;
            }
        }

        // ScrollRect 너비를 기반으로 4열이 꽉 차게 cellSize 계산
        private void AdjustCellSize()
        {
            if (gridParent == null) return;
            var grid = gridParent.GetComponent<GridLayoutGroup>();
            if (grid == null) return;

            // ScrollRect (Viewport의 부모)
            var viewport = gridParent.parent;
            if (viewport == null) return;
            var scrollGo = viewport.parent;
            if (scrollGo == null) return;
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            if (scrollRt == null) return;

            // 너비 계산: ScrollRect sizeDelta or rect (레이아웃 후)
            float width = scrollRt.rect.width;
            if (width <= 0f) width = scrollRt.sizeDelta.x;
            if (width <= 0f) return; // 아직 레이아웃 안됨 → 코루틴에서 재시도

            int cols = grid.constraintCount > 0 ? grid.constraintCount : 4;
            float totalSpacing = grid.spacing.x * (cols - 1);
            float totalPadding = grid.padding.left + grid.padding.right;
            float cellW = Mathf.Floor((width - totalPadding - totalSpacing) / cols);
            float cellH = Mathf.Round(cellW * 1.15f); // 약간 세로로 길게
            grid.cellSize = new Vector2(cellW, cellH);
        }

        public override void OnClose()
        {
            HideDetail();
        }

        // ── 레지스트리 ────────────────────────────────────────────────

        private void LoadRegistry()
        {
            if (_registry != null) return;
            _registry = Resources.Load<TurretRegistry>("TurretRegistry");
        }

        // ── 그리드 빌드 ──────────────────────────────────────────────

        private void BuildGrid()
        {
            if (gridParent == null || _registry == null) return;

            foreach (var c in _cells) if (c) Destroy(c);
            _cells.Clear();

            foreach (var entry in _registry.All)
            {
                if (entry == null || entry.isWall || entry.type == TurretType.None) continue;
                bool owned = SaveData.IsOwned(entry.type);
                var cell = MakeCell(entry, owned);
                cell.transform.SetParent(gridParent, false);
                _cells.Add(cell);
            }
        }

        private GameObject MakeCell(TurretRegistry.TurretEntry entry, bool owned)
        {
            // ── 루트 ─────────────────────────────────────────────────
            var cell = new GameObject(entry.type.ToString());
            cell.AddComponent<RectTransform>();

            // 배경 Image (색상)
            var bg = cell.AddComponent<Image>();
            if (owned)
                bg.color = new Color(entry.color.r * 0.6f, entry.color.g * 0.6f, entry.color.b * 0.6f, 1f);
            else
                bg.color = new Color(0.12f, 0.12f, 0.15f, 1f);

            // ── 아이콘 텍스트 (상단 60%) ──────────────────────────────
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cell.transform, false);
            var iconTmp = iconGo.AddComponent<TextMeshProUGUI>();
            iconTmp.text      = owned ? GetIcon(entry.type) : "?";
            iconTmp.fontSize  = owned ? 42 : 48;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color     = owned ? Color.white : new Color(0.4f, 0.4f, 0.45f);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.38f);
            iconRt.anchorMax = new Vector2(1f, 1f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            // ── 이름 텍스트 (하단 38%) ───────────────────────────────
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(cell.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text                = owned ? entry.label : "???";
            nameTmp.fontSize            = 14;
            nameTmp.enableAutoSizing    = true;
            nameTmp.fontSizeMin         = 10;
            nameTmp.fontSizeMax         = 16;
            nameTmp.alignment           = TextAlignmentOptions.Center;
            nameTmp.color               = owned ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.35f, 0.35f, 0.38f);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.05f, 0f);
            nameRt.anchorMax = new Vector2(0.95f, 0.38f);
            nameRt.offsetMin = new Vector2(0f, 3f);
            nameRt.offsetMax = new Vector2(0f, -2f);

            // ── 꾹 누르기 핸들러 ─────────────────────────────────────
            var capturedEntry = entry;
            var capturedOwned = owned;

            var handler = cell.AddComponent<CellHoldHandler>();
            handler.onDown = () => OnCellHoldStart(capturedEntry, capturedOwned);
            handler.onUp   = () => OnCellHoldEnd();

            return cell;
        }

        // ── 꾹 누르기 로직 ───────────────────────────────────────────

        private void OnCellHoldStart(TurretRegistry.TurretEntry entry, bool owned)
        {
            if (_holdCoroutine != null) StopCoroutine(_holdCoroutine);
            _holdCoroutine = StartCoroutine(HoldRoutine(entry, owned));
        }

        private void OnCellHoldEnd()
        {
            if (_holdCoroutine != null)
            {
                StopCoroutine(_holdCoroutine);
                _holdCoroutine = null;
            }
            // 상세가 떠있으면 손 뗄 때 닫기 (단, ShowDetail 직후 프레임은 무시)
            if (_detailVisible && !_justShown) HideDetail();
        }

        private bool _justShown;

        private IEnumerator HoldRoutine(TurretRegistry.TurretEntry entry, bool owned)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            _justShown = true;
            ShowDetail(entry, owned);
            // 1프레임 대기 후 플래그 해제 → 그 이후 손 떼면 정상 닫힘
            yield return null;
            _justShown = false;
        }

        // ── 상세 패널 ────────────────────────────────────────────────

        private void ShowDetail(TurretRegistry.TurretEntry entry, bool owned)
        {
            if (detailPanel == null) return;
            _detailVisible = true;
            detailPanel.SetActive(true);

            if (detailIcon   != null) detailIcon.text   = owned ? GetIcon(entry.type) : "?";
            if (detailName   != null) detailName.text   = owned ? entry.label : "???";
            if (detailDesc   != null)
            {
                detailDesc.text = owned
                    ? $"Cost: {entry.cost}골드\nSize: {entry.sizeX}×{entry.sizeY}"
                    : "아직 획득하지 못한 터렛입니다.";
            }
            if (detailStatus != null)
            {
                detailStatus.text  = owned ? "✓ 보유 중" : "미보유";
                detailStatus.color = owned
                    ? new Color(0.3f, 0.9f, 0.4f)
                    : new Color(0.55f, 0.55f, 0.55f);
            }
        }

        private void HideDetail()
        {
            _detailVisible = false;
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        // ── 아이콘 조회 ──────────────────────────────────────────────

        private static string GetIcon(TurretType type)
            => _icons.TryGetValue(type, out var icon) ? icon : "🔫";
    }
}
