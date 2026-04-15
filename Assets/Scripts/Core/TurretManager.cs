using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class TurretManager : MonoBehaviour
    {
        public static TurretManager Instance { get; private set; }

        [Header("Registry (여기 하나만 연결하면 끝!)")]
        public TurretRegistry registry;

        [Header("공용 프리팹")]
        public GameObject projectilePrefab;

        private List<TurretBase> _all = new List<TurretBase>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (registry == null)
                Debug.LogError("[TurretManager] TurretRegistry가 연결되지 않았습니다! Inspector에서 연결해주세요.");
            else
                registry.RebuildCache();
        }

        // ── Registry 래퍼 ─────────────────────────────────────────────

        public TurretDef GetDef(TurretType type) =>
            registry != null ? registry.GetDef(type)
                             : new TurretDef { type = type, sizeX = 1, sizeY = 1, cost = 10 };

        private GameObject GetPrefab(TurretType type) =>
            registry?.GetPrefab(type);

        public GameObject GetPrefabPublic(TurretType type) => GetPrefab(type);

        public TurretStatData GetStatData(TurretType type)
        {
            var prefab = GetPrefab(type);
            return prefab != null ? prefab.GetComponent<TurretBase>()?.statData : null;
        }

        private bool IsWallType(TurretType t) => registry != null && registry.IsWall(t);

        // ── 모양에 따른 타일 목록 ─────────────────────────────────────

        public List<Tile> GetShapeTiles(Tile origin, TurretType type, TurretStatData statData = null)
        {
            var def = GetDef(type);
            Vector2Int[] offsets;

            if (statData != null)
                offsets = statData.GetShape(def.sizeX, def.sizeY);
            else
            {
                var tmp = new List<Vector2Int>();
                for (int dy = 0; dy < def.sizeY; dy++)
                    for (int dx = 0; dx < def.sizeX; dx++)
                        tmp.Add(new Vector2Int(dx, dy));
                offsets = tmp.ToArray();
            }

            var tiles = new List<Tile>();
            var map   = MapManager.Instance;
            foreach (var offset in offsets)
            {
                var t = map.GetTile(origin.gridX + offset.x, origin.gridY + offset.y);
                if (t == null) return null;
                tiles.Add(t);
            }
            return tiles;
        }

        public List<Tile> GetOccupiedTiles(Tile origin, int sizeX, int sizeY = 1)
        {
            var list = new List<Tile>();
            var map  = MapManager.Instance;
            for (int dy = 0; dy < sizeY; dy++)
                for (int dx = 0; dx < sizeX; dx++)
                {
                    var t = map.GetTile(origin.gridX + dx, origin.gridY + dy);
                    if (t == null) return null;
                    list.Add(t);
                }
            return list;
        }

        // ── 경로 체크 ─────────────────────────────────────────────────

        public bool CheckAllPathsExist()
        {
            var map = MapManager.Instance;
            foreach (var spawn in map.spawnPositions)
                foreach (var end in map.endPositions)
                    if (Pathfinder.FindPath(spawn, end, map) == null) return false;
            return true;
        }

        // ── 타일 인덱스별 차단 여부 ───────────────────────────────────

        private bool ShouldBlockTile(TurretType type, int idx, int total, TurretStatData statData)
        {
            if (statData != null && statData.HasCustomShape())
                return !statData.IsTilePassable(idx);

            switch (type)
            {
                case TurretType.SpikeTrap:    return false;
                case TurretType.ElectricGate: return (idx == 0 || idx == total - 1);
                default:                      return true;
            }
        }

        // ── 시뮬레이션 ────────────────────────────────────────────────

        private bool SimulatePlace(List<Tile> tiles, TurretType type, TurretStatData statData = null)
        {
            var savedTurrets  = new Dictionary<Tile, TurretBase>();
            var savedPassable = new Dictionary<Tile, bool>();
            var tempObjects   = new List<GameObject>();

            for (int i = 0; i < tiles.Count; i++)
            {
                savedTurrets[tiles[i]]  = tiles[i].placedTurret;
                savedPassable[tiles[i]] = tiles[i].passableOverride;
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                if (ShouldBlockTile(type, i, tiles.Count, statData))
                {
                    var tg = new GameObject("__chk__");
                    tiles[i].placedTurret     = tg.AddComponent<WallTurret>();
                    tiles[i].passableOverride = false;
                    tempObjects.Add(tg);
                }
                else
                {
                    tiles[i].placedTurret     = null;
                    tiles[i].passableOverride = true;
                }
            }

            bool ok = CheckAllPathsExist();

            foreach (var tg in tempObjects) DestroyImmediate(tg);
            foreach (var t in tiles)
            {
                t.placedTurret     = savedTurrets[t];
                t.passableOverride = savedPassable[t];
            }
            return ok;
        }

        // ── 설치 가능 여부 ────────────────────────────────────────────

        public bool CanPlaceAt(Tile tile, TurretType type)
        {
            var sd    = GetStatData(type);
            var tiles = GetShapeTiles(tile, type, sd);
            if (tiles == null) return false;
            foreach (var t in tiles) if (!t.IsPlaceable()) return false;
            return SimulatePlace(tiles, type, sd);
        }

        public bool CanMoveAt(TurretBase turret, Tile newOrigin)
        {
            var sd       = turret.statData;
            var newTiles = GetShapeTiles(newOrigin, turret.turretType, sd);
            if (newTiles == null) return false;

            foreach (var t in newTiles)
                if (!turret.occupiedTiles.Contains(t) && !t.IsPlaceable()) return false;

            ClearTileAssignments(turret);
            bool ok = SimulatePlace(newTiles, turret.turretType, sd);
            AssignTiles(turret, turret.occupiedTiles, turret.turretType, sd);
            return ok;
        }

        // ── 설치 (MapData용: 경로 체크 스킵) ─────────────────────────

        public void PlaceFromMapData(Tile tile, TurretType type)
        {
            var def    = GetDef(type);
            var prefab = GetPrefab(type);
            var sd     = prefab != null ? prefab.GetComponent<TurretBase>()?.statData : null;
            var tiles  = GetShapeTiles(tile, type, sd);

            if (tiles == null) return;
            foreach (var t in tiles) if (!t.IsPlaceable()) return;
            if (prefab == null) return;

            var go = SpawnTurretGO(prefab, tiles, def, sd, type);
            if (go == null) return;

            var turret = go.GetComponent<TurretBase>();
            turret.turretType  = type;
            turret.currentTile = tile;
            turret.occupiedTiles.Clear();
            turret.occupiedTiles.AddRange(tiles);
            AssignTiles(turret, tiles, type, sd);
            _all.Add(turret);
            turret.UpdateSortingOrder();
        }

        // ── 설치 (인벤토리 차감 포함) ────────────────────────────────

        public void PlaceSelectedTurret(Tile tile, TurretType type)
        {
            var def    = GetDef(type);
            var prefab = GetPrefab(type);
            var sd     = prefab != null ? prefab.GetComponent<TurretBase>()?.statData : null;
            var tiles  = GetShapeTiles(tile, type, sd);

            if (tiles == null)                             { UIManager.Instance?.ShowMessage("Not enough space!"); return; }
            foreach (var t in tiles)
                if (!t.IsPlaceable())                      { UIManager.Instance?.ShowMessage("Cannot place here!"); return; }
            if (!SimulatePlace(tiles, type, sd))           { UIManager.Instance?.ShowMessage("Path would be blocked!"); return; }
            if (!InventoryManager.Instance.Consume(type))  { UIManager.Instance?.ShowMessage("No stock! Get from cards."); return; }
            if (prefab == null) { Debug.LogWarning($"No prefab for: {type}"); return; }

            var go = SpawnTurretGO(prefab, tiles, def, sd, type);
            if (go == null) return;

            var turret = go.GetComponent<TurretBase>();
            turret.turretType  = type;
            turret.currentTile = tile;
            turret.occupiedTiles.Clear();
            turret.occupiedTiles.AddRange(tiles);
            AssignTiles(turret, tiles, type, sd);
            _all.Add(turret);

            InjectProjectile(turret);

            MonsterManager.Instance.RequestPathRecalc();
            turret.UpdateSortingOrder();
        }

        // ── 공통 스폰 헬퍼 ───────────────────────────────────────────

        private GameObject SpawnTurretGO(GameObject prefab, List<Tile> tiles, TurretDef def, TurretStatData sd, TurretType type)
        {
            bool was = prefab.activeSelf;
            prefab.SetActive(true);
            Vector3 center = GetTilesCenter(tiles);
            GameObject go  = Instantiate(prefab, center, Quaternion.identity);
            prefab.SetActive(was);

            bool shouldAutoScale = sd != null ? sd.autoScale : IsWallType(type);
            if (shouldAutoScale)
            {
                float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
                int bW = def.sizeX, bH = def.sizeY;

                if (sd != null && sd.HasCustomShape() && sd.tileShape != null && sd.tileShape.Length > 0)
                {
                    bW = 1; bH = 1;
                    foreach (var o in sd.tileShape)
                    {
                        bW = Mathf.Max(bW, o.x + 1);
                        bH = Mathf.Max(bH, o.y + 1);
                    }
                }

                float scaleX = bW * step - MapManager.Instance.tileGap;
                float scaleY = bH * step - MapManager.Instance.tileGap;
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    float ppu  = sr.sprite.pixelsPerUnit;
                    float sprW = sr.sprite.rect.width  / ppu;
                    float sprH = sr.sprite.rect.height / ppu;
                    go.transform.localScale = new Vector3(scaleX / sprW * 0.92f, scaleY / sprH * 0.92f, 1f);
                }
                else
                {
                    go.transform.localScale = new Vector3(scaleX * 0.92f, scaleY * 0.92f, 1f);
                }
            }
            return go;
        }

        private void InjectProjectile(TurretBase turret)
        {
            if (projectilePrefab == null) return;
            if (turret is RangedTurret rt && rt.projectilePrefab == null)
                rt.projectilePrefab = projectilePrefab;
            if (turret is RapidFireTurret rft && rft.projectilePrefab == null)
                rft.projectilePrefab = projectilePrefab;
        }

        // ── 타일 배정 ─────────────────────────────────────────────────

        private void AssignTiles(TurretBase turret, List<Tile> tiles, TurretType type, TurretStatData sd = null)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].placedTurret     = turret;
                tiles[i].passableOverride = !ShouldBlockTile(type, i, tiles.Count, sd);
            }
        }

        private void ClearTileAssignments(TurretBase turret)
        {
            foreach (var t in turret.occupiedTiles)
            {
                if (t == null) continue;
                t.placedTurret     = null;
                t.passableOverride = false;
            }
        }

        // ── 이동 ──────────────────────────────────────────────────────

        public void MoveTurret(Tile fromTile, Tile toTile)
        {
            TurretBase turret = fromTile.placedTurret;
            if (turret == null) return;

            var sd       = turret.statData;
            var newTiles = GetShapeTiles(toTile, turret.turretType, sd);

            if (newTiles == null) { UIManager.Instance?.ShowMessage("Not enough space!"); return; }

            foreach (var t in newTiles)
                if (!turret.occupiedTiles.Contains(t) && !t.IsPlaceable())
                    { UIManager.Instance?.ShowMessage("Cannot move here!"); return; }

            ClearTileAssignments(turret);

            if (!SimulatePlace(newTiles, turret.turretType, sd))
            {
                AssignTiles(turret, turret.occupiedTiles, turret.turretType, sd);
                UIManager.Instance?.ShowMessage("Move would block the path!");
                return;
            }

            turret.currentTile = toTile;
            turret.occupiedTiles.Clear();
            turret.occupiedTiles.AddRange(newTiles);
            turret.transform.position = GetTilesCenter(newTiles);
            AssignTiles(turret, newTiles, turret.turretType, sd);

            MonsterManager.Instance.RequestPathRecalc();
        }

        // ── 합치기 ────────────────────────────────────────────────────

        public void TryMerge(Tile from, Tile to)
        {
            TurretBase tFrom = from.placedTurret;
            TurretBase tTo   = to.placedTurret;
            if (tFrom == null || tTo == null) return;
            if (IsWallType(tFrom.turretType) || IsWallType(tTo.turretType))
                { UIManager.Instance?.ShowMessage("Walls cannot be merged!"); return; }

            if (tFrom.turretType == tTo.turretType && tFrom.level == tTo.level)
            {
                ClearTileAssignments(tFrom);
                _all.Remove(tFrom);
                Destroy(tFrom.gameObject);
                tTo.LevelUp();
                StartCoroutine(MergePulse(tTo));
                UIManager.Instance?.ShowMessage($"Level Up! Lv.{tTo.level}");
                MonsterManager.Instance.RequestPathRecalc();
            }
            else UIManager.Instance?.ShowMessage("Merge same type & level only!");
        }

        private IEnumerator MergePulse(TurretBase t)
        {
            Vector3 orig = t.transform.localScale;
            t.transform.localScale = orig * 1.45f;
            yield return new WaitForSeconds(0.15f);
            t.transform.localScale = orig;
        }

        // ── 자동 머지 ─────────────────────────────────────────────────

        public void AutoMergeAll()
        {
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < _all.Count; i++)
                for (int j = i + 1; j < _all.Count; j++)
                {
                    var a = _all[i]; var b = _all[j];
                    if (a == null || b == null) continue;
                    if (a.turretType != b.turretType || a.level != b.level) continue;
                    ClearTileAssignments(a);
                    _all.Remove(a);
                    Destroy(a.gameObject);
                    b.LevelUp();
                    MonsterManager.Instance?.RequestPathRecalc();
                    merged = true;
                    break;
                }
                if (merged) break;
            }
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────

        private Vector3 GetTilesCenter(List<Tile> tiles)
        {
            Vector3 sum = Vector3.zero;
            foreach (var t in tiles) sum += t.transform.position;
            return sum / tiles.Count;
        }

        public List<TurretBase> GetAll() => _all;

        // 하위 호환: turretDefs 리스트 (읽기 전용)
        public IReadOnlyList<TurretDef> turretDefs
        {
            get
            {
                if (registry == null) return new List<TurretDef>();
                var list = new List<TurretDef>();
                foreach (var e in registry.All) list.Add(registry.GetDef(e.type));
                return list;
            }
        }
    }
}
