using UnityEngine;
using UnityEngine.Events;

namespace Underdark
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Settings")]
        public int   totalWaves = 10;
        public float prepTime   = 5f;

        public int   CurrentWave { get; private set; } = 0;
        public float PrepTimer   { get; private set; } = 0f;
        public bool  IsPreparing => GameManager.Instance.CurrentState == GameState.Preparation;

        public UnityEvent<int> onWaveStart;
        public UnityEvent<int> onWaveComplete;
        public UnityEvent      onAllWavesComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartNextWave()
        {
            if (GameManager.Instance.CurrentState != GameState.Preparation) return;
            if (CurrentWave >= totalWaves) { GameManager.Instance.TriggerVictory(); return; }

            GameManager.Instance.SetState(GameState.WaveInProgress);
            onWaveStart?.Invoke(CurrentWave);
            // StartWave removed - StageManager.StartNextWave() handles WaveData-based spawning
            UIManager.Instance.ShowWaveUI(CurrentWave + 1);
        }

        public void OnWaveComplete()
        {
            onWaveComplete?.Invoke(CurrentWave);
            CurrentWave++;
            CleanupProjectiles();

            if (CurrentWave >= totalWaves) { GameManager.Instance.TriggerVictory(); return; }

            GameManager.Instance.SetState(GameState.Preparation);
            GameManager.Instance.OnWaveCleared();
            UIManager.Instance.ShowPrepUI(CurrentWave + 1);
        }

        /// <summary>
        /// 웨이브 종료 시 씬에 남은 발사체/이펙트 제거.
        /// 컴포넌트 타입 기반으로 정리 (이름 기반 X → 터렛 오브젝트 삭제 방지)
        /// </summary>
        private void CleanupProjectiles()
        {
            foreach (var p in FindObjectsOfType<ExplosiveProjectile>()) if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsOfType<SlowProjectile>())      if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsOfType<TornadoProjectile>())   if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsOfType<LavaPuddle>())          if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsOfType<Projectile>())          if (p != null) Destroy(p.gameObject);
        }

        public void Reset()
        {
            CurrentWave = 0;
            PrepTimer   = 0f;
        }
    }
}
