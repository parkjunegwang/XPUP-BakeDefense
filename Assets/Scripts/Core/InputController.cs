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

        private void Start()
        {
            _ghost = new GameObject("DragGhost");
            _ghostSr = _ghost.AddComponent<SpriteRenderer>();
            _ghostSr.sprite       = GameSetup.WhiteSquareStatic();
            _ghostSr.color        = new Color(1f, 1f, 1f, 0.45f);
            _ghostSr.sortingOrder = SLayer.Ghost;
            _ghost.SetActive(false);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screen   = mouse.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            worldPos.z = 0f;

            Tile hover = RaycastTile(worldPos);
            UpdateHover(hover);

            // 포탑 드래그 시작 (좌클릭)
            if (mouse.leftButton.wasPressedThisFrame && !_isDragging)
            {
                if (hover != null && hover.HasTurret()
                    && GameManager.Instance.CurrentState == GameState.Preparation)
                {
                    // passableOverride 타일(전기 가운데)은 드래그 불가 - 실제 포탑이 없는 타일
                    // placedTurret은 있지만 그게 메인 turret인지 확인
                    var t = hover.placedTurret;
                    if (t != null && t.currentTile == hover)
                        StartDragTile(hover);
                    else if (t != null && t.currentTile != hover)
                    {
                        // 멀티타일 타워의 서브타일 - currentTile로 이동
                        StartDragTile(t.currentTile);
                    }
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame && _isDragging)
                EndDrag(worldPos);
        }

        // ── UI 버튼 드래그 시작 ───────────────────────────────────────
public void StartDragFromUI(TurretType type, Color col) { if (GameManager.Instance.CurrentState != GameState.Preparation) return; if (!InventoryManager.Instance.CanPlace(type)) { UIManager.Instance?.ShowMessage("No stock!"); return; } _isDragging = true; _fromUI = true; _dragType = type; _fromTile = null; _dragTurret = null; var def = TurretManager.Instance.GetDef(type); _dragSizeX = def.sizeX; _dragSizeY = def.sizeY; float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap; _ghost.transform.localScale = new Vector3(_dragSizeX * step * 0.88f, _dragSizeY * step * 0.88f, 1f); _ghostSr.color = new Color(col.r, col.g, col.b, 0.5f); _ghost.SetActive(true); }

        // ── 타일 포탑 드래그 시작 ────────────────────────────────────
        private void StartDragTile(Tile tile)
        {
            var turret = tile.placedTurret;
            if (turret == null) return;

            _isDragging = true; _fromUI = false;
            _dragType   = turret.turretType;
            _fromTile   = tile;
            _dragTurret = turret;

            var def = TurretManager.Instance.GetDef(turret.turretType);
            _dragSizeX = def.sizeX;
            _dragSizeY = def.sizeY;

            Color c = turret.bodyRenderer != null ? turret.bodyRenderer.color : Color.white;
            _ghostSr.color = new Color(c.r, c.g, c.b, 0.5f);

            float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            _ghost.transform.localScale = new Vector3(_dragSizeX * step * 0.88f, _dragSizeY * step * 0.88f, 1f);
            _ghost.SetActive(true);
        }

        // ── 호버 하이라이트 + 고스트 스냅 ────────────────────────────
private void UpdateHover(Tile hover) { foreach (var t in _lastHighlighted) t.RefreshColor(); _lastHighlighted.Clear(); if (hover == null) { _ghost.SetActive(_isDragging); return; } if (_isDragging) { var prefab = _fromUI ? TurretManager.Instance.GetType().GetField("rangedTurretPrefab") : null; TurretStatData sd = null; if (_dragTurret != null) sd = _dragTurret.statData; else { var pf = GetPrefabForType(_dragType); sd = pf != null ? pf.GetComponent<TurretBase>()?.statData : null; } var tiles = TurretManager.Instance.GetShapeTiles(hover, _dragType, sd); bool canPlace = _fromUI ? TurretManager.Instance.CanPlaceAt(hover, _dragType) : (_dragTurret != null && TurretManager.Instance.CanMoveAt(_dragTurret, hover)); Color hlCol = canPlace ? Tile.ColorHighlight : Tile.ColorError; if (tiles != null) { foreach (var t in tiles) { t.SetHighlight(hlCol); _lastHighlighted.Add(t); } Vector3 center = Vector3.zero; foreach (var t in tiles) center += t.transform.position; center /= tiles.Count; _ghost.transform.position = new Vector3(center.x, center.y, 0f); } else { hover.SetHighlight(Tile.ColorError); _lastHighlighted.Add(hover); _ghost.transform.position = hover.transform.position; } } else { if (hover.IsPlaceable()) { hover.SetHighlight(new Color(0.3f, 0.5f, 0.85f)); _lastHighlighted.Add(hover); } } }

        // ── 드래그 종료 ───────────────────────────────────────────────
        private void EndDrag(Vector3 worldPos)
        {
            _ghost.SetActive(false);
            _isDragging = false;
            foreach (var t in _lastHighlighted) t.RefreshColor();
            _lastHighlighted.Clear();

            Tile drop = RaycastTile(worldPos);
            if (drop == null) { Reset(); return; }

            if (_fromUI)
            {
                TurretManager.Instance.PlaceSelectedTurret(drop, _dragType);
            }
            else if (_fromTile != null && _dragTurret != null)
            {
                if (drop == _fromTile || drop == _dragTurret.currentTile) { Reset(); return; }

                if (drop.HasTurret() && drop.placedTurret != _dragTurret)
                    TurretManager.Instance.TryMerge(_dragTurret.currentTile, drop);
                else
                    TurretManager.Instance.MoveTurret(_dragTurret.currentTile, drop);
            }
            Reset();
        }

        private void Reset()
        {
            _fromUI = false; _fromTile = null; _dragTurret = null;
            _dragSizeX = 1; _dragSizeY = 1;
        }

        private GameObject GetPrefabForType(TurretType type) { var tm = TurretManager.Instance; return type switch { TurretType.RangedTurret => tm.rangedTurretPrefab, TurretType.MeleeTurret  => tm.meleeTurretPrefab, TurretType.SpikeTrap    => tm.spikeTrapPrefab, TurretType.ElectricGate => tm.electricGatePrefab, TurretType.Wall         => tm.wallPrefab, _ => null }; }

        private Tile RaycastTile(Vector3 worldPos)
        {
            var hit = Physics2D.Raycast(worldPos, Vector2.zero);
            return hit.collider != null ? hit.collider.GetComponent<Tile>() : null;
        }
    }
}
