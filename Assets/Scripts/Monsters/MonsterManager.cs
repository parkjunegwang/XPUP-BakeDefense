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

        [Header("Object Pool")]
        public int poolSizePerPrefab = 15;

        public List<Monster> ActiveMonsters { get; private set; } = new List<Monster>();

        private int  _monstersToSpawn;
        private int  _monstersAlive;
        private bool _waveFinished;

        private Dictionary<Vector2Int, List<Vector2Int>> _pathCache
            = new Dictionary<Vector2Int, List<Vector2Int>>();

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

        // ── 오브젝트 풀 ───────────────────────────────────────────────
        private Monster GetFromPool(string prefabName)
        {
            if (!_pool.ContainsKey(prefabName))
                _pool[prefabName] = new Queue<Monster>();

            var q = _pool[prefabName];
            while (q.Count > 0)
            {
                var m = q.Dequeue();
                if (m != null) { m.gameObject.SetActive(true); return m; }
            }
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

        public void ReturnToPool(Monster m, string prefabName)
        {
            if (m == null) return;
            m.gameObject.SetActive(false);
            if (!_pool.ContainsKey(prefabName))
                _pool[prefabName] = new Queue<Monster>();
            _pool[prefabName].Enqueue(m);
        }

        // ── HP바 / bodyRenderer 자동 연결 ─────────────────────────────
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
                if (fillTf != null)
                    m.hpBarFill = fillTf.GetComponent<SpriteRenderer>();
                else
                    BuildHpBar(go, m);
            }
        }

        private void BuildHpBar(GameObject root, Monster m)
        {
            float w = 0.7f; float h = 0.09f; float yOff = 0.62f;

            var bg = new GameObject("HpBg");
            bg.transform.SetParent(root.transform, false);
            bg.transform.localPosition = new Vector3(0f, yOff, 0f);
            bg.transform.localScale    = new Vector3(w + 0.04f, h + 0.02f, 1f);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = GameSetup.WhiteSquareStatic();
            bgSr.color  = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            bgSr.sortingOrder = SLayer.MonsterHPBg;

            var fill = new GameObject("HpFill");
            fill.transform.SetParent(bg.transform, false);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fill.transform.localScale    = new Vector3(1f, 0.85f, 1f);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sprite = GameSetup.WhiteSquareStatic();
            fillSr.color  = Color.green;
            fillSr.sortingOrder = SLayer.MonsterHPFill;
            m.hpBarFill = fillSr;
        }

        // ── 경로 재계산 ───────────────────────────────────────────────
        public void RequestPathRecalc() => RecalcAllPaths();

        public void SlowAllForPlacement(float factor, float duration)
        {
            foreach (var m in ActiveMonsters)
            {
                if (m == null || !m.IsAlive) continue;
                m.ApplySlow(factor, duration);
            }
        }

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

        // ── 웨이브 시작 (StageData 기반) ─────────────────────────────
        public void StartWaveFromData(WaveData waveData, int waveIndex)
        {
            if (waveData == null || waveData.groups == null || waveData.groups.Count == 0)
            {
                Debug.LogWarning("[MonsterManager] WaveData 없음!");
                return;
            }

            _waveFinished = false;
            RecalcAllPaths();

            int total = 0;
            foreach (var g in waveData.groups) total += Mathf.Max(1, g.count);
            _monstersToSpawn = total;
            _monstersAlive   = total;

            StartCoroutine(SpawnWaveRoutine(waveData));
        }

        private IEnumerator SpawnWaveRoutine(WaveData waveData)
        {
            var map      = MapManager.Instance;
            int spawnCnt = map.spawnPositions.Count;
            int spawnIdx = 0;

            foreach (var group in waveData.groups)
            {
                float interval = group.spawnInterval > 0f ? group.spawnInterval
                               : (group.isBoss ? bossSpawnInterval : spawnInterval);

                for (int i = 0; i < group.count; i++)
                {
                    var spawnGrid = map.spawnPositions[spawnIdx % spawnCnt];
                    spawnIdx++;

                    if (_pathCache.TryGetValue(spawnGrid, out var path))
                        SpawnMonster(spawnGrid, path, group);
                    else
                        Debug.LogWarning($"[MonsterManager] No path for {spawnGrid}");

                    yield return new WaitForSeconds(interval);
                }
            }
        }

        private void SpawnMonster(Vector2Int spawnGrid, List<Vector2Int> path, MonsterSpawnGroup group)
        {
            // 프리팹: statData에서 가져오고, 없으면 isBoss 여부로 fallback
            string prefabName = group.statData != null ? group.statData.prefabName
                              : (group.isBoss ? "Boss_0" : "Enemy_0");

            var m = GetFromPool(prefabName);
            if (m == null) { _monstersAlive--; return; }

            SetupMonsterReferences(m.gameObject, m);

            // 스탯은 group에서 직접 입력한 값 사용
            m.maxHp  = group.hp;
            m.speed  = group.speed;
            m.reward = group.reward;

            // 보스 HP 배율
            if (group.isBoss)
                m.maxHp *= group.bossHpMult;

            // 크기
            m.transform.localScale = Vector3.one;
            if (group.isBoss)
            {
                float sizeScale = group.statData != null ? group.statData.bossSizeScale : 1.8f;
                m.transform.localScale *= sizeScale;
            }

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
                if (StageManager.Instance != null) StageManager.Instance.OnWaveComplete();
                else WaveManager.Instance?.OnWaveComplete();
            }
        }

        public void ClearAll()
        {
            var snapshot = new List<Monster>(ActiveMonsters);
            foreach (var m in snapshot)
                if (m != null) ReturnToPool(m, m.PrefabName);
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
