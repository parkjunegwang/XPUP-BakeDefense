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
        private void SetupMonsterReferences(GameObject go, Monster m)
        {
            if (m.bodyRenderer == null)
                m.bodyRenderer = go.GetComponent<SpriteRenderer>()
                               ?? go.GetComponentInChildren<SpriteRenderer>();

            if (m.hpBarFill == null)
            {
                var fillTf = FindChildByName(go.transform, "HpFill")
                          ?? FindChildByName(go.transform, "HPFill")
                          ?? FindChildByName(go.transform, "hp_fill");
                if (fillTf != null) m.hpBarFill = fillTf.GetComponent<SpriteRenderer>();
            }
        }

        // ── 경로 재계산 ───────────────────────────────────────────────
        public void RequestPathRecalc() => RecalcAllPaths();

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
        public void StartWave(int waveIndex)
        {
            bool isBoss = IsBossWave(waveIndex);
            var  stat   = isBoss
                        ? (bossStats != null ? bossStats : GetStatForWave(waveIndex))
                        : GetStatForWave(waveIndex);

            int count        = isBoss ? 1 : (stat != null ? stat.GetCount(waveIndex) : 8 + waveIndex * 2);
            _monstersToSpawn = count;
            _monstersAlive   = count;
            _waveFinished    = false;

            RecalcAllPaths();
            if (isBoss) Debug.Log($"[MonsterManager] BOSS WAVE {waveIndex + 1}!");
            StartCoroutine(SpawnRoutine(waveIndex, stat, isBoss));
        }

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
        public void OnMonsterDied(Monster m)
        {
            ActiveMonsters.Remove(m);
            _monstersAlive--;
            if (_monstersAlive <= 0 && !_waveFinished)
            {
                _waveFinished = true;
                WaveManager.Instance.OnWaveComplete();
            }
        }

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
