using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Underdark
{
    /// <summary>
    /// 드래그앤드롭 입력 컨트롤러.
    ///
    /// 흐름:
    ///   1. 인벤토리 슬롯에서 PointerDown → 드래그 시작 (StartDragFromUI)
    ///   2. 드래그 중 타일 위 → 고스트 + 하이라이트
    ///   3. UI 영역에 드롭 → 취소 (취소버튼 포함)
    ///   4. 타일에 드롭 → 배치 (같은 타입 있으면 레벨업)
    ///   5. 설치된 터렛은 클릭으로 사정거리만 표시 (이동 불가)
    /// </summary>
    public class InputController : MonoBehaviour
    {
        public static InputController Instance { get; private set; }

        private bool       _isDragging;
        private TurretType _dragType;
        private Color      _dragColor;
        private int        _dragSizeX = 1;
        private int        _dragSizeY = 1;

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
            _ghost   = new GameObject("DragGhost");
            _ghostSr = _ghost.AddComponent<SpriteRenderer>();
            _ghostSr.sprite       = GameSetup.WhiteSquareStatic();
            _ghostSr.color        = new Color(1f, 1f, 1f, 0.45f);
            _ghostSr.sortingOrder = SLayer.Ghost;
            _ghost.SetActive(false);

            var riGo = new GameObject("RangeIndicator");
            riGo.AddComponent<RangeIndicator>();
        }

        private void Update()
        {
            Vector2 screenPos = Vector2.zero;
            bool released = false;

            var mouse = Mouse.current;
            var touch = Touchscreen.current;

            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                released  = mouse.leftButton.wasReleasedThisFrame;
            }
            else if (touch != null && touch.touches.Count > 0)
            {
                var t0 = touch.touches[0];
                screenPos = t0.position.ReadValue();
                released  = t0.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended
                         || t0.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled;
            }

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            worldPos.z = 0f;

            if (_isDragging)
            {
                Tile hover = RaycastTile(worldPos);
                UpdateGhostAndHighlight(hover, worldPos);

                if (released)
                    EndDrag(worldPos);
            }
            else
            {
                // 드래그 중 아닐 때 - 터렛 클릭 시 사정거리 표시
                if (released && !IsPointerOverUI())
                {
                    Tile tile = RaycastTile(worldPos);
                    if (tile != null && tile.HasTurret())
                        ShowTurretRange(tile.placedTurret);
                    else
                        RangeIndicator.Instance?.Hide();
                }
            }
        }

        // ── UI 슬롯에서 드래그 시작 (UIInventoryPanel에서 호출) ──────
public void StartDragFromUI(TurretType type, Color col) { var state = GameManager.Instance.CurrentState; if (state != GameState.Preparation && state != GameState.WaveInProgress) return; if (!InventoryManager.Instance.CanPlace(type)) { UIManager.Instance?.ShowMessage("No stock!"); return; } _isDragging = true; _dragType = type; _dragColor = col; var def = TurretManager.Instance.GetDef(type); var sd = TurretManager.Instance.GetStatData(type); float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap; int bW = def.sizeX, bH = def.sizeY; if (sd != null && sd.HasCustomShape() && sd.tileShape != null && sd.tileShape.Length > 0) { bW = 1; bH = 1; foreach (var o in sd.tileShape) { bW = Mathf.Max(bW, o.x + 1); bH = Mathf.Max(bH, o.y + 1); } } _dragSizeX = bW; _dragSizeY = bH; _ghost.transform.localScale = new Vector3(bW * step * 0.88f, bH * step * 0.88f, 1f); _ghostSr.color = new Color(col.r, col.g, col.b, 0.5f); _ghost.SetActive(true); RangeIndicator.Instance?.Hide(); if (state == GameState.WaveInProgress) Time.timeScale = 0.15f; }

        // 취소 버튼에서 호출
public void CancelDrag() { if (!_isDragging) return; _isDragging = false; _ghost.SetActive(false); RangeIndicator.Instance?.Hide(); ClearHighlight(); Time.timeScale = 1f; }

        // ── 드래그 중 고스트/하이라이트 갱신 ────────────────────────
        private void UpdateGhostAndHighlight(Tile hover, Vector3 worldPos)
        {
            ClearHighlight();

            // UI 위에 있으면 고스트 숨김 (취소 상태 표시)
            if (IsPointerOverUI())
            {
                _ghost.SetActive(false);
                return;
            }

            _ghost.SetActive(true);

            if (hover == null)
            {
                // 타일 없는 곳 - 월드 위치에 고스트만
                _ghost.transform.position = worldPos;
                RangeIndicator.Instance?.Hide();
                return;
            }

            var sd = TurretManager.Instance.GetStatData(_dragType);
            var tiles = TurretManager.Instance.GetShapeTiles(hover, _dragType, sd);
            bool canPlace = TurretManager.Instance.CanPlaceAt(hover, _dragType);

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
                hover.SetHighlight(Tile.ColorError);
                _lastHighlighted.Add(hover);
            }

            _ghost.transform.position = new Vector3(center.x, center.y, 0f);
            UpdateRangeIndicator(center, sd, canPlace);
        }

        // ── 드롭 처리 ────────────────────────────────────────────────
private void EndDrag(Vector3 worldPos) { _isDragging = false; _ghost.SetActive(false); ClearHighlight(); Time.timeScale = 1f; if (IsPointerOverUI()) { RangeIndicator.Instance?.Hide(); return; } Tile drop = RaycastTile(worldPos); if (drop == null) { RangeIndicator.Instance?.Hide(); return; } if (drop.HasTurret() && drop.placedTurret != null && drop.placedTurret.turretType == _dragType) { if (InventoryManager.Instance.Consume(_dragType)) { drop.placedTurret.LevelUp(); StartCoroutine(MergePulse(drop.placedTurret)); UIManager.Instance?.ShowMessage($"Level Up! Lv.{drop.placedTurret.level}"); MonsterManager.Instance?.RequestPathRecalc(); } return; } TurretManager.Instance.PlaceSelectedTurret(drop, _dragType); RangeIndicator.Instance?.Hide(); }

        private System.Collections.IEnumerator MergePulse(TurretBase t)
        {
            Vector3 orig = t.transform.localScale;
            t.transform.localScale = orig * 1.45f;
            yield return new WaitForSeconds(0.15f);
            t.transform.localScale = orig;
        }

        // ── 사정거리 표시 (배치된 터렛 클릭) ────────────────────────
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
                    RangeIndicator.Instance.ShowDirection(turret.transform.position, melee.FacingDir, len,
                        new Color(col.r, col.g, col.b, 0.75f), step * 0.85f, 2.5f);
                    return;

                case TurretType.Wall:
                case TurretType.Wall2x1:
                case TurretType.Wall1x2:
                case TurretType.Wall2x2:
                    return;

                default:
                    if (turret.range > 0f)
                        RangeIndicator.Instance.ShowCircle(turret.transform.position, turret.range,
                            new Color(col.r, col.g, col.b, 0.8f), 2.5f);
                    return;
            }
        }

        // ── 드래그 중 사정거리 표시 ──────────────────────────────────
        private void UpdateRangeIndicator(Vector3 center, TurretStatData sd, bool canPlace)
        {
            if (RangeIndicator.Instance == null) return;
            Color rc = canPlace ? new Color(0.4f, 1f, 0.5f, 0.7f) : new Color(1f, 0.3f, 0.3f, 0.7f);

            switch (_dragType)
            {
                case TurretType.SpikeTrap:
                case TurretType.ElectricGate:
                    if (_lastHighlighted.Count > 0)
                        RangeIndicator.Instance.ShowTiles(_lastHighlighted, new Color(rc.r, rc.g, rc.b, 0.3f));
                    else RangeIndicator.Instance.Hide();
                    return;

                case TurretType.MeleeTurret:
                    float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
                    float len = sd != null && sd.levels?.Length > 0 ? sd.levels[0].range : step * 1.2f;
                    RangeIndicator.Instance.ShowDirection(center, new Vector2Int(0, -1), len,
                        new Color(rc.r, rc.g, rc.b, 0.75f), step * 0.85f);
                    return;

                case TurretType.Wall:
                case TurretType.Wall2x1:
                case TurretType.Wall1x2:
                case TurretType.Wall2x2:
                    RangeIndicator.Instance.Hide();
                    return;

                default:
                    float range = sd != null && sd.levels?.Length > 0 ? sd.levels[0].range : 0f;
                    if (range > 0f) RangeIndicator.Instance.ShowCircle(center, range, rc);
                    else RangeIndicator.Instance.Hide();
                    return;
            }
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────
        private void ClearHighlight()
        {
            foreach (var t in _lastHighlighted) t.RefreshColor();
            _lastHighlighted.Clear();
        }

        private Tile RaycastTile(Vector3 worldPos)
        {
            var hit = Physics2D.Raycast(worldPos, Vector2.zero);
            return hit.collider != null ? hit.collider.GetComponent<Tile>() : null;
        }

        private bool IsPointerOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        // 레거시 호환
        public void SelectTurretFromUI(TurretType type, Color col) => StartDragFromUI(type, col);
        public void CancelSelection() => CancelDrag();
    }
}
