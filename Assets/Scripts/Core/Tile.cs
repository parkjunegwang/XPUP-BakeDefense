using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 개별 타일. 포탑 배치 정보만 보관.
    /// 실제 입력은 InputController가 처리.
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [Header("Tile Info")]
        public int gridX;
        public int gridY;
        public TileType tileType = TileType.Empty;

        [Header("Turret")]
        public TurretBase placedTurret;

        /// <summary>포탑과 무관하게 이 타일을 강제 통과 가능으로 설정 (전기기둥 가운데)</summary>
        public bool passableOverride = false;

        [Header("Visual")]
        public SpriteRenderer bgRenderer;

        // 기본 투명 색상 (평소엔 안 보임)
        public static readonly Color ColorHidden   = new Color(0f,    0f,    0f,    0f);
        // 색상 정의
        public static readonly Color ColorEmpty     = new Color(0.18f, 0.18f, 0.28f);
        public static readonly Color ColorWall      = new Color(0.35f, 0.22f, 0.12f);
        public static readonly Color ColorSpawn     = new Color(0.10f, 0.55f, 0.15f);
        public static readonly Color ColorEnd       = new Color(0.65f, 0.10f, 0.10f);
        public static readonly Color ColorHighlight = new Color(0.4f,  0.7f,  1.0f);
        public static readonly Color ColorError     = new Color(0.9f,  0.2f,  0.2f);

        private void Awake()
        {
            bgRenderer = GetComponent<SpriteRenderer>();
            if (bgRenderer != null) bgRenderer.sortingOrder = SLayer.Tile;
        }

        public void Init(int x, int y, TileType type)
        {
            gridX = x; gridY = y;
            SetType(type);
        }

        public void SetType(TileType type)
        {
            tileType = type;
            RefreshColor();
        }

        // 타일 표시 여부 (드래그 중에만 보임)
        private bool _visible = false;

        public void RefreshColor()
        {
            if (bgRenderer == null) return;
            if (!_visible) { bgRenderer.color = ColorHidden; return; }
            switch (tileType)
            {
                case TileType.Empty:      bgRenderer.color = ColorEmpty;  break;
                case TileType.SpawnPoint: bgRenderer.color = ColorSpawn;  break;
                case TileType.EndPoint:   bgRenderer.color = ColorEnd;    break;
            }
        }

        /// <summary>드래그 시작 시 모든 타일 보이게</summary>
        public void ShowForPlacement()
        {
            _visible = true;
            RefreshColor();
        }

        /// <summary>드래그 끝났을 때 타일 숨기기</summary>
        public void HideForPlacement()
        {
            _visible = false;
            RefreshColor();
        }

        public void SetHighlight(Color col) 
        { 
            if (bgRenderer != null) bgRenderer.color = col; 
        }

public bool IsPlaceable() { var state = GameManager.Instance.CurrentState; bool validState = state == GameState.Preparation || state == GameState.WaveInProgress; return tileType == TileType.Empty && placedTurret == null && validState; }

        public bool HasTurret() => placedTurret != null;
    }
}
