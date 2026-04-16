using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Underdark
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        public TextMeshProUGUI waveText;
        public TextMeshProUGUI messageText;
        public TextMeshProUGUI levelText;

        [Header("XP Bar")]
        public Image xpBarFill;
        public TextMeshProUGUI xpText;

        [Header("Panels")]
        public GameObject gameOverPanel;
        public GameObject victoryPanel;

        [Header("Buttons")]
        public Button startWaveBtn;
        public Button restartBtn;
        public Button victoryRestartBtn;
        public Button lobbyBtn;          // 게임오버 → 로비
        public Button victoryLobbyBtn;   // 클리어 → 로비
        public Button debugXPBtn;

        private float _messageDuration = 2f;
        private float _messageTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() { }

public void InitButtons()
        { 
            if (startWaveBtn != null) startWaveBtn.onClick.AddListener(() => { if (StageManager.Instance != null) StageManager.Instance.StartNextWave(); else WaveManager.Instance?.StartNextWave(); }); restartBtn?.onClick.AddListener(() => { if (StageManager.Instance != null) StageManager.Instance.RestartStage(); else GameManager.Instance.RestartGame(); }); victoryRestartBtn?.onClick.AddListener(() => { if (StageManager.Instance != null) StageManager.Instance.RestartStage(); else GameManager.Instance.RestartGame(); }); lobbyBtn?.onClick.AddListener(() => StageManager.Instance?.GoToLobby()); victoryLobbyBtn?.onClick.AddListener(() => StageManager.Instance?.GoToLobby()); debugXPBtn?.onClick.AddListener(() => GameManager.Instance?.AddXP(50)); }

        private void Update()
        {
            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.deltaTime;
                if (_messageTimer <= 0f)
                    messageText?.gameObject.SetActive(false);
            }
        }

        // 골드 호환 (빈 메서드)
        public void RefreshGold(int _) { }

        public void RefreshXP(int xp, int maxXP, int level)
        {
            if (levelText != null) levelText.text = $"Lv.{level}";
            if (xpText != null)    xpText.text    = $"{xp}/{maxXP}";
            if (xpBarFill != null)
            {
                float ratio = maxXP > 0 ? (float)xp / maxXP : 0f;
                xpBarFill.fillAmount = ratio;
            }
        }

public void ShowWaveUI(int waveNumber)
        {
            // 보스 여부는 WaveData의 group.isBoss로 관리되므로 여기선 일반 표시
            if (waveText != null) waveText.text = $"Wave {waveNumber}";
            if (waveText != null) waveText.color = Color.white;
            startWaveBtn?.gameObject.SetActive(false);
        }

        public void ShowPrepUI(int nextWave)
        {
            if (waveText != null) waveText.text = $"Ready... (Wave {nextWave})";
            startWaveBtn?.gameObject.SetActive(true);
        }

public void ShowMessage(string msg)
        {
            if (messageText == null) return;
            messageText.text = msg;
            messageText.raycastTarget = false; // 클릭 이벤트 통과
            messageText.gameObject.SetActive(true);
            _messageTimer = _messageDuration;
        }

        public void ShowGameOver()
        {
            gameOverPanel?.SetActive(true);
            startWaveBtn?.gameObject.SetActive(false);
        }

        public void ShowVictory()
        {
            victoryPanel?.SetActive(true);
            startWaveBtn?.gameObject.SetActive(false);
        }
    }
}
