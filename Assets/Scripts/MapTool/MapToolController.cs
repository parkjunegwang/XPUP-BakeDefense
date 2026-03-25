using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Underdark
{
    public class MapToolController : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int   columns  = 7;
        public int   rows     = 11;
        public float tileSize = 1.0f;
        public float tileGap  = 0.05f;

        [Header("Prefabs")]
        public GameObject tilePrefab;

        [Header("Current Map Data")]
        public MapData targetMapData;

        [Header("Floor Sprites")]
        public List<Sprite> floorSprites = new List<Sprite>();
        public int selectedSpriteIndex = 0;

        public enum PaintMode { Floor, Spawn, End, Wall1x1, Wall2x1, Wall1x2, Wall2x2, Erase }
        public PaintMode currentMode = PaintMode.Floor;

        private MapToolCell[,] _cells;
        private GameObject     _gridRoot;
        private bool           _isDrawing;

        public Sprite CurrentSprite =>
            floorSprites != null && floorSprites.Count > 0 && selectedSpriteIndex < floorSprites.Count
            ? floorSprites[selectedSpriteIndex] : null;

        private void Start() => BuildGrid();

        private void Update()
        {
            var mouse = Mouse.current;
            var kb    = Keyboard.current;
            if (mouse == null) return;

            bool altHeld = kb != null && kb.leftAltKey.isPressed;
            if (altHeld) { _isDrawing = false; return; }

            if (mouse.leftButton.wasPressedThisFrame)  _isDrawing = true;
            if (mouse.leftButton.wasReleasedThisFrame) _isDrawing = false;

            if (_isDrawing)
            {
                Vector2 screen   = mouse.position.ReadValue();
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
                worldPos.z = 0f;
                var hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider != null)
                {
                    var cell = hit.collider.GetComponent<MapToolCell>();
                    if (cell != null) Paint(cell.GridX, cell.GridY);
                }
            }

            if (kb == null) return;
            if (kb.fKey.wasPressedThisFrame) currentMode = PaintMode.Floor;
            if (kb.sKey.wasPressedThisFrame) currentMode = PaintMode.Spawn;
            if (kb.eKey.wasPressedThisFrame) currentMode = PaintMode.End;
            if (kb.digit1Key.wasPressedThisFrame) currentMode = PaintMode.Wall1x1;
            if (kb.digit2Key.wasPressedThisFrame) currentMode = PaintMode.Wall2x1;
            if (kb.digit3Key.wasPressedThisFrame) currentMode = PaintMode.Wall1x2;
            if (kb.digit4Key.wasPressedThisFrame) currentMode = PaintMode.Wall2x2;
            if (kb.xKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame)
                currentMode = PaintMode.Erase;
            if (kb.leftBracketKey.wasPressedThisFrame)  CycleSpriteIndex(-1);
            if (kb.rightBracketKey.wasPressedThisFrame) CycleSpriteIndex(1);
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl && kb.sKey.wasPressedThisFrame) SaveToMapData();
            if (ctrl && kb.lKey.wasPressedThisFrame) LoadFromMapData();
        }

        public void CycleSpriteIndex(int dir)
        {
            if (floorSprites == null || floorSprites.Count == 0) return;
            selectedSpriteIndex = (selectedSpriteIndex + dir + floorSprites.Count) % floorSprites.Count;
        }

        public void BuildGrid()
        {
            if (_gridRoot != null) Destroy(_gridRoot);
            _gridRoot = new GameObject("Grid");
            _cells    = new MapToolCell[columns, rows];

            float step = tileSize + tileGap;
            float offX = -(columns - 1) * step * 0.5f;
            float offY = -(rows    - 1) * step * 0.5f;

            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                Vector3 pos = new Vector3(offX + x * step, offY + y * step, 0f);
                GameObject go;

                if (tilePrefab != null)
                {
                    bool was = tilePrefab.activeSelf;
                    tilePrefab.SetActive(true);
                    go = Instantiate(tilePrefab, pos, Quaternion.identity, _gridRoot.transform);
                    tilePrefab.SetActive(was);
                    go.transform.localScale = Vector3.one * tileSize;
                }
                else
                {
                    go = new GameObject($"Cell_{x}_{y}");
                    go.transform.SetParent(_gridRoot.transform);
                    go.transform.position   = pos;
                    go.transform.localScale = Vector3.one * tileSize;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = MakeWhiteSquare();
                    sr.color  = new Color(0.18f, 0.18f, 0.28f);
                    go.AddComponent<BoxCollider2D>();
                }

                var existing = go.GetComponent<MapToolCell>();
                if (existing != null) Destroy(existing);
                var cell = go.AddComponent<MapToolCell>();
                cell.Init(x, y, this);
                _cells[x, y] = cell;
            }
        }

        private Sprite MakeWhiteSquare()
        {
            var tex = new Texture2D(32, 32);
            var pix = new Color32[1024];
            for (int i = 0; i < pix.Length; i++) pix[i] = Color.white;
            tex.SetPixels32(pix); tex.Apply();
            return Sprite.Create(tex, new Rect(0,0,32,32), new Vector2(0.5f,0.5f), 32f);
        }

        public void Paint(int x, int y)
        {
            var cell = GetCell(x, y);
            if (cell == null) return;
            switch (currentMode)
            {
                case PaintMode.Floor:   cell.SetFloor(CurrentSprite); break;
                case PaintMode.Spawn:   cell.SetSpawn(); break;
                case PaintMode.End:     cell.SetEnd();   break;
                case PaintMode.Wall1x1: PlaceWall(x, y, MapData.WallSizeType.Wall1x1); break;
                case PaintMode.Wall2x1: PlaceWall(x, y, MapData.WallSizeType.Wall2x1); break;
                case PaintMode.Wall1x2: PlaceWall(x, y, MapData.WallSizeType.Wall1x2); break;
                case PaintMode.Wall2x2: PlaceWall(x, y, MapData.WallSizeType.Wall2x2); break;
                case PaintMode.Erase:   cell.Erase(); break;
            }
        }

        private void PlaceWall(int x, int y, MapData.WallSizeType wallSize)
        {
            int w = (wallSize == MapData.WallSizeType.Wall2x1 || wallSize == MapData.WallSizeType.Wall2x2) ? 2 : 1;
            int h = (wallSize == MapData.WallSizeType.Wall1x2 || wallSize == MapData.WallSizeType.Wall2x2) ? 2 : 1;
            for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
                GetCell(x + dx, y + dy)?.SetWall(wallSize, dx == 0 && dy == 0);
        }

        public MapToolCell GetCell(int x, int y)
        {
            if (_cells == null || x < 0 || x >= columns || y < 0 || y >= rows) return null;
            return _cells[x, y];
        }

        // ── 저장 ──────────────────────────────────────────────────────
        public void SaveToMapData()
        {
#if UNITY_EDITOR
            if (targetMapData == null)
            {
                // 이름 입력 다이얼로그
                string savePath = UnityEditor.EditorUtility.SaveFilePanelInProject(
                    "Save Map Data", "NewMapData", "asset",
                    "Choose where to save the map", "Assets/Data/Maps");
                if (string.IsNullOrEmpty(savePath)) return;

                targetMapData = ScriptableObject.CreateInstance<MapData>();
                UnityEditor.AssetDatabase.CreateAsset(targetMapData, savePath);
                Debug.Log($"[MapTool] Created: {savePath}");
            }

            targetMapData.columns  = columns;
            targetMapData.rows     = rows;
            targetMapData.tileSize = tileSize;
            targetMapData.tileGap  = tileGap;
            targetMapData.spawnPositions.Clear();
            targetMapData.endPositions.Clear();
            targetMapData.tileOverrides.Clear();
            targetMapData.wallPlacements.Clear();

            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                var cell = GetCell(x, y);
                if (cell == null) continue;
                switch (cell.CellType)
                {
                    case MapToolCell.Type.Spawn:
                        targetMapData.spawnPositions.Add(new Vector2Int(x, y)); break;
                    case MapToolCell.Type.End:
                        targetMapData.endPositions.Add(new Vector2Int(x, y)); break;
                    case MapToolCell.Type.Floor:
                        if (cell.CurrentSprite != null)
                        {
                            string sp = UnityEditor.AssetDatabase.GetAssetPath(cell.CurrentSprite);
                            targetMapData.tileOverrides.Add(
                                new MapData.TileOverride { gridX = x, gridY = y, spritePath = sp });
                        }
                        break;
                    case MapToolCell.Type.Wall:
                        if (cell.IsWallOrigin)
                            targetMapData.wallPlacements.Add(
                                new MapData.WallPlacement { gridX = x, gridY = y, wallSize = cell.WallSize });
                        break;
                }
            }

            UnityEditor.EditorUtility.SetDirty(targetMapData);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[MapTool] Saved! → {UnityEditor.AssetDatabase.GetAssetPath(targetMapData)}");
#endif
        }

        // ── 불러오기: 파일 선택 다이얼로그 ──────────────────────────
        public void LoadFromMapData()
        {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel(
                "Load Map Data", "Assets/Data/Maps", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // 절대경로 → 상대경로
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);

            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (loaded == null)
            {
                Debug.LogWarning($"[MapTool] Not a valid MapData: {path}");
                return;
            }

            targetMapData = loaded;
            columns  = targetMapData.columns;
            rows     = targetMapData.rows;
            tileSize = targetMapData.tileSize;
            tileGap  = targetMapData.tileGap;
            BuildGrid();

            foreach (var p in targetMapData.spawnPositions) GetCell(p.x, p.y)?.SetSpawn();
            foreach (var p in targetMapData.endPositions)   GetCell(p.x, p.y)?.SetEnd();
            foreach (var t in targetMapData.tileOverrides)
            {
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(t.spritePath);
                GetCell(t.gridX, t.gridY)?.SetFloor(s);
            }
            foreach (var w in targetMapData.wallPlacements)
                PlaceWall(w.gridX, w.gridY, w.wallSize);

            Debug.Log($"[MapTool] Loaded: {loaded.name}");
#endif
        }
    }
}
