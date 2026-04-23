using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [Header("=== Map Data (우선순위 1위) ===")]
        [Tooltip("툴씬에서 저장한 MapData 에셋. 설정하면 아래 Grid Settings보다 우선 적용됩니다.")]
        public MapData mapData;

        [Header("Grid Settings")]
        public int   columns  = 7;
        public int   rows     = 11;
        public float tileSize = 1.0f;
        public float tileGap  = 0.05f;

        [Header("Prefab")]
        public GameObject tilePrefab;

        [Tooltip("EndPos에 표시할 프리팹 (없으면 빨간 타일 표시)")]
        public GameObject endMarkerPrefab;

        [Header("Spawn / End  ← 인스펙터에서 직접 설정하세요")]
        public List<Vector2Int> spawnPositions = new List<Vector2Int>();
        public List<Vector2Int> endPositions   = new List<Vector2Int>();

        // 런타임 그리드
        private Tile[,] _grid;

        // 인스펙터 값 백업 (GameSetup이 덮어쓰지 않게 하기 위해 Awake에서 저장)
        private List<Vector2Int> _savedSpawns = new List<Vector2Int>();
        private List<Vector2Int> _savedEnds   = new List<Vector2Int>();
        private bool _hasSavedPositions;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 인스펙터에 값이 있으면 백업
            if (spawnPositions.Count > 0 || endPositions.Count > 0)
            {
                _savedSpawns.AddRange(spawnPositions);
                _savedEnds.AddRange(endPositions);
                _hasSavedPositions = true;
                Debug.Log($"[MapManager] 인스펙터 스폰 {_savedSpawns.Count}개, 끝점 {_savedEnds.Count}개 백업");
            }
        }

        /// <summary>
        /// GameSetup에서 호출. 인스펙터 값이 있으면 그걸 우선 사용.
        /// </summary>
public void GenerateMap(List<Vector2Int> defaultSpawns = null, List<Vector2Int> defaultEnds = null) 
        {
            if (mapData != null) 
            {
                columns = mapData.columns; rows = mapData.rows; tileSize = mapData.tileSize;
                tileGap = mapData.tileGap;
                spawnPositions = new List<Vector2Int>(mapData.spawnPositions);
                endPositions = new List<Vector2Int>(mapData.endPositions); 
                
                Debug.Log($"[MapManager] MapData '{mapData.name}' {columns}x{rows} Spawns:{spawnPositions.Count} Ends:{endPositions.Count}"); } 
            else if (_hasSavedPositions)
            { 
                spawnPositions = new List<Vector2Int>(_savedSpawns); 
                endPositions = new List<Vector2Int>(_savedEnds);
                Debug.Log("[MapManager] Inspector spawn/end used"); 
            } 
            else if (defaultSpawns != null)
            { 
                spawnPositions = defaultSpawns; endPositions = defaultEnds ?? new List<Vector2Int>();
                Debug.Log("[MapManager] Default spawn/end used"); 
            } 
            
            ValidateAndClampPositions(); 
            BuildGrid();
            string ss = "Spawns: "; foreach (var s in spawnPositions) ss += $"({s.x},{s.y}) "; 
            string es = "Ends: "; foreach (var e in endPositions) es += $"({e.x},{e.y}) ";
            Debug.Log($"[MapManager] {ss}| {es}"); 
        }

private void BuildGrid()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            string tileInfo = tilePrefab == null ? "NULL" : tilePrefab.name;
            Debug.Log($"[MapManager] BuildGrid - Scene:{gameObject.scene.name}, Active:{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, tile:{tileInfo}");

            _grid = new Tile[columns, rows];
            float step    = tileSize + tileGap;
            float offsetX = -(columns - 1) * step * 0.5f;
            float offsetY = -(rows    - 1) * step * 0.5f;

            var endBuffer = new System.Collections.Generic.HashSet<Vector2Int>();
            foreach (var e in endPositions)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        endBuffer.Add(new Vector2Int(e.x + dx, e.y + dy));

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    Vector3 pos = new Vector3(offsetX + x * step, offsetY + y * step, 0f);

                    bool was = tilePrefab.activeSelf;
                    tilePrefab.SetActive(true);
                    GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                    tilePrefab.SetActive(was);

                    if (x == 0 && y == 0)
                        Debug.Log($"[MapManager] 첫 타일 씬: {go.scene.name}");

                    go.transform.localScale = Vector3.one * tileSize;
                    go.name = $"Tile_{x}_{y}";

                    TileType type = TileType.Empty;
                    if (IsSpawn(x, y)) type = TileType.SpawnPoint;
                    else if (IsEnd(x, y)) type = TileType.EndPoint;

                    Tile tile = go.GetComponent<Tile>();
                    tile.Init(x, y, type);
                    _grid[x, y] = tile;

                    bool inEndBuffer = endBuffer.Contains(new Vector2Int(x, y));
                    if (inEndBuffer && !IsEnd(x, y))
                    {
                        var sr = go.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = new Color(0, 0, 0, 0);
                        tile.isEndBuffer = true;
                    }
                    else if (IsEnd(x, y))
                    {
                        var sr = go.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = new Color(0, 0, 0, 0);

                        if (endMarkerPrefab != null)
                        {
                            var marker = Instantiate(endMarkerPrefab, pos, Quaternion.identity, transform);
                            marker.name = $"EndMarker_{x}_{y}";
                        }
                    }
                }
            }
        }

        private bool IsSpawn(int x, int y)
        {
            foreach (var p in spawnPositions)
                if (p.x == x && p.y == y) return true;
            return false;
        }

        private bool IsEnd(int x, int y)
        {
            foreach (var p in endPositions)
                if (p.x == x && p.y == y) return true;
            return false;
        }

        public Tile GetTile(int x, int y)
        {
            if (_grid == null) return null;
            if (x < 0 || x >= columns || y < 0 || y >= rows) return null;
            return _grid[x, y];
        }

        public Tile[,] GetGrid() => _grid;

        public Vector3 GridToWorld(int x, int y)
        {
            float step    = tileSize + tileGap;
            float offsetX = -(columns - 1) * step * 0.5f;
            float offsetY = -(rows    - 1) * step * 0.5f;
            return new Vector3(offsetX + x * step, offsetY + y * step, 0f);
        }

/// <summary>드래그 시작 시 호출 - 전체 타일 표시</summary>
        public void ShowAllTiles()
        {
            if (_grid == null) return;
            foreach (var t in _grid) t?.ShowForPlacement();
        }

        /// <summary>드래그 끝날 때 호출 - 전체 타일 숨김</summary>
        public void HideAllTiles()
        {
            if (_grid == null) return;
            foreach (var t in _grid) t?.HideForPlacement();
        }

        public bool IsBlocked(int x, int y)
        { 
            Tile t = GetTile(x, y); 
            if (t == null) return true;
            
            if (t.tileType == TileType.EndPoint)  return false; 
            if (t.tileType == TileType.SpawnPoint) return false;
            if (t.passableOverride) return false; 
            if (t.placedTurret == null) return false; 
            return !t.placedTurret.IsPassable;
        }
    

/// <summary>MapData의 wallPlacements를 모두 1x1 Wall로 설치. GameSetup 완료 후 호출.</summary>
public void ApplyWallsFromMapData() 
        { 
            if (mapData == null || mapData.wallPlacements == null || mapData.wallPlacements.Count == 0) 
                return;
            var tm = TurretManager.Instance; 
            var inv = InventoryManager.Instance; 
            if (tm == null || inv == null)
            { 
                Debug.LogWarning("[MapManager] TurretManager or InventoryManager not found!");
                return; 
            } foreach
                (var w in mapData.wallPlacements)
            { var tile = GetTile(w.gridX, w.gridY); 
                if (tile == null) continue;
                TurretType tt = WallSizeToTurretType(w.wallSize); 
                tm.PlaceFromMapData(tile, tt);
                if (tile.placedTurret != null) tile.placedTurret.isFromMapData = true; 
            } 
            Debug.Log($"[MapManager] Applied {mapData.wallPlacements.Count} wall placements from MapData."); 
        } 
        private TurretType WallSizeToTurretType(MapData.WallSizeType wallSize) 
        { 
            switch (wallSize) 
            { 
                case MapData.WallSizeType.Wall2x1: 
                    return TurretType.Wall2x1;
                case MapData.WallSizeType.Wall1x2:
                    return TurretType.Wall1x2; 

                case MapData.WallSizeType.Wall2x2:
                    return TurretType.Wall2x2; 
                default: return TurretType.Wall; 
            } 
        }

        private void ValidateAndClampPositions() 
        { 
            var validSpawns = new System.Collections.Generic.List<Vector2Int>();
            foreach (var p in spawnPositions) 
            {
                if (p.x >= 0 && p.x < columns && p.y >= 0 && p.y < rows) validSpawns.Add(p);
                else Debug.LogWarning($"[MapManager] Spawn ({p.x},{p.y}) is out of range! Map is {columns}x{rows}. Ignoring.");
            }
            var validEnds = new System.Collections.Generic.List<Vector2Int>(); 
            foreach (var p in endPositions) 
            { 
                if (p.x >= 0 && p.x < columns && p.y >= 0 && p.y < rows) validEnds.Add(p);
                else Debug.LogWarning($"[MapManager] End ({p.x},{p.y}) is out of range! Map is {columns}x{rows}. Ignoring.");
            }
            if (validSpawns.Count == 0)
            { validSpawns.Add(new Vector2Int(columns / 2, rows - 1)); 
                Debug.LogWarning($"[MapManager] No valid spawns! Using default: ({columns/2},{rows-1})");
            }
            
            if (validEnds.Count == 0)
            { 
                validEnds.Add(new Vector2Int(columns / 2, 0));
                Debug.LogWarning($"[MapManager] No valid ends! Using default: ({columns/2},0)"); 
            }
            spawnPositions = validSpawns; endPositions = validEnds;
        }
}
}
