using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class MonsterManager : MonoBehaviour
    {
        public static MonsterManager Instance { get; private set; }

        [Header("Fallback Prefab")]
        public GameObject monsterPrefab;

        [Header("Wave Settings")]
        public float spawnInterval     = 0.8f;
        public float bossSpawnInterval = 1.5f;
        [Tooltip("N웨이브마다 보스. 0이면 보스 없음")]
        public int   bossWaveInterval  = 5;

        [Header("Monster Stat Data")]
        [Tooltip("웨이브 진행에 따라 순서대로 사용. 마지막 항목이 이후 웨이브에 계속 사용됨")]
        public MonsterStatData[] waveStats;
        [Tooltip("보스 전용 스탯 데이터")]
        public MonsterStatData   bossStats;

        [Header("Object Pool")]
        [Tooltip("프리팹별 미리 생성할 풀 크기")]
        public int poolSizePerPrefab = 15;

        public List<Monster> ActiveMonsters { get; private set; } = new List<Monster>();

        private int  _monstersToSpawn;
        private int  _monstersAlive;
        private bool _waveFinished;

        private Dictionary<Vector2Int, List<Vector2Int>> _pathCache
            = new Dictionary<Vector2Int, List<Vector2Int>>();

        // ── 오브젝트 풀 ───────────────────────────────────────────────
        // key: prefabName, value: 풀 큐
        private Dictionary<string, Queue<Monster>> _pool
            = new Dictionary<string, Queue<Monster>>();
        private Dictionary<string, GameObject> _prefabCache
            = new Dictionary<string, GameObject>();
        private Transform _poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _poolRoot = new GameObject("MonsterPool").transform;
            _poolRoot.SetParent(transform);
        }

        // ── 프리팹 로드 ───────────────────────────────────────────────
        private GameObject LoadPrefab(string name)
        {
            if (string.IsNullOrEmpty(name)) return monsterPrefab;
            if (_prefabCache.TryGetValue(name, out var cached)) return cached;
            var prefab = Resources.Load<GameObject>($"prefabs/{name}");
            if (prefab == null)
            {
                Debug.LogWarning($"[MonsterManager] prefabs/{name} not found, using fallback.");
                return monsterPrefab;
            }
            _prefabCache[name] = prefab;
            return prefab;
        }

        // ── 풀에서 꺼내기 ─────────────────────────────────────────────
        private Monster GetFromPool(string prefabName)
        {
            if (!_pool.ContainsKey(prefabName))
                _pool[prefabName] = new Queue<Monster>();

            var q = _pool[prefabName];

            // 풀에 비활성화된 게 있으면 재사용
            while (q.Count > 0)
            {
                var m = q.Dequeue();
                if (m != null)
                {
                    m.gameObject.SetActive(true);
                    return m;
                }
            }

            // 없으면 새로 생성
            return CreateNewMonster(prefabName);
        }

        private Monster CreateNewMonster(string prefabName)
        {
            var prefab = LoadPrefab(prefabName);
            if (prefab == null) return null;

            var go = Instantiate(prefab, _poolRoot);
            var m  = go.GetComponent<Monster>() ?? go.AddComponent<Monster>();

            SetupMonsterReferences(go, m);
            go.SetActive(false);
            return m;
        }

        // ── 풀에 반납 ─────────────────────────────────────────────────
        public void ReturnToPool(Monster m, string prefabName)
        {
            if (m == null) return;
            m.gameObject.SetActive(false);
            if (!_pool.ContainsKey(prefabName))
                _pool[prefabName] = new Queue<Monster>();
            _pool[prefabName].Enqueue(m);
        }

        // ── bodyRenderer / hpBarFill 자동 연결 ───────────────────────
private void SetupMonsterReferences(GameObject go, Monster m) { if (m.bodyRenderer == null) m.bodyRenderer = go.GetComponent<SpriteRenderer>() ?? go.GetComponentInChildren<SpriteRenderer>(); if (m.hpBarFill == null) { var fillTf = FindChildByName(go.transform, "HpFill") ?? FindChildByName(go.transform, "HPFill") ?? FindChildByName(go.transform, "hp_fill"); if (fillTf != null) { m.hpBarFill = fillTf.GetComponent<SpriteRenderer>(); } else { BuildHpBar(go, m); } } } private void BuildHpBar(GameObject root, Monster m) { float w = 0.7f; float h = 0.09f; float yOff = 0.62f; var bg = new GameObject("HpBg"); bg.transform.SetParent(root.transform, false); bg.transform.localPosition = new Vector3(0f, yOff, 0f); bg.transform.localScale = new Vector3(w + 0.04f, h + 0.02f, 1f); var bgSr = bg.AddComponent<SpriteRenderer>(); bgSr.sprite = GameSetup.WhiteSquareStatic(); bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.85f); bgSr.sortingOrder = SLayer.MonsterHPBg; var fill = new GameObject("HpFill"); fill.transform.SetParent(bg.transform, false); fill.transform.localPosition = new Vector3(0f, 0f, -0.01f); fill.transform.localScale = new Vector3(1f, 0.85f, 1f); var fillSr = fill.AddComponent<SpriteRenderer>(); fillSr.sprite = GameSetup.WhiteSquareStatic(); fillSr.color = Color.green; fillSr.sortingOrder = SLayer.MonsterHPFill; m.hpBarFill = fillSr; }

        // ── 경로 재계산 ───────────────────────────────────────────────
        public void RequestPathRecalc() => RecalcAllPaths();

public void SlowAllForPlacement(float factor, float duration) { foreach (var m in ActiveMonsters) { if (m == null || !m.IsAlive) continue; m.ApplySlow(factor, duration); } }


        private void RecalcAllPaths()
        {
            _pathCache.Clear();
            var map = MapManager.Instance;
            if (map == null) return;
            foreach (var spawn in map.spawnPositions)
                foreach (var end in map.endPositions)
                {
                    var path = Pathfinder.FindPath(spawn, end, map);
                    if (path != null) { _pathCache[spawn] = path; break; }
                }
            var snapshot = new List<Monster>(ActiveMonsters);
            foreach (var m in snapshot)
            {
                if (m == null || !m.IsAlive) continue;
                var sp = GetNearestSpawn(m.transform.position);
                if (_pathCache.TryGetValue(sp, out var path)) m.UpdatePath(path);
            }
        }

        private Vector2Int GetNearestSpawn(Vector3 worldPos)
        {
            var map  = MapManager.Instance;
            var best = map.spawnPositions[0];
            float minD = float.MaxValue;
            foreach (var s in map.spawnPositions)
            {
                float d = Vector3.Distance(worldPos, map.GridToWorld(s.x, s.y));
                if (d < minD) { minD = d; best = s; }
            }
            return best;
        }

        // ── 웨이브 시작 ───────────────────────────────────────────────
public void StartWave(int waveIndex) { bool isBoss = IsBossWave(waveIndex); var stat = isBoss ? (bossStats != null ? bossStats : GetStatForWave(waveIndex)) : GetStatForWave(waveIndex); int count = isBoss ? 1 : (stat != null ? stat.GetCount(waveIndex) : 8 + waveIndex * 2); _monstersToSpawn = count; _monstersAlive = count; _waveFinished = false; RecalcAllPaths(); if (isBoss) Debug.Log($"[MonsterManager] BOSS WAVE {waveIndex + 1}!"); StartCoroutine(SpawnRoutine(waveIndex, stat, isBoss)); } /// <summary>StageData 기반 웨이브 스폰</summary>
        public void StartWaveFromData(WaveData waveData, int waveIndex) { if (waveData == null || waveData.groups == null || waveData.groups.Count == 0) { StartWave(waveIndex); return; } _waveFinished = false; RecalcAllPaths(); int total = 0; foreach (var g in waveData.groups) total += Mathf.Max(1, g.count); _monstersToSpawn = total; _monstersAlive = total; StartCoroutine(SpawnWaveDataRoutine(waveData, waveIndex)); } private IEnumerator SpawnWaveDataRoutine(WaveData waveData, int waveIndex) { var map = MapManager.Instance; int spawnCnt = map.spawnPositions.Count; int spawnIdx = 0; foreach (var group in waveData.groups) { float interval = group.spawnInterval > 0f ? group.spawnInterval : (group.isBoss ? bossSpawnInterval : spawnInterval); for (int i = 0; i < group.count; i++) { var spawnGrid = map.spawnPositions[spawnIdx % spawnCnt]; spawnIdx++; if (_pathCache.TryGetValue(spawnGrid, out var path)) SpawnMonsterFromGroup(spawnGrid, path, group, waveIndex); else Debug.LogWarning($"[MonsterManager] No path for {spawnGrid}"); yield return new WaitForSeconds(interval); } } } private void SpawnMonsterFromGroup(Vector2Int spawnGrid, List<Vector2Int> path, MonsterSpawnGroup group, int waveIndex) { var stat = group.statData; string prefabName = stat != null ? stat.prefabName : (group.isBoss ? "Boss_0" : "Enemy_0"); var m = GetFromPool(prefabName); if (m == null) { _monstersAlive--; return; } SetupMonsterReferences(m.gameObject, m); if (stat != null) { m.maxHp = stat.GetHp(waveIndex); m.speed = stat.GetSpeed(waveIndex); m.reward = stat.GetReward(waveIndex); if (group.isBoss) { m.maxHp *= stat.bossHpMult; m.speed *= stat.bossSpeedMult; } } else { m.maxHp = group.isBoss ? 100f + waveIndex * 80f : 10f + waveIndex * 20f; m.speed = group.isBoss ? 1.2f : 1.8f; m.reward = group.isBoss ? 30 : 5; } m.transform.localScale = Vector3.one; if (group.isBoss && stat != null && stat.bossSizeScale != 1f) m.transform.localScale *= stat.bossSizeScale; else if (group.isBoss && stat == null) m.transform.localScale *= 1.8f; m.SetPrefabName(prefabName);
            if (group.killXpOverride > 0) m.reward = group.killXpOverride;
            m.transform.position = MapManager.Instance.GridToWorld(spawnGrid.x, spawnGrid.y);
            m.Init(path, Color.white);
            ActiveMonsters.Add(m); }

        private bool IsBossWave(int waveIndex)
            => bossWaveInterval > 0 && (waveIndex + 1) % bossWaveInterval == 0;

        private MonsterStatData GetStatForWave(int waveIndex)
        {
            if (waveStats == null || waveStats.Length == 0) return null;
            return waveStats[Mathf.Min(waveIndex, waveStats.Length - 1)];
        }

        private IEnumerator SpawnRoutine(int waveIndex, MonsterStatData stat, bool isBoss)
        {
            var   map      = MapManager.Instance;
            int   spawnCnt = map.spawnPositions.Count;
            int   spawned  = 0;
            float interval = isBoss ? bossSpawnInterval : spawnInterval;

            while (spawned < _monstersToSpawn)
            {
                var spawnGrid = map.spawnPositions[spawned % spawnCnt];
                if (_pathCache.TryGetValue(spawnGrid, out var path))
                    SpawnMonster(spawnGrid, path, waveIndex, stat, isBoss);
                else
                    Debug.LogWarning($"[MonsterManager] No path for {spawnGrid}");
                spawned++;
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnMonster(Vector2Int spawnGrid, List<Vector2Int> path,
                                   int waveIndex, MonsterStatData stat, bool isBoss)
        {
            string prefabName = stat != null ? stat.prefabName : (isBoss ? "Boss_0" : "Enemy_0");

            // 풀에서 꺼내기
            var m = GetFromPool(prefabName);
            if (m == null) { Debug.LogError("[MonsterManager] No monster from pool!"); return; }

            // 참조 재확인 (풀 재사용 시 누락될 수 있으므로)
            SetupMonsterReferences(m.gameObject, m);

            // 스탯 적용
            if (stat != null)
            {
                m.maxHp  = stat.GetHp(waveIndex);
                m.speed  = stat.GetSpeed(waveIndex);
                m.reward = stat.GetReward(waveIndex);
                if (isBoss)
                {
                    m.maxHp *= stat.bossHpMult;
                    m.speed *= stat.bossSpeedMult;
                }
            }
            else
            {
                m.maxHp  = isBoss ? 100f + waveIndex * 80f : 10f + waveIndex * 20f;
                m.speed  = isBoss ? 1.2f + waveIndex * 0.1f : 1.8f + waveIndex * 0.12f;
                m.reward = isBoss ? 30 + waveIndex * 5 : 5 + waveIndex;
            }

            // 보스 크기 (Init 전에 설정해야 _originalScale에 반영)
            m.transform.localScale = Vector3.one;
            if (isBoss && stat != null && stat.bossSizeScale != 1f)
                m.transform.localScale *= stat.bossSizeScale;
            else if (isBoss && stat == null)
                m.transform.localScale *= 1.8f;

            m.SetPrefabName(prefabName);
            m.transform.position = MapManager.Instance.GridToWorld(spawnGrid.x, spawnGrid.y);
            m.Init(path, Color.white);
            ActiveMonsters.Add(m);
        }

        // ── 몬스터 사망 ───────────────────────────────────────────────
public void OnMonsterDied(Monster m) { ActiveMonsters.Remove(m); _monstersAlive--; if (_monstersAlive <= 0 && !_waveFinished) { _waveFinished = true; if (StageManager.Instance != null) StageManager.Instance.OnWaveComplete(); else WaveManager.Instance?.OnWaveComplete(); } }

        public void ClearAll()
        {
            var snapshot = new List<Monster>(ActiveMonsters);
            foreach (var m in snapshot)
            {
                if (m != null) ReturnToPool(m, m.PrefabName);
            }
            ActiveMonsters.Clear();
        }

        private Transform FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
