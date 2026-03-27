using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Underdark
{
    /// <summary>
    /// 모바일 탭-투-플레이스 입력 컨트롤러.
    ///
    /// 흐름:
    ///   1. 인벤토리 슬롯 탭  → 터렛 선택 (SelectTurretFromUI)
    ///   2. 선택 중 타일 탭   → 해당 타일에 배치 (또는 취소 버튼 탭)
    ///   3. 배치된 터렛 탭    → 사정거리 표시 / 이동 선택
    ///   4. 이동 선택 후 타일 탭 → 이동 or 합치기
    /// </summary>
    public class InputController : MonoBehaviour
    {
        public static InputController Instance { get; private set; }

        // ── 선택 상태 ─────────────────────────────────────────────────
        private enum Mode { Idle, PlacingFromUI, MovingTurret }
        private Mode _mode = Mode.Idle;

        private TurretType _selectedType;
        private Color      _selectedColor;
        private TurretBase _selectedTurret;  // 이동 대상

        // ── 고스트 프리뷰 ─────────────────────────────────────────────
        private GameObject     _ghost;
        private SpriteRenderer _ghostSr;

        private List<Tile> _lastHighlighted = new List<Tile>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 고스트 오브젝트
            _ghost   = new GameObject("PlaceGhost");
            _ghostSr = _ghost.AddComponent<SpriteRenderer>();
            _ghostSr.sprite       = GameSetup.WhiteSquareStatic();
            _ghostSr.color        = new Color(1f, 1f, 1f, 0.4f);
            _ghostSr.sortingOrder = SLayer.Ghost;
            _ghost.SetActive(false);

            // 사정거리 표시기
            var riGo = new GameObject("RangeIndicator");
            riGo.AddComponent<RangeIndicator>();
        }

        private void Update()
        {
            // Mouse(에디터/PC) 또는 Touchscreen 공통 처리
            bool pressed  = false;
            bool released = false;
            Vector2 screenPos = Vector2.zero;

            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                pressed   = mouse.leftButton.wasPressedThisFrame;
                released  = mouse.leftButton.wasReleasedThisFrame;
            }
            else if (touch != null && touch.touches.Count > 0)
            {
                var t0 = touch.touches[0];
                screenPos = t0.position.ReadValue();
                pressed   = t0.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began;
                released  = t0.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended;
            }

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            worldPos.z = 0f;

            Tile hoverTile = RaycastTile(worldPos);

            // 고스트 & 하이라이트 갱신 (매 프레임)
            UpdateGhostAndHighlight(hoverTile);

            // 탭(released) 처리
            if (released)
                HandleTap(worldPos, hoverTile);
        }

        // ── 인벤토리 슬롯에서 호출 ───────────────────────────────────
        public void SelectTurretFromUI(TurretType type, Color col)
        {
            if (GameManager.Instance.CurrentState != GameState.Preparation) return;
            if (!InventoryManager.Instance.CanPlace(type))
            {
                UIManager.Instance?.ShowMessage("No stock!");
                return;
            }

            // 이미 같은 타입 선택 중이면 취소
            if (_mode == Mode.PlacingFromUI && _selectedType == type)
            {
                CancelSelection();
                return;
            }

            _mode          = Mode.PlacingFromUI;
            _selectedType  = type;
            _selectedColor = col;
            _selectedTurret = null;

            SetupGhost(type, null, col);
            UIInventoryPanel.Instance?.SetSelectedSlot(type);
            UIManager.Instance?.ShowMessage($"{TurretManager.Instance.GetDef(type).label} 배치할 타일을 선택하세요");
        }

        // 외부에서 취소 버튼 눌렀을 때 호출
        public void CancelSelection()
        {
            _mode = Mode.Idle;
            _selectedTurret = null;
            _ghost.SetActive(false);
            RangeIndicator.Instance?.Hide();
            ClearHighlight();
            UIInventoryPanel.Instance?.SetSelectedSlot(TurretType.None);
        }

        // ── 탭 처리 ──────────────────────────────────────────────────
        private void HandleTap(Vector3 worldPos, Tile tile)
        {
            // UI 위를 탭했으면 무시 (인벤토리 버튼 등)
            if (IsPointerOverUI(worldPos)) return;

            switch (_mode)
            {
                case Mode.Idle:
                    HandleIdleTap(tile);
                    break;

                case Mode.PlacingFromUI:
                    HandlePlaceTap(tile);
                    break;

                case Mode.MovingTurret:
                    HandleMoveTap(tile);
                    break;
            }
        }

        private void HandleIdleTap(Tile tile)
        {
            if (tile == null) return;

            if (tile.HasTurret())
            {
                var turret = tile.placedTurret;
                if (turret == null) return;

                ShowTurretRange(turret);

                // 준비단계에서만 이동 가능
                if (GameManager.Instance.CurrentState == GameState.Preparation)
                {
                    if (turret.isFromMapData)
                    {
                        UIManager.Instance?.ShowMessage("Map walls cannot be moved!");
                        return;
                    }
                    // 터렛 선택 → 이동 모드
                    _mode           = Mode.MovingTurret;
                    _selectedTurret = turret;
                    SetupGhost(turret.turretType, turret, turret.bodyRenderer?.color ?? Color.white);
                    UIManager.Instance?.ShowMessage("이동할 타일을 선택하세요 (같은 타일: 취소)");
                }
            }
        }

        private void HandlePlaceTap(Tile tile)
        {
            if (tile == null)
            {
                // 빈 곳 탭 → 취소
                CancelSelection();
                return;
            }

            TurretManager.Instance.PlaceSelectedTurret(tile, _selectedType);
            // 배치 성공 여부와 무관하게 선택 유지 (연속 배치 가능)
            // 재고 소진되면 자동 취소
            if (!InventoryManager.Instance.CanPlace(_selectedType))
                CancelSelection();
        }

        private void HandleMoveTap(Tile tile)
        {
            if (tile == null || _selectedTurret == null)
            {
                CancelSelection();
                return;
            }

            // 같은 타일 탭 → 취소
            if (_selectedTurret.occupiedTiles.Contains(tile))
            {
                CancelSelection();
                return;
            }

            // 다른 터렛 탭 → 합치기 시도
            if (tile.HasTurret() && tile.placedTurret != _selectedTurret)
            {
                TurretManager.Instance.TryMerge(_selectedTurret.currentTile, tile);
            }
            else
            {
                TurretManager.Instance.MoveTurret(_selectedTurret.currentTile, tile);
            }

            CancelSelection();
        }

        // ── 고스트 & 하이라이트 ──────────────────────────────────────
        private void UpdateGhostAndHighlight(Tile hover)
        {
            ClearHighlight();

            if (_mode == Mode.Idle)
            {
                _ghost.SetActive(false);
                return;
            }

            _ghost.SetActive(true);

            TurretType   type = _mode == Mode.PlacingFromUI ? _selectedType : _selectedTurret?.turretType ?? TurretType.None;
            TurretStatData sd = _selectedTurret != null ? _selectedTurret.statData : TurretManager.Instance.GetStatData(type);

            if (hover == null)
            {
                _ghost.SetActive(false);
                return;
            }

            var tiles = TurretManager.Instance.GetShapeTiles(hover, type, sd);
            bool canPlace = _mode == Mode.PlacingFromUI
                ? TurretManager.Instance.CanPlaceAt(hover, type)
                : (_selectedTurret != null && TurretManager.Instance.CanMoveAt(_selectedTurret, hover));

            Color hlCol = canPlace ? Tile.ColorHighlight : Tile.ColorError;

            Vector3 center = hover.transform.position;
            if (tiles != null)
            {
                foreach (var t in tiles) { t.SetHighlight(hlCol); _lastHighlighted.Add(t); }
                center = Vector3.zero;
                foreach (var t in tiles) center += t.transform.position;
                center /= tiles.Count;
            }
            else
            {
                hover.SetHighlight(Tile.ColorError); _lastHighlighted.Add(hover);
            }

            _ghost.transform.position = new Vector3(center.x, center.y, 0f);
            UpdateDragRangeIndicator(center, type, sd, canPlace);
        }

        private void SetupGhost(TurretType type, TurretBase turret, Color col)
        {
            var def  = TurretManager.Instance.GetDef(type);
            var sd   = turret != null ? turret.statData : TurretManager.Instance.GetStatData(type);
            float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            int bW = def.sizeX, bH = def.sizeY;
            if (sd != null && sd.HasCustomShape() && sd.tileShape != null && sd.tileShape.Length > 0)
            {
                bW = 1; bH = 1;
                foreach (var o in sd.tileShape) { bW = Mathf.Max(bW, o.x + 1); bH = Mathf.Max(bH, o.y + 1); }
            }
            _ghost.transform.localScale = new Vector3(bW * step * 0.88f, bH * step * 0.88f, 1f);
            _ghostSr.color = new Color(col.r, col.g, col.b, 0.45f);
            _ghost.SetActive(true);
        }

        private void ClearHighlight()
        {
            foreach (var t in _lastHighlighted) t.RefreshColor();
            _lastHighlighted.Clear();
        }

        // ── 사정거리 표시 ────────────────────────────────────────────
        private void UpdateDragRangeIndicator(Vector3 center, TurretType type, TurretStatData sd, bool canPlace)
        {
            if (RangeIndicator.Instance == null) return;
            Color rc = canPlace ? new Color(0.4f, 1f, 0.5f, 0.7f) : new Color(1f, 0.3f, 0.3f, 0.7f);

            switch (type)
            {
                case TurretType.SpikeTrap:
                case TurretType.ElectricGate:
                    if (_lastHighlighted.Count > 0)
                        RangeIndicator.Instance.ShowTiles(_lastHighlighted, new Color(rc.r, rc.g, rc.b, 0.3f));
                    else RangeIndicator.Instance.Hide();
                    return;

                case TurretType.MeleeTurret:
                    float step2 = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
                    var dir2 = _selectedTurret != null ? (_selectedTurret as MeleeTurret)?.FacingDir ?? new Vector2Int(0, -1) : new Vector2Int(0, -1);
                    float len2 = GetDragRange(sd); if (len2 <= 0f) len2 = step2 * 1.2f;
                    RangeIndicator.Instance.ShowDirection(center, dir2, len2, new Color(rc.r, rc.g, rc.b, 0.75f), step2 * 0.85f);
                    return;

                case TurretType.Wall:
                case TurretType.Wall2x1:
                case TurretType.Wall1x2:
                case TurretType.Wall2x2:
                    RangeIndicator.Instance.Hide();
                    return;

                default:
                    float range = GetDragRange(sd);
                    if (range > 0f) RangeIndicator.Instance.ShowCircle(center, range, rc);
                    else RangeIndicator.Instance.Hide();
                    return;
            }
        }

        private void ShowTurretRange(TurretBase turret)
        {
            if (turret == null || RangeIndicator.Instance == null) return;
            var def = TurretManager.Instance.GetDef(turret.turretType);
            Color col = def.color; col.a = 0.55f;

            switch (turret.turretType)
            {
                case TurretType.SpikeTrap:
                case TurretType.ElectricGate:
                    var tiles = TurretManager.Instance.GetShapeTiles(turret.currentTile, turret.turretType, turret.statData);
                    if (tiles != null) RangeIndicator.Instance.ShowTiles(tiles, new Color(col.r, col.g, col.b, 0.35f), 2.5f);
                    return;

                case TurretType.MeleeTurret:
                    var melee = turret as MeleeTurret;
                    if (melee == null) return;
                    float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
                    float len = melee.range > 0 ? melee.range : step * 1.2f;
                    RangeIndicator.Instance.ShowDirection(turret.transform.position, melee.FacingDir, len, new Color(col.r, col.g, col.b, 0.75f), step * 0.85f, 2.5f);
                    return;

                case TurretType.Wall:
                case TurretType.Wall2x1:
                case TurretType.Wall1x2:
                case TurretType.Wall2x2:
                    return;

                default:
                    if (turret.range > 0f)
                        RangeIndicator.Instance.ShowCircle(turret.transform.position, turret.range, new Color(col.r, col.g, col.b, 0.8f), 2.5f);
                    return;
            }
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────
        private float GetDragRange(TurretStatData sd)
        {
            if (sd != null && sd.levels != null && sd.levels.Length > 0) return sd.levels[0].range;
            if (_selectedTurret != null) return _selectedTurret.range;
            return 0f;
        }

        private Tile RaycastTile(Vector3 worldPos)
        {
            var hit = Physics2D.Raycast(worldPos, Vector2.zero);
            return hit.collider != null ? hit.collider.GetComponent<Tile>() : null;
        }

        private bool IsPointerOverUI(Vector3 worldPos)
        {
            // EventSystem을 통해 UI 위인지 확인
            return UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        // 레거시 호환 (외부에서 호출 가능성)
        public void StartDragFromUI(TurretType type, Color col) => SelectTurretFromUI(type, col);
    }
}
