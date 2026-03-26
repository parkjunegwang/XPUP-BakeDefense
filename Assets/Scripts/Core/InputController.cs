using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Underdark
{
    public class InputController : MonoBehaviour
    {
        public static InputController Instance { get; private set; }

        private GameObject     _ghost;
        private SpriteRenderer _ghostSr;

        private bool       _isDragging;
        private bool       _fromUI;
        private TurretType _dragType;
        private Tile       _fromTile;
        private TurretBase _dragTurret;
        private int        _dragSizeX = 1;
        private int        _dragSizeY = 1;

        private List<Tile> _lastHighlighted = new List<Tile>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

private void Start() { _ghost = new GameObject("DragGhost"); _ghostSr = _ghost.AddComponent<SpriteRenderer>(); _ghostSr.sprite = GameSetup.WhiteSquareStatic(); _ghostSr.color = new Color(1f, 1f, 1f, 0.45f); _ghostSr.sortingOrder = SLayer.Ghost; _ghost.SetActive(false); var riGo = new GameObject("RangeIndicator"); riGo.AddComponent<RangeIndicator>(); }

private void Update() { var mouse = Mouse.current; if (mouse == null) return; Vector2 screen = mouse.position.ReadValue(); Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f)); worldPos.z = 0f; Tile hover = RaycastTile(worldPos); UpdateHover(hover); if (mouse.leftButton.wasPressedThisFrame && !_isDragging) { if (hover != null && hover.HasTurret()) { var t = hover.placedTurret; if (GameManager.Instance.CurrentState == GameState.Preparation) { if (t != null && t.isFromMapData) { UIManager.Instance?.ShowMessage("Map walls cannot be moved!"); ShowTurretRange(t); } else if (t != null) { ShowTurretRange(t); if (t.currentTile == hover) StartDragTile(hover); else StartDragTile(t.currentTile); } } else { ShowTurretRange(t); } } } if (mouse.leftButton.wasReleasedThisFrame && _isDragging) EndDrag(worldPos); }

        // ── UI 버튼 드래그 시작 ───────────────────────────────────────
public void StartDragFromUI(TurretType type, Color col) { if (GameManager.Instance.CurrentState != GameState.Preparation) return; if (!InventoryManager.Instance.CanPlace(type)) { UIManager.Instance?.ShowMessage("No stock!"); return; } _isDragging = true; _fromUI = true; _dragType = type; _fromTile = null; _dragTurret = null; var def = TurretManager.Instance.GetDef(type); _dragSizeX = def.sizeX; _dragSizeY = def.sizeY; var sd = TurretManager.Instance.GetStatData(type); float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap; int bW = _dragSizeX, bH = _dragSizeY; if (sd != null && sd.HasCustomShape() && sd.tileShape != null && sd.tileShape.Length > 0) { bW = 1; bH = 1; foreach (var o in sd.tileShape) { bW = Mathf.Max(bW, o.x + 1); bH = Mathf.Max(bH, o.y + 1); } } _ghost.transform.localScale = new Vector3(bW * step * 0.88f, bH * step * 0.88f, 1f); _ghostSr.color = new Color(col.r, col.g, col.b, 0.5f); _ghost.SetActive(true); }

        // ── 타일 포탑 드래그 시작 ────────────────────────────────────
private void StartDragTile(Tile tile) { var turret = tile.placedTurret; if (turret == null) return; if (turret.isFromMapData) { UIManager.Instance?.ShowMessage("Map walls cannot be moved!"); return; } _isDragging = true; _fromUI = false; _dragType = turret.turretType; _fromTile = tile; _dragTurret = turret; var def = TurretManager.Instance.GetDef(turret.turretType); _dragSizeX = def.sizeX; _dragSizeY = def.sizeY; Color c = turret.bodyRenderer != null ? turret.bodyRenderer.color : Color.white; _ghostSr.color = new Color(c.r, c.g, c.b, 0.5f); float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap; int bW = _dragSizeX, bH = _dragSizeY; var sd = turret.statData; if (sd != null && sd.HasCustomShape() && sd.tileShape != null && sd.tileShape.Length > 0) { bW = 1; bH = 1; foreach (var o in sd.tileShape) { bW = Mathf.Max(bW, o.x + 1); bH = Mathf.Max(bH, o.y + 1); } } _ghost.transform.localScale = new Vector3(bW * step * 0.88f, bH * step * 0.88f, 1f); _ghost.SetActive(true); }

        // ── 호버 하이라이트 + 고스트 스냅 ────────────────────────────
private void UpdateHover(Tile hover) { foreach (var t in _lastHighlighted) t.RefreshColor(); _lastHighlighted.Clear(); if (hover == null) { _ghost.SetActive(_isDragging); if (!_isDragging) RangeIndicator.Instance?.Hide(); return; } if (_isDragging) { TurretStatData sd = _dragTurret != null ? _dragTurret.statData : TurretManager.Instance.GetStatData(_dragType); var tiles = TurretManager.Instance.GetShapeTiles(hover, _dragType, sd); bool canPlace = _fromUI ? TurretManager.Instance.CanPlaceAt(hover, _dragType) : (_dragTurret != null && TurretManager.Instance.CanMoveAt(_dragTurret, hover)); Color hlCol = canPlace ? Tile.ColorHighlight : Tile.ColorError; Vector3 center; if (tiles != null) { foreach (var t in tiles) { t.SetHighlight(hlCol); _lastHighlighted.Add(t); } center = Vector3.zero; foreach (var t in tiles) center += t.transform.position; center /= tiles.Count; _ghost.transform.position = new Vector3(center.x, center.y, 0f); } else { hover.SetHighlight(Tile.ColorError); _lastHighlighted.Add(hover); center = hover.transform.position; _ghost.transform.position = center; } UpdateDragRangeIndicator(center, sd, canPlace); } else { if (hover.IsPlaceable()) { hover.SetHighlight(new Color(0.3f, 0.5f, 0.85f)); _lastHighlighted.Add(hover); } RangeIndicator.Instance?.Hide(); } }

        // ── 드래그 종료 ───────────────────────────────────────────────
private void EndDrag(Vector3 worldPos) { _ghost.SetActive(false); _isDragging = false; RangeIndicator.Instance?.Hide(); foreach (var t in _lastHighlighted) t.RefreshColor(); _lastHighlighted.Clear(); Tile drop = RaycastTile(worldPos); if (drop == null) { Reset(); return; } if (_fromUI) { TurretManager.Instance.PlaceSelectedTurret(drop, _dragType); } else if (_fromTile != null && _dragTurret != null) { if (drop == _fromTile || drop == _dragTurret.currentTile) { Reset(); return; } if (drop.HasTurret() && drop.placedTurret != _dragTurret) TurretManager.Instance.TryMerge(_dragTurret.currentTile, drop); else TurretManager.Instance.MoveTurret(_dragTurret.currentTile, drop); } Reset(); }

        private void Reset()
        {
            _fromUI = false; _fromTile = null; _dragTurret = null;
            _dragSizeX = 1; _dragSizeY = 1;
        }

private GameObject GetPrefabForType(TurretType type) { return TurretManager.Instance.GetPrefabPublic(type); }

        private Tile RaycastTile(Vector3 worldPos)
        {
            var hit = Physics2D.Raycast(worldPos, Vector2.zero);
            return hit.collider != null ? hit.collider.GetComponent<Tile>() : null;
        }
    

private float GetDragRange(TurretStatData sd) { if (sd != null && sd.levels != null && sd.levels.Length > 0) return sd.levels[0].range; if (_dragTurret != null) return _dragTurret.range; return 0f; } private void UpdateDragRangeIndicator(Vector3 center, TurretStatData sd, bool canPlace) { if (RangeIndicator.Instance == null) return; Color rc = canPlace ? new Color(0.4f, 1f, 0.5f, 0.7f) : new Color(1f, 0.3f, 0.3f, 0.7f); switch (_dragType) { case TurretType.SpikeTrap: case TurretType.ElectricGate: { var hoveredTile = _lastHighlighted.Count > 0 ? _lastHighlighted[0] : null; if (hoveredTile != null && _lastHighlighted.Count > 0) RangeIndicator.Instance.ShowTiles(_lastHighlighted, new Color(rc.r, rc.g, rc.b, 0.3f)); else RangeIndicator.Instance.Hide(); return; } case TurretType.MeleeTurret: { float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap; Vector2Int dir = _dragTurret != null ? (_dragTurret as MeleeTurret)?.FacingDir ?? new Vector2Int(0, -1) : new Vector2Int(0, -1); float len = GetDragRange(sd); if (len <= 0f) len = step * 1.2f; RangeIndicator.Instance.ShowDirection(center, dir, len, new Color(rc.r, rc.g, rc.b, 0.75f), step * 0.85f); return; } case TurretType.Wall: case TurretType.Wall2x1: case TurretType.Wall1x2: case TurretType.Wall2x2: RangeIndicator.Instance.Hide(); return; default: { float range = GetDragRange(sd); if (range > 0f) RangeIndicator.Instance.ShowCircle(center, range, rc); else RangeIndicator.Instance.Hide(); return; } } }


private void ShowTurretRange(TurretBase turret) { if (turret == null || RangeIndicator.Instance == null) return; var def = TurretManager.Instance.GetDef(turret.turretType); Color col = def.color; col.a = 0.55f; switch (turret.turretType) { case TurretType.SpikeTrap: case TurretType.ElectricGate: { var sd = turret.statData; var tiles = TurretManager.Instance.GetShapeTiles(turret.currentTile, turret.turretType, sd); if (tiles != null) RangeIndicator.Instance.ShowTiles(tiles, new Color(col.r, col.g, col.b, 0.35f), 2.5f); return; } case TurretType.MeleeTurret: { var melee = turret as MeleeTurret; if (melee == null) return; float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap; float len = melee.range > 0 ? melee.range : step * 1.2f; RangeIndicator.Instance.ShowDirection(turret.transform.position, melee.FacingDir, len, new Color(col.r, col.g, col.b, 0.75f), step * 0.85f, 2.5f); return; } case TurretType.Wall: case TurretType.Wall2x1: case TurretType.Wall1x2: case TurretType.Wall2x2: return; default: if (turret.range > 0f) RangeIndicator.Instance.ShowCircle(turret.transform.position, turret.range, new Color(col.r, col.g, col.b, 0.8f), 2.5f); return; } }
}
}
