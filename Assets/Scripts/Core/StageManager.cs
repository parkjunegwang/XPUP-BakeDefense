using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Underdark
{
    /// <summary>
    /// 스테이지 + 웨이브 진행 전체 관리.
    /// StageRegistry에서 스테이지 목록을 가져와 동작.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Lobby Scene")]
        public string lobbySceneName = "LobbyScene";

        // ── 런타임 상태 ─────────────────────────────────────────────
        public StageData CurrentStage  { get; private set; }
        public int       StageIndex    { get; private set; }
        public int       CurrentWave   { get; private set; }   // 0-based
        public int       TotalWaves    => CurrentStage != null ? CurrentStage.TotalWaves : 0;
        public bool      IsPreparing   => GameManager.Instance?.CurrentState == GameState.Preparation;

        // WaveManager 호환용
        public int totalWaves => TotalWaves;

        private List<StageData> _stages = new List<StageData>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // StageRegistry에서 스테이지 목록 로드
            var registry = Resources.Load<StageRegistry>("StageRegistry");
            if (registry == null)
            {
                Debug.LogError("[StageManager] StageRegistry를 찾을 수 없습니다! Resources/StageRegistry.asset을 확인해주세요.");
                return;
            }

            _stages = registry.ValidStages();
            Debug.Log($"[StageManager] StageRegistry에서 스테이지 {_stages.Count}개 로드");

            StageIndex  = SaveData.SelectedStageIndex;
            CurrentWave = 0;

            // StageIndex 범위 초과 시 0으로 fallback
            if (StageIndex < 0 || StageIndex >= _stages.Count)
            {
                Debug.LogWarning($"[StageManager] StageIndex({StageIndex})가 범위 초과 (_stages.Count={_stages.Count}). 0으로 fallback.");
                StageIndex = 0;
            }

            CurrentStage = _stages[StageIndex];
            if (CurrentStage == null)
            {
                Debug.LogError($"[StageManager] _stages[{StageIndex}]가 null입니다!");
                return;
            }

            // MapData 적용
            if (CurrentStage.mapData != null)
            {
                var map = MapManager.Instance;
                if (map != null) map.mapData = CurrentStage.mapData;
            }

            Debug.Log($"[StageManager] Stage {StageIndex + 1} '{CurrentStage.stageName}' 로드됨. 총 웨이브: {TotalWaves}");
        }

        // ── 웨이브 시작 ─────────────────────────────────────────────
        public void StartNextWave()
        {
            if (GameManager.Instance.CurrentState != GameState.Preparation) return;
            if (CurrentStage == null) { Debug.LogWarning("[StageManager] CurrentStage null!"); return; }
            if (CurrentWave >= TotalWaves) { GameManager.Instance.TriggerVictory(); return; }

            GameManager.Instance.SetState(GameState.WaveInProgress);
            UIManager.Instance?.ShowWaveUI(CurrentWave + 1);

            var waveData = CurrentStage.waves[CurrentWave];
            MonsterManager.Instance.StartWaveFromData(waveData, CurrentWave);
        }

        // ── 웨이브 완료 (MonsterManager에서 호출) ───────────────────
        public void OnWaveComplete()
        {
            CleanupProjectiles();

            int waveJustDone = CurrentWave;
            CurrentWave++;

            if (CurrentWave >= TotalWaves)
            {
                SaveData.SetCleared(StageIndex, true);
                GameManager.Instance.TriggerVictory();
                return;
            }

            GameManager.Instance.SetState(GameState.Preparation);

            // waveCompleteXp: -1 = xpPerWave 사용, 0 = XP 없음, 1이상 = 직접 입력값
            WaveData wd = (CurrentStage != null && waveJustDone < CurrentStage.TotalWaves)
                ? CurrentStage.waves[waveJustDone]
                : null;
            int xp = (wd != null && wd.waveCompleteXp >= 0)
                ? wd.waveCompleteXp
                : GameManager.Instance.xpPerWave;

            Debug.Log($"[StageManager] Wave {waveJustDone + 1} clear XP: {xp} (override={wd?.waveCompleteXp})");
            GameManager.Instance.AddXP(xp);
            UIManager.Instance?.ShowPrepUI(CurrentWave + 1);
        }

        // ── 로비로 돌아가기 ─────────────────────────────────────────
        public void GoToLobby()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        // ── 재시작 ──────────────────────────────────────────────────
        public void RestartStage()
        {
            // 재시작 전에 현재 세션 터릿 정보를 다시 SaveData에 저장
            var sessionTurrets = CardManager.Instance?.SessionTurrets;
            if (sessionTurrets != null && sessionTurrets.Count > 0)
            {
                var arr = new TurretType[sessionTurrets.Count];
                for (int i = 0; i < sessionTurrets.Count; i++)
                    arr[i] = sessionTurrets[i];
                SaveData.SelectedTurrets = arr;
            }

            CurrentWave = 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ── 발사체 정리 ─────────────────────────────────────────────
        private void CleanupProjectiles()
        {
            foreach (var p in FindObjectsOfType<ExplosiveProjectile>()) if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsOfType<SlowProjectile>())      if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsOfType<TornadoProjectile>())   if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsOfType<LavaPuddle>())          if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsOfType<Projectile>())          if (p != null) Destroy(p.gameObject);
        }

        // ── WaveManager 호환 shim ────────────────────────────────────
        public void Reset() { CurrentWave = 0; }
    }
}
