using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Underdark
{
    /// <summary>
    /// 스테이지 + 웨이브 진행 전체 관리.
    /// WaveManager의 역할을 흡수하고 StageData 기반으로 동작.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Stage List (Inspector 또는 Resources 자동로드)")]
        [Tooltip("모든 스테이지 데이터. 인덱스 순서가 스테이지 번호.")]
        public List<StageData> stages = new List<StageData>();

        [Header("Lobby Scene")]
        public string lobbySceneName = "LobbyScene";

        // ── 런타임 상태 ─────────────────────────────────────────────
        public StageData   CurrentStage  { get; private set; }
        public int         StageIndex    { get; private set; }
        public int         CurrentWave   { get; private set; }   // 0-based
        public int         TotalWaves    => CurrentStage != null ? CurrentStage.TotalWaves : 0;
        public bool        IsPreparing   => GameManager.Instance?.CurrentState == GameState.Preparation;

        // WaveManager 호환용
        public int totalWaves => TotalWaves;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Resources에서 StageData 자동 로드 (Inspector 미할당 시)
            if (stages == null || stages.Count == 0)
                LoadStagesFromResources();
        }

private void Start()
        {
            StageIndex  = SaveData.SelectedStageIndex;
            CurrentWave = 0;

            // stages가 비어있으면 Inspector 연결 안된 것 → 에러 보여주고 안전하게 fallback
            if (stages == null || stages.Count == 0)
            {
                Debug.LogError("[StageManager] stages 리스트가 비어있어요! Inspector에서 StageManager의 Stages 리스트에 StageData를 연결해주세요.");
                return;
            }

            // StageIndex 범위 초과 시 0으로 fallback
            if (StageIndex < 0 || StageIndex >= stages.Count)
            {
                Debug.LogWarning($"[StageManager] StageIndex({StageIndex})가 범위 초과 (stages.Count={stages.Count}). 0으로 fallback.");
                StageIndex = 0;
            }

            CurrentStage = stages[StageIndex];

            // MapData 적용
            if (CurrentStage?.mapData != null)
            {
                var map = MapManager.Instance;
                if (map != null) map.mapData = CurrentStage.mapData;
            }

            Debug.Log($"[StageManager] Stage {StageIndex + 1} / {stages.Count} 로드됨. 총 웨이브: {TotalWaves}");
        }

        private void LoadStagesFromResources()
        {
            var loaded = Resources.LoadAll<StageData>("Stages");
            stages = new List<StageData>(loaded);
            // 이름순 정렬 (Stage1, Stage2...)
            stages.Sort((a, b) => string.Compare(a.stageName, b.stageName));
            Debug.Log($"[StageManager] Resources에서 스테이지 {stages.Count}개 로드");
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
public void OnWaveComplete() { CleanupProjectiles(); int waveJustDone = CurrentWave; CurrentWave++; if (CurrentWave >= TotalWaves) { SaveData.SetCleared(StageIndex, true); GameManager.Instance.TriggerVictory(); return; } GameManager.Instance.SetState(GameState.Preparation); WaveData wd = (CurrentStage != null && waveJustDone < CurrentStage.TotalWaves) ? CurrentStage.waves[waveJustDone] : null; // waveCompleteXp: -1 = xpPerWave 사용, 0 = XP 없음, 1이상 = 직접 입력값
            int xp = (wd != null && wd.waveCompleteXp >= 0) ? wd.waveCompleteXp : GameManager.Instance.xpPerWave; Debug.Log($"[StageManager] Wave {waveJustDone + 1} clear XP: {xp} (override={wd?.waveCompleteXp})"); GameManager.Instance.AddXP(xp); UIManager.Instance?.ShowPrepUI(CurrentWave + 1); }

        // ── 로비로 돌아가기 ─────────────────────────────────────────
        public void GoToLobby()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        // ── 재시작 ──────────────────────────────────────────────────
public void RestartStage()
        {
            // 재시작 전에 현재 세션 터릿 정보를 다시 SaveData에 저장
            // (DoInitialSetup에서 SelectedTurrets를 null로 지웠기 때문에 복원해야 함)
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
