using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class TurretManager : MonoBehaviour
    {
        public static TurretManager Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject rangedTurretPrefab;
        public GameObject meleeTurretPrefab;
        public GameObject spikeTrapPrefab;
        public GameObject electricGatePrefab;
        public GameObject wallPrefab;
        public GameObject wall2x1Prefab;
        public GameObject wall1x2Prefab;
        public GameObject wall2x2Prefab;
        public GameObject areaDamagePrefab;
        public GameObject explosiveCannonPrefab;
        public GameObject slowShooterPrefab;
        public GameObject rapidFirePrefab;
        public GameObject tornadoPrefab;
        public GameObject lavaRainPrefab;
        public GameObject chainLightningPrefab;
        public GameObject blackHolePrefab;
        public GameObject precisionStrikePrefab;
        public GameObject gambleBatPrefab;
        public GameObject pulseSlowerPrefab;
        public GameObject dragonStatuePrefab;
        public GameObject hasteTowerPrefab;
        public GameObject pinballCannonPrefab;
        public GameObject boomerangTurretPrefab;
        public GameObject projectilePrefab;

        [Header("Turret Definitions")]
        public List<TurretDef> turretDefs = new List<TurretDef>();

        private List<TurretBase> _all = new List<TurretBase>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (turretDefs.Count == 0) InitDefaultDefs();
        }

private void InitDefaultDefs() { turretDefs.Add(new TurretDef { type=TurretType.RangedTurret,    sizeX=1,sizeY=1,cost=15,color=new Color(0.3f,0.6f,1f),   label="Ranged",     emoji="Gun",  isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.MeleeTurret,     sizeX=1,sizeY=1,cost=12,color=new Color(0.9f,0.7f,0.2f), label="Melee",      emoji="Sword",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.SpikeTrap,       sizeX=2,sizeY=1,cost=10,color=new Color(0.4f,0.35f,0.3f),label="Spikes",     emoji="Trap", isPassable=true  }); turretDefs.Add(new TurretDef { type=TurretType.ElectricGate,    sizeX=3,sizeY=1,cost=20,color=new Color(0.9f,0.8f,0.1f), label="Elec Gate",  emoji="Elec", isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.Wall,            sizeX=1,sizeY=1,cost=5, color=new Color(0.55f,0.45f,0.35f),label="Wall 1x1",emoji="Wall",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.Wall2x1,         sizeX=2,sizeY=1,cost=8, color=new Color(0.50f,0.40f,0.30f),label="Wall 2x1",emoji="Wall",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.Wall1x2,         sizeX=1,sizeY=2,cost=8, color=new Color(0.50f,0.40f,0.30f),label="Wall 1x2",emoji="Wall",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.Wall2x2,         sizeX=2,sizeY=2,cost=12,color=new Color(0.45f,0.35f,0.25f),label="Wall 2x2",emoji="Wall",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.AreaDamage,      sizeX=1,sizeY=1,cost=18,color=new Color(0.7f,0.2f,0.85f), label="Area Dmg",   emoji="Area",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.ExplosiveCannon, sizeX=1,sizeY=1,cost=22,color=new Color(1f,0.4f,0.1f),   label="Cannon",     emoji="Bomb",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.SlowShooter,     sizeX=1,sizeY=1,cost=16,color=new Color(0.3f,0.6f,1f),   label="Slow",       emoji="Ice", isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.RapidFire,       sizeX=1,sizeY=1,cost=14,color=new Color(1f,0.85f,0.2f),  label="Rapid",      emoji="Fast",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.Tornado,         sizeX=1,sizeY=1,cost=25,color=new Color(0.5f,0.85f,1f),  label="Tornado",    emoji="Wind",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.LavaRain,        sizeX=1,sizeY=1,cost=20,color=new Color(1f,0.3f,0f),     label="Lava Rain",  emoji="Lava",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.ChainLightning,  sizeX=1,sizeY=1,cost=24,color=new Color(0.4f,0.8f,1f),   label="Chain Bolt", emoji="Bolt",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.BlackHole,       sizeX=1,sizeY=1,cost=30,color=new Color(0.5f,0f,0.8f),   label="Black Hole", emoji="Hole",isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.PrecisionStrike, sizeX=1,sizeY=1,cost=20,color=new Color(1f,0.95f,0.2f),  label="Precision",  emoji="Aim", isPassable=false }); turretDefs.Add(new TurretDef { type=TurretType.GambleBat,        sizeX=1,sizeY=1,cost=18,color=new Color(0.9f,0.3f,0.9f), label="Gamble Bat",   emoji="Bat",  isPassable=false });
        turretDefs.Add(new TurretDef { type=TurretType.PulseSlower,      sizeX=1,sizeY=1,cost=20,color=new Color(0.4f,0.8f,1f),   label="Pulse Slow",   emoji="Pulse",isPassable=false });
        turretDefs.Add(new TurretDef { type=TurretType.DragonStatue,     sizeX=1,sizeY=1,cost=28,color=new Color(1f,0.45f,0.1f),  label="Dragon",       emoji="Fire", isPassable=false });
        turretDefs.Add(new TurretDef { type=TurretType.HasteTower,       sizeX=1,sizeY=1,cost=22,color=new Color(0.9f,1f,0.3f),   label="Haste",        emoji="Haste",isPassable=false });
        turretDefs.Add(new TurretDef { type=TurretType.PinballCannon,    sizeX=1,sizeY=1,cost=24,color=new Color(1f,0.85f,0.1f),  label="Pinball",      emoji="Ball", isPassable=false });
        turretDefs.Add(new TurretDef { type=TurretType.BoomerangTurret,  sizeX=1,sizeY=1,cost=20,color=new Color(0.5f,1f,0.3f),   label="Boomerang",    emoji="Boom", isPassable=false });
    }

        public TurretDef GetDef(TurretType type)
        {
            foreach (var d in turretDefs)
                if (d.type == type) return d;
            return new TurretDef { type=type, sizeX=1, sizeY=1, cost=10 };
        }

        // ── 모양에 따른 타일 목록 ─────────────────────────────────────
        /// <summary>
        /// statData.GetShape()로 커스텀 모양을 지원.
        /// statData가 없으면 TurretDef의 sizeX/sizeY 직사각형 사용.
        /// </summary>
        public List<Tile> GetShapeTiles(Tile origin, TurretType type, TurretStatData statData = null)
        {
            var def = GetDef(type);
            Vector2Int[] offsets;

            if (statData != null)
                offsets = statData.GetShape(def.sizeX, def.sizeY);
            else
            {
                // fallback 직사각형
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
                if (t == null) return null; // 범위 밖
                tiles.Add(t);
            }
            return tiles;
        }

        // 하위 호환: sizeX/sizeY 직사각형
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
private bool ShouldBlockTile(TurretType type, int idx, int total, TurretStatData statData) { if (statData != null && statData.HasCustomShape()) return !statData.IsTilePassable(idx); switch (type) { case TurretType.SpikeTrap: return false; case TurretType.ElectricGate: return (idx == 0 || idx == total - 1); default: return true; } }

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
                    tiles[i].placedTurret    = tg.AddComponent<WallTurret>();
                    tiles[i].passableOverride = false;
                    tempObjects.Add(tg);
                }
                else
                {
                    tiles[i].placedTurret    = null;
                    tiles[i].passableOverride = true;
                }
            }

            bool ok = CheckAllPathsExist();

            foreach (var tg in tempObjects) DestroyImmediate(tg);
            foreach (var t in tiles)
            {
                t.placedTurret    = savedTurrets[t];
                t.passableOverride = savedPassable[t];
            }
            return ok;
        }

        // ── 설치 가능 여부 ────────────────────────────────────────────
        public TurretStatData GetStatData(TurretType type) { var prefab = GetPrefab(type); var sd = prefab != null ? prefab.GetComponent<TurretBase>()?.statData : null; return sd; }

public bool CanPlaceAt(Tile tile, TurretType type) { var sd = GetStatData(type); var tiles = GetShapeTiles(tile, type, sd); if (tiles == null) return false; foreach (var t in tiles) if (!t.IsPlaceable()) return false; return SimulatePlace(tiles, type, sd); }

        // ── 이동 가능 여부 ────────────────────────────────────────────
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

        // ── 설치 ──────────────────────────────────────────────────────
        /// <summary>MapData 전용 - 인벤토리 거치지 않고 직접 설치 (실패 시 두지 않음)</summary>
        public void PlaceFromMapData(Tile tile, TurretType type)
        {
            var def    = GetDef(type);
            var prefab = GetPrefab(type);
            var sd     = prefab != null ? prefab.GetComponent<TurretBase>()?.statData : null;
            var tiles  = GetShapeTiles(tile, type, sd);

            if (tiles == null) return;
            foreach (var t in tiles)
                if (!t.IsPlaceable()) return;
            // MapData 벽은 경로 차단 체크 스킵 (이미 디자이너가 설계한 맵이뭐로)

            if (prefab == null) return;

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
                else go.transform.localScale = new Vector3(scaleX * 0.92f, scaleY * 0.92f, 1f);
            }

            TurretBase turret = go.GetComponent<TurretBase>();
            turret.turretType  = type;
            turret.currentTile = tile;
            turret.occupiedTiles.Clear();
            turret.occupiedTiles.AddRange(tiles);
            AssignTiles(turret, tiles, type, sd);
            _all.Add(turret);
            turret.UpdateSortingOrder();
        }

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
            // 인벤토리에서 설치권 차감
            if (!InventoryManager.Instance.Consume(type)) { UIManager.Instance?.ShowMessage("No stock! Get from cards."); return; }

            if (prefab == null) { Debug.LogWarning($"No prefab: {type}"); return; }

            bool was = prefab.activeSelf;
            prefab.SetActive(true);
            Vector3 center = GetTilesCenter(tiles);
            GameObject go  = Instantiate(prefab, center, Quaternion.identity);
            prefab.SetActive(was);

            // AutoScale (statData 기준)
            bool shouldAutoScale = sd != null ? sd.autoScale : IsWallType(type);
            if (shouldAutoScale)
            {
                float step   = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
                // 커스텀 모양이면 bounding box 크기로 계산
                int bW = 1, bH = 1;
                if (sd != null && sd.HasCustomShape() && sd.tileShape != null && sd.tileShape.Length > 0)
                {
                    foreach (var o in sd.tileShape)
                    {
                        bW = Mathf.Max(bW, o.x + 1);
                        bH = Mathf.Max(bH, o.y + 1);
                    }
                }
                else { bW = def.sizeX; bH = def.sizeY; }

                float scaleX = bW * step - MapManager.Instance.tileGap;
                float scaleY = bH * step - MapManager.Instance.tileGap;

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    float ppu    = sr.sprite.pixelsPerUnit;
                    float sprW   = sr.sprite.rect.width  / ppu;
                    float sprH   = sr.sprite.rect.height / ppu;
                    go.transform.localScale = new Vector3(scaleX / sprW * 0.92f, scaleY / sprH * 0.92f, 1f);
                }
                else
                {
                    go.transform.localScale = new Vector3(scaleX * 0.92f, scaleY * 0.92f, 1f);
                }
            }

            TurretBase turret = go.GetComponent<TurretBase>();
            turret.turretType  = type; // Wall2x1/1x2/2x2도 WallTurret 공유이므로 직접 주입
            turret.currentTile = tile;
            turret.occupiedTiles.Clear();
            turret.occupiedTiles.AddRange(tiles);

            AssignTiles(turret, tiles, type, sd);
            _all.Add(turret);

            if (turret is RangedTurret rt && rt.projectilePrefab == null && projectilePrefab != null)
                rt.projectilePrefab = projectilePrefab;
            if (turret is RapidFireTurret rft && rft.projectilePrefab == null && projectilePrefab != null)
                rft.projectilePrefab = projectilePrefab;

            MonsterManager.Instance.RequestPathRecalc();
            turret.UpdateSortingOrder();
        }

        // ── 타일 배정 ─────────────────────────────────────────────────
        private void AssignTiles(TurretBase turret, List<Tile> tiles, TurretType type, TurretStatData sd = null)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].placedTurret    = turret;
                tiles[i].passableOverride = !ShouldBlockTile(type, i, tiles.Count, sd);
            }
        }

        private void ClearTileAssignments(TurretBase turret)
        {
            foreach (var t in turret.occupiedTiles)
            {
                if (t == null) continue;
                t.placedTurret    = null;
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
public void TryMerge(Tile from, Tile to) { TurretBase tFrom = from.placedTurret; TurretBase tTo   = to.placedTurret; if (tFrom == null || tTo == null) return; if (IsWallType(tFrom.turretType) || IsWallType(tTo.turretType)) { UIManager.Instance?.ShowMessage("Walls cannot be merged!"); return; } if (tFrom.turretType == tTo.turretType && tFrom.level == tTo.level) { ClearTileAssignments(tFrom); _all.Remove(tFrom); Destroy(tFrom.gameObject); tTo.LevelUp(); StartCoroutine(MergePulse(tTo)); UIManager.Instance?.ShowMessage($"Level Up! Lv.{tTo.level}"); MonsterManager.Instance.RequestPathRecalc(); } else UIManager.Instance?.ShowMessage("Merge same type & level only!"); } private bool IsWallType(TurretType t) => t == TurretType.Wall || t == TurretType.Wall2x1 || t == TurretType.Wall1x2 || t == TurretType.Wall2x2;

        private IEnumerator MergePulse(TurretBase t)
        {
            Vector3 orig = t.transform.localScale;
            t.transform.localScale = orig * 1.45f;
            yield return new WaitForSeconds(0.15f);
            t.transform.localScale = orig;
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────
        private Vector3 GetTilesCenter(List<Tile> tiles)
        {
            Vector3 sum = Vector3.zero;
            foreach (var t in tiles) sum += t.transform.position;
            return sum / tiles.Count;
        }

public GameObject GetPrefabPublic(TurretType type) => GetPrefab(type);
private GameObject GetPrefab(TurretType type) => type switch { TurretType.RangedTurret => rangedTurretPrefab, TurretType.MeleeTurret => meleeTurretPrefab, TurretType.SpikeTrap => spikeTrapPrefab, TurretType.ElectricGate => electricGatePrefab, TurretType.Wall => wallPrefab, TurretType.Wall2x1 => wall2x1Prefab != null ? wall2x1Prefab : wallPrefab, TurretType.Wall1x2 => wall1x2Prefab != null ? wall1x2Prefab : wallPrefab, TurretType.Wall2x2 => wall2x2Prefab != null ? wall2x2Prefab : wallPrefab, TurretType.AreaDamage => areaDamagePrefab, TurretType.ExplosiveCannon => explosiveCannonPrefab, TurretType.SlowShooter => slowShooterPrefab, TurretType.RapidFire => rapidFirePrefab, TurretType.Tornado => tornadoPrefab, TurretType.LavaRain => lavaRainPrefab, TurretType.ChainLightning => chainLightningPrefab, TurretType.BlackHole => blackHolePrefab, TurretType.PrecisionStrike => precisionStrikePrefab, TurretType.GambleBat        => gambleBatPrefab,
        TurretType.PulseSlower      => pulseSlowerPrefab,
        TurretType.DragonStatue     => dragonStatuePrefab,
        TurretType.HasteTower       => hasteTowerPrefab,
        TurretType.PinballCannon    => pinballCannonPrefab,
        TurretType.BoomerangTurret  => boomerangTurretPrefab,
        _ => null };

        public List<TurretBase> GetAll() => _all;

        /// <summary>같은 타입+레벨 타워 자동 머지 (카드 효과)</summary>
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
                    // a 제거, b 레벨업
                    ClearTileAssignments(a);
                    _all.Remove(a);
                    Destroy(a.gameObject);
                    b.LevelUp();
                    MonsterManager.Instance?.RequestPathRecalc();
                    merged = true;
                    break;
                }
                if (merged) break; // 한 번에 하나씩
            }
        }
    }
}
