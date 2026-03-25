using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class MonsterManager : MonoBehaviour
    {
        public static MonsterManager Instance { get; private set; }

        [Header("Monster Prefab")]
        public GameObject monsterPrefab;

        [Header("Wave Settings")]
        public float spawnInterval = 0.8f;

        public List<Monster> ActiveMonsters { get; private set; } = new List<Monster>();

        private int  _monstersToSpawn;
        private int  _monstersAlive;
        private bool _waveFinished;

        // 스폰→끝점 경로 캐시
        private Dictionary<Vector2Int, List<Vector2Int>> _pathCache
            = new Dictionary<Vector2Int, List<Vector2Int>>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── 경로 재계산 ───────────────────────────────────────────────
        /// <summary>
        /// 즉시 경로 재계산 (포탑 설치/이동 후 바로 호출)
        /// </summary>
        public void RequestPathRecalc()
        {
            RecalcAllPaths();
        }

        private void RecalcAllPaths()
        {
            _pathCache.Clear();
            var map = MapManager.Instance;
            if (map == null) return;

            foreach (var spawn in map.spawnPositions)
            {
                foreach (var end in map.endPositions)
                {
                    var path = Pathfinder.FindPath(spawn, end, map);
                    if (path != null)
                    {
                        _pathCache[spawn] = path;
                        break; // 스폰당 첫 끝점만
                    }
                }
            }

            // 살아있는 몬스터 경로 즉시 업데이트
            var snapshot = new List<Monster>(ActiveMonsters);
            foreach (var m in snapshot)
            {
                if (m == null || !m.IsAlive) continue;
                Vector2Int nearestSpawn = GetNearestSpawn(m.transform.position);
                if (_pathCache.TryGetValue(nearestSpawn, out var path))
                    m.UpdatePath(path);
            }
        }

        private Vector2Int GetNearestSpawn(Vector3 worldPos)
        {
            var map = MapManager.Instance;
            Vector2Int best  = map.spawnPositions[0];
            float      minD  = float.MaxValue;
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
            int count        = 10;
            _monstersToSpawn = count;
            _monstersAlive   = count;
            _waveFinished    = false;

            // 웨이브 시작 시 경로 최신화
            RecalcAllPaths();
            StartCoroutine(SpawnRoutine(waveIndex));
        }

        private IEnumerator SpawnRoutine(int waveIndex)
        {
            var map        = MapManager.Instance;
            int spawnCount = map.spawnPositions.Count;
            int spawned    = 0;

            while (spawned < _monstersToSpawn)
            {
                Vector2Int spawnGrid = map.spawnPositions[spawned % spawnCount];
                if (_pathCache.TryGetValue(spawnGrid, out var path))
                    SpawnMonster(spawnGrid, path, waveIndex);
                else
                    Debug.LogWarning($"[MonsterManager] 스폰 {spawnGrid}에 경로 없음!");

                spawned++;
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnMonster(Vector2Int spawnGrid, List<Vector2Int> path, int waveIndex)
        {
            if (monsterPrefab == null) { Debug.LogError("[MonsterManager] monsterPrefab null!"); return; }

            bool was = monsterPrefab.activeSelf;
            monsterPrefab.SetActive(true);
            var go = Instantiate(monsterPrefab,
                MapManager.Instance.GridToWorld(spawnGrid.x, spawnGrid.y),
                Quaternion.identity);
            monsterPrefab.SetActive(was);

            var m = go.GetComponent<Monster>();
            m.maxHp  = 10f + waveIndex * 25f;
            m.speed  = 1.8f + waveIndex * 0.15f;
            m.reward = 5 + waveIndex;

            Color col = Color.Lerp(
                new Color(0.2f, 0.8f, 0.2f),
                new Color(0.9f, 0.1f, 0.1f),
                waveIndex / 10f);
            m.Init(path, col);

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
                if (m != null) Destroy(m.gameObject);
            ActiveMonsters.Clear();
        }
    }
}
