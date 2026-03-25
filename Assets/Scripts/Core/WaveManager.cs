using UnityEngine;
using UnityEngine.Events;

namespace Underdark
{
    /// <summary>
    /// 웨이브 순환 관리. 웨이브 완료 → 준비단계 → 다음 웨이브.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Settings")]
        public int totalWaves = 10;
        public float prepTime = 5f; // 준비 시간(초)

        public int CurrentWave { get; private set; } = 0;
        public float PrepTimer  { get; private set; } = 0f;
        public bool  IsPreparing => GameManager.Instance.CurrentState == GameState.Preparation;

        public UnityEvent<int>   onWaveStart;
        public UnityEvent<int>   onWaveComplete;
        public UnityEvent        onAllWavesComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// UI에서 "웨이브 시작" 버튼 클릭 시 호출
        /// </summary>
        public void StartNextWave()
        {
            if (GameManager.Instance.CurrentState != GameState.Preparation) return;
            if (CurrentWave >= totalWaves)
            {
                GameManager.Instance.TriggerVictory();
                return;
            }

            GameManager.Instance.SetState(GameState.WaveInProgress);
            onWaveStart?.Invoke(CurrentWave);
            MonsterManager.Instance.StartWave(CurrentWave);
            UIManager.Instance.ShowWaveUI(CurrentWave + 1);
        }

        /// <summary>
        /// MonsterManager가 웨이브 몬스터 전멸 시 호출
        /// </summary>
        public void OnWaveComplete()
        {
            onWaveComplete?.Invoke(CurrentWave);
            CurrentWave++;

            if (CurrentWave >= totalWaves)
            {
                GameManager.Instance.TriggerVictory();
                return;
            }

            GameManager.Instance.SetState(GameState.Preparation);
            GameManager.Instance.OnWaveCleared();
            UIManager.Instance.ShowPrepUI(CurrentWave + 1);
        }

        public void Reset()
        {
            CurrentWave = 0;
            PrepTimer   = 0f;
        }
    }
}
