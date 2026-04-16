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
public void StartDragFromUI(TurretType type, Color col) { var state = GameManager.Instance.CurrentState; if (state != GameState.Preparation && state != GameState.WaveInProgress) return; if (!InventoryManager.Instance.CanPlace(type)) { UIManager.Instance?.ShowMessage("No stock!"); return; } _isDragging = true; _dragType = type; _dragColor = col; var def = TurretManager.Instance.GetDef(type); var sd = TurretManager.Instance.GetStatData(type); float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap; int bW = def.sizeX, bH = def.sizeY; if (sd != null && sd.HasCustomShape() && sd.tileShape != null && sd.tileShape.Length > 0) { bW = 1; bH = 1; foreach (var o in sd.tileShape) { bW = Mathf.Max(bW, o.x + 1); bH = Mathf.Max(bH, o.y + 1); } } _dragSizeX = bW; _dragSizeY = bH; _ghost.transform.localScale = new Vector3(bW * step * 0.88f, bH * step * 0.88f, 1f); _ghostSr.color = new Color(col.r, col.g, col.b, 0.5f); _ghost.SetActive(true); RangeIndicator.Instance?.Hide(); MapManager.Instance?.ShowAllTiles(); if (state == GameState.WaveInProgress) Time.timeScale = 0.15f; }

        // 취소 버튼에서 호출
public void CancelDrag() { if (!_isDragging) return; _isDragging = false; _ghost.SetActive(false); RangeIndicator.Instance?.Hide(); ClearHighlight(); MapManager.Instance?.HideAllTiles(); Time.timeScale = 1f; }

        // ── 드래그 중 고스트/하이라이트 갱신 ────────────────────────
private void UpdateGhostAndHighlight(Tile hover, Vector3 worldPos)
        {
            ClearHighlight();

            if (IsPointerOverUI())
            {
                _ghost.SetActive(false);
                return;
            }

            _ghost.SetActive(true);

            // hover가 null이면(타일 갭) RaycastTile의 스냅으로 다시 시도
            if (hover == null)
                hover = RaycastTile(worldPos);

            if (hover == null)
            {
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
private void EndDrag(Vector3 worldPos)
        {
            _isDragging = false;
            _ghost.SetActive(false);
            ClearHighlight();
            // timeScale 먼저 복구 - 이후 로직에서 이벤트 씨힐 방지
            Time.timeScale = 1f;
            MapManager.Instance?.HideAllTiles();
            RangeIndicator.Instance?.Hide();

            if (IsPointerOverUI()) return;

            // 타일 갭 포함 - 스냅 방식으로 타일 찾기
            Tile drop = RaycastTile(worldPos);
            if (drop == null) return;

            // 레벨업: 같은 타입 터렛이 이미 있으면
            if (drop.HasTurret() && drop.placedTurret != null
                && drop.placedTurret.turretType == _dragType)
            {
                if (InventoryManager.Instance.Consume(_dragType))
                {
                    drop.placedTurret.LevelUp();
                    StartCoroutine(MergePulse(drop.placedTurret));
                    UIManager.Instance?.ShowMessage($"Level Up! Lv.{drop.placedTurret.level}");
                    MonsterManager.Instance?.RequestPathRecalc();
                }
                return;
            }

            TurretManager.Instance.PlaceSelectedTurret(drop, _dragType);
        }

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
            Color col = def.color;
            col.a = 0.85f;

            switch (turret.turretType)
            {
                case TurretType.SpikeTrap:
                        var tiles = TurretManager.Instance.GetShapeTiles(turret.currentTile, turret.turretType, turret.statData);
                    if (tiles != null)
                        RangeIndicator.Instance.ShowTiles(tiles, new Color(col.r, col.g, col.b, 0.5f), 2.5f);
                    return;

                case TurretType.MeleeTurret:
                    var melee = turret as MeleeTurret;
                    if (melee == null) return;
                    var meleeTiles = GetMeleeTiles(melee);
                    if (meleeTiles != null && meleeTiles.Count > 0)
                        RangeIndicator.Instance.ShowTiles(meleeTiles, new Color(col.r, col.g, col.b, 0.55f), 2.5f);
                    return;

                case TurretType.CrossMeleeTurret:
                    var cross = turret as CrossMeleeTurret;
                    int atkTiles = cross != null ? cross.AttackTiles : 1;
                    RangeIndicator.Instance.ShowCross(turret.transform.position, atkTiles, col, 2.5f);
                    return;

                case TurretType.DragonStatue:
                    var dragon = turret as DragonStatue;
                    float bRange = dragon != null ? dragon.breathRange : 2.2f;
                    float bWidth = dragon != null ? dragon.breathWidth : 0.9f;
                    RangeIndicator.Instance.ShowBreathRect(turret.transform.position,
                        bRange, bWidth, col, 2.5f);
                    return;

                case TurretType.BlackHole:
                    // 타워 탐지 범위 (연한)
                    if (turret.range > 0f)
                        RangeIndicator.Instance.ShowCircle(turret.transform.position, turret.range,
                            new Color(0.5f, 0f, 1f, 0.5f), 2.5f);
                    return;

                case TurretType.Wall:
                case TurretType.Wall2x1:
                case TurretType.Wall1x2:
                case TurretType.Wall2x2:
                    return;

                default:
                    float r = turret.range;
                    if (r <= 0f && turret.statData != null && turret.statData.levels?.Length > 0)
                        r = turret.statData.levels[0].range;
                    if (r > 0f)
                        RangeIndicator.Instance.ShowCircle(turret.transform.position, r, col, 2.5f);
                    return;
            }
        }

        // ── 드래그 중 사정거리 표시 ──────────────────────────────────
private void UpdateRangeIndicator(Vector3 center, TurretStatData sd, bool canPlace)
        {
            if (RangeIndicator.Instance == null) return;
            var def = TurretManager.Instance.GetDef(_dragType);
            // 설치 가능 여부에 따라 색상 결정
            Color baseCol = canPlace ? def.color : new Color(1f, 0.25f, 0.25f);
            baseCol.a = 0.85f;

            switch (_dragType)
            {
                case TurretType.SpikeTrap:
                
                    if (_lastHighlighted.Count > 0)
                        RangeIndicator.Instance.ShowTiles(_lastHighlighted,
                            new Color(baseCol.r, baseCol.g, baseCol.b, 0.4f));
                    else RangeIndicator.Instance.Hide();
                    return;

                case TurretType.MeleeTurret:
                    // 드래그 중에는 아래 방향 1칸 미리보기
                    var dragMeleeDir = new Vector2Int(0, -1);
                    var dragMeleeTiles = GetDragMeleeTiles(center, dragMeleeDir, 1);
                    if (dragMeleeTiles != null && dragMeleeTiles.Count > 0)
                        RangeIndicator.Instance.ShowTiles(dragMeleeTiles, new Color(baseCol.r, baseCol.g, baseCol.b, 0.45f));
                    else RangeIndicator.Instance.Hide();
                    return;

                case TurretType.CrossMeleeTurret:
                    // 드래그 중: lv1 기준 1칸 미리보기
                    RangeIndicator.Instance.ShowCross(center, 1, baseCol);
                    return;

                case TurretType.DragonStatue:
                    float bRange = 2.2f;
                    float bWidth = 0.9f;
                    if (sd != null && sd.levels?.Length > 0 && sd.levels[0].range > 0)
                        bRange = sd.levels[0].range;
                    RangeIndicator.Instance.ShowBreathRect(center, bRange, bWidth, baseCol);
                    return;

                case TurretType.Wall:
                case TurretType.Wall2x1:
                case TurretType.Wall1x2:
                case TurretType.Wall2x2:
                    RangeIndicator.Instance.Hide();
                    return;

                default:
                    float range = sd != null && sd.levels?.Length > 0 ? sd.levels[0].range : 0f;
                    if (range > 0f)
                        RangeIndicator.Instance.ShowCircle(center, range, baseCol);
                    else
                        RangeIndicator.Instance.Hide();
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
            // 1차: 정확한 Collider 히트
            var hit = Physics2D.Raycast(worldPos, Vector2.zero);
            if (hit.collider != null)
            {
                var t = hit.collider.GetComponent<Tile>();
                if (t != null) return t;
            }

            // 2차: 타일 갭에 떨어진 경우 → MapManager 그리드로 가장 가까운 타일 스냅
            var map = MapManager.Instance;
            if (map == null) return null;

            float step    = map.tileSize + map.tileGap;
            float offsetX = -(map.columns - 1) * step * 0.5f;
            float offsetY = -(map.rows    - 1) * step * 0.5f;

            int gx = Mathf.RoundToInt((worldPos.x - offsetX) / step);
            int gy = Mathf.RoundToInt((worldPos.y - offsetY) / step);

            // 그리드 범위 클램프
            gx = Mathf.Clamp(gx, 0, map.columns - 1);
            gy = Mathf.Clamp(gy, 0, map.rows    - 1);

            return map.GetTile(gx, gy);
        }

        /// <summary>설치된 MeleeTurret의 실제 FacingDir 기준 공격 타일 목록</summary>
        private List<Tile> GetMeleeTiles(MeleeTurret melee)
        {
            var result = new List<Tile>();
            if (melee.currentTile == null) return result;
            var map = MapManager.Instance;
            var dir = melee.FacingDir;
            int ox = melee.currentTile.gridX;
            int oy = melee.currentTile.gridY;
            // statData의 attackTiles를 가져오려 하지만, 없으면 1칸
            int tiles = 1;
            if (melee.statData != null)
            {
                var lv = melee.statData.GetLevel(melee.level);
                tiles = Mathf.Max(1, lv.attackTiles);
            }
            for (int i = 1; i <= tiles; i++)
            {
                var t = map.GetTile(ox + dir.x * i, oy + dir.y * i);
                if (t != null) result.Add(t);
            }
            return result;
        }

        /// <summary>드래그 중 임시위치 기준 방향 타일 목록</summary>
        private List<Tile> GetDragMeleeTiles(Vector3 worldCenter, Vector2Int dir, int count)
        {
            var result = new List<Tile>();
            var map = MapManager.Instance;
            if (map == null) return result;
            float step    = map.tileSize + map.tileGap;
            float offsetX = -(map.columns - 1) * step * 0.5f;
            float offsetY = -(map.rows    - 1) * step * 0.5f;
            int gx = Mathf.RoundToInt((worldCenter.x - offsetX) / step);
            int gy = Mathf.RoundToInt((worldCenter.y - offsetY) / step);
            for (int i = 1; i <= count; i++)
            {
                var t = map.GetTile(gx + dir.x * i, gy + dir.y * i);
                if (t != null) result.Add(t);
            }
            return result;
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
