using UnityEngine;
using UnityEngine.SceneManagement;

namespace Underdark
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("XP Settings")]
        public int xpPerWave   = 30;
        public int xpPerKill   = 5;
        public int xpToLevelUp = 100;

        [Header("Start Inventory")]
        public TurretType[] startTurretPool = {
            TurretType.RangedTurret, TurretType.MeleeTurret,
            TurretType.SpikeTrap,   TurretType.Wall
        };
        public int startTurretCount = 2;

        public GameState CurrentState { get; private set; } = GameState.Preparation;
        public int XP    { get; private set; }
        public int Level { get; private set; } = 1;
        public int XPToNext => xpToLevelUp;

        // 카드 선택 중 중복 레벨업 방지
        private bool _pendingLevelUp = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            XP = 0; Level = 1;
            SetState(GameState.Preparation);
            UIManager.Instance?.RefreshXP(XP, xpToLevelUp, Level);
        }

        public void GiveStartTurrets()
        {
            if (startTurretPool == null || startTurretPool.Length == 0) return;
            for (int i = 0; i < startTurretCount; i++)
            {
                var t = startTurretPool[Random.Range(0, startTurretPool.Length)];
                InventoryManager.Instance?.Add(t, 1);
            }
        }

        public void SetState(GameState state)
        {
            CurrentState = state;
            Debug.Log($"[GameManager] State → {state}");
        }

        // 골드 호환 shim
        public bool SpendGold(int _) => true;
        public void AddGold(int _) { }
        public int Gold => 0;

        // ── XP / 레벨업 ───────────────────────────────────────────────
        public void AddXP(int amount)
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.Victory) return;

            XP += amount;
            UIManager.Instance?.RefreshXP(XP, xpToLevelUp, Level);

            // 카드 선택 중이면 큐잉 (콜백에서 처리)
            if (_pendingLevelUp) return;

            if (XP >= xpToLevelUp)
            {
                XP -= xpToLevelUp;
                Level++;
                _pendingLevelUp = true;
                OnLevelUp();
            }
        }

        private void OnLevelUp()
        {
            Debug.Log($"[GameManager] LevelUp → Lv.{Level}");
            UIManager.Instance?.ShowMessage($"Level {Level}! Choose a card!");
            UIManager.Instance?.RefreshXP(XP, xpToLevelUp, Level);

            if (CardManager.Instance == null)
            {
                Debug.LogWarning("[GameManager] CardManager not found!");
                _pendingLevelUp = false;
                return;
            }

            if (CardManager.Instance.cardPool == null || CardManager.Instance.cardPool.Count == 0)
            {
                Debug.LogWarning("[GameManager] Card pool is empty! Check CardManager in Inspector.");
                _pendingLevelUp = false;
                return;
            }

            CardManager.Instance.ShowCards(() =>
            {
                _pendingLevelUp = false;
                UIManager.Instance?.RefreshXP(XP, xpToLevelUp, Level);

                // 카드 선택 도중 쌓인 XP가 또 레벨업 조건이면 연속 처리
                if (XP >= xpToLevelUp)
                {
                    XP -= xpToLevelUp;
                    Level++;
                    _pendingLevelUp = true;
                    OnLevelUp();
                }
            });
        }

        public void OnWaveCleared() => AddXP(xpPerWave);
        public void OnMonsterKilled(int reward = -1) => AddXP(reward >= 0 ? reward : xpPerKill);

        public void TriggerGameOver()
        {
            if (CurrentState == GameState.GameOver) return;
            SetState(GameState.GameOver);
            MonsterManager.Instance.ClearAll();
            UIManager.Instance?.ShowGameOver();
        }

public void TriggerVictory() { SetState(GameState.Victory); if (CardManager.Instance != null && CardManager.Instance.cardPool?.Count > 0) CardManager.Instance.ShowCards(() => UIManager.Instance?.ShowVictory()); else UIManager.Instance?.ShowVictory(); }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
