using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 터렛 사정거리 표시기.
    /// - 원형  : ShowCircle()   - 일반 원거리 터렛
    /// - 타일  : ShowTiles()    - SpikeTrap 등 고정 타일 기반
    /// - 방향선: ShowDirection() - MeleeTurret (방향 + 범위)
    /// </summary>
    public class RangeIndicator : MonoBehaviour
    {
        public static RangeIndicator Instance { get; private set; }

        // ── LineRenderer (원 / 방향선 공용) ──────────────────────────
        private LineRenderer _lr;
        private const int    CircleSegs = 64;

        // ── 타일 하이라이트 (SpikeTrap 등) ───────────────────────────
        private readonly List<SpriteRenderer> _tileHighlights = new List<SpriteRenderer>();
        private GameObject _tileRoot;

        private float _timer    = 0f;
        private float _fadeTime = 0f;

        // ── 정렬 순서 : 항상 최상위 ──────────────────────────────────
        private const int TopOrder = 9998; // Projectile(9999) 바로 아래

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // LineRenderer 설정
            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.useWorldSpace    = true;
            _lr.loop             = false;
            _lr.positionCount    = 0;
            _lr.startWidth       = 0.055f;
            _lr.endWidth         = 0.055f;
            _lr.sortingLayerName = "Default";
            _lr.sortingOrder     = TopOrder;
            _lr.material         = new Material(Shader.Find("Sprites/Default"));

            // 타일 하이라이트 루트
            _tileRoot = new GameObject("TileHighlights");
            _tileRoot.transform.SetParent(transform);

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_fadeTime > 0f)
            {
                _timer += Time.deltaTime;
                if (_timer >= _fadeTime) Hide();
            }
        }

        // ── 퍼블릭 API ────────────────────────────────────────────────

        /// <summary>원형 사정거리 (일반 터렛 드래그 / 클릭)</summary>
        public void ShowCircle(Vector3 center, float radius, Color col, float duration = 0f)
        {
            ClearTileHighlights();
            gameObject.SetActive(true);
            SetFade(duration);
            DrawCircle(center, radius, col);
        }

        /// <summary>타일 기반 하이라이트 (SpikeTrap, ElectricGate 등)</summary>
        public void ShowTiles(List<Tile> tiles, Color col, float duration = 0f)
        {
            _lr.positionCount = 0;
            gameObject.SetActive(true);
            SetFade(duration);
            ClearTileHighlights();

            float tileSize = MapManager.Instance.tileSize;
            foreach (var t in tiles)
            {
                var go = new GameObject("TH");
                go.transform.SetParent(_tileRoot.transform);
                go.transform.position = t.transform.position;
                go.transform.localScale = Vector3.one * tileSize * 0.92f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = GameSetup.WhiteSquareStatic();
                sr.color        = col;
                sr.sortingOrder = TopOrder;
                _tileHighlights.Add(sr);
            }
        }

        /// <summary>방향 표시 (MeleeTurret - 방향으로 뻗는 선 + 폭 표시)</summary>
        public void ShowDirection(Vector3 origin, Vector2Int dir, float length, Color col, float width = 0.7f, float duration = 0f)
        {
            ClearTileHighlights();
            gameObject.SetActive(true);
            SetFade(duration);

            // 방향 벡터
            Vector3 d = new Vector3(dir.x, dir.y, 0f).normalized;
            Vector3 perp = new Vector3(-d.y, d.x, 0f) * width * 0.5f;
            Vector3 tip  = origin + d * length;

            // 사다리꼴 모양: 시작(좁음) → 끝(넓음)
            _lr.loop          = true;
            _lr.positionCount = 4;
            _lr.startColor    = col;
            _lr.endColor      = col;
            _lr.startWidth    = 0.055f;
            _lr.endWidth      = 0.055f;

            float startW = 0.3f;
            _lr.SetPosition(0, origin + new Vector3(-d.y, d.x, 0f) * startW * 0.5f + new Vector3(0,0,-0.1f));
            _lr.SetPosition(1, tip    + perp + new Vector3(0,0,-0.1f));
            _lr.SetPosition(2, tip    - perp + new Vector3(0,0,-0.1f));
            _lr.SetPosition(3, origin - new Vector3(-d.y, d.x, 0f) * startW * 0.5f + new Vector3(0,0,-0.1f));
        }

/// <summary>좌우 브레스 범위 표시 (DragonStatue용) - 좌우 각각 사각형</summary>
        public void ShowBreathRect(Vector3 center, float rangeX, float rangeY, Color col, float duration = 0f)
        {
            ClearTileHighlights();
            gameObject.SetActive(true);
            SetFade(duration);

            // 좌우 두 개의 사각형을 LineRenderer로 그림
            // 점 순서: 좌사각형 4개 + 연결 + 우사각형 4개
            // left rect: center→(-rangeX, +rangeY/2) corners
            // right rect: center→(+rangeX, +rangeY/2) corners
            float z = -0.1f;
            float hw = rangeY * 0.5f; // half width (세로)

            _lr.loop          = false;
            _lr.positionCount = 10;
            _lr.startColor    = col;
            _lr.endColor      = col;
            _lr.startWidth    = 0.055f;
            _lr.endWidth      = 0.055f;

            // 왼쪽 사각형 (시계방향)
            _lr.SetPosition(0, new Vector3(center.x,          center.y + hw, z));  // 왼 시작
            _lr.SetPosition(1, new Vector3(center.x - rangeX, center.y + hw, z));  // 왼 상단
            _lr.SetPosition(2, new Vector3(center.x - rangeX, center.y - hw, z));  // 왼 하단
            _lr.SetPosition(3, new Vector3(center.x,          center.y - hw, z));  // 왼 끝
            _lr.SetPosition(4, new Vector3(center.x,          center.y + hw, z));  // 닫기
            // 오른쪽 사각형으로 이동
            _lr.SetPosition(5, new Vector3(center.x,          center.y - hw, z));  // 오 시작
            _lr.SetPosition(6, new Vector3(center.x + rangeX, center.y - hw, z));  // 오 하단
            _lr.SetPosition(7, new Vector3(center.x + rangeX, center.y + hw, z));  // 오 상단
            _lr.SetPosition(8, new Vector3(center.x,          center.y + hw, z));  // 오 끝
            _lr.SetPosition(9, new Vector3(center.x,          center.y - hw, z));  // 닫기
        }


        public void Hide()
        {
            _lr.positionCount = 0;
            _lr.loop = false;
            ClearTileHighlights();
            gameObject.SetActive(false);
            _fadeTime = 0f;
            _timer    = 0f;
        }

        // ── 내부 ──────────────────────────────────────────────────────
        private void SetFade(float duration)
        {
            _fadeTime = duration;
            _timer    = 0f;
        }

        private void DrawCircle(Vector3 center, float radius, Color col)
        {
            _lr.loop          = true;
            _lr.positionCount = CircleSegs;
            _lr.startColor    = col;
            _lr.endColor      = col;
            for (int i = 0; i < CircleSegs; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegs;
                _lr.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    -0.1f));
            }
        }

        private void ClearTileHighlights()
        {
            foreach (var sr in _tileHighlights)
                if (sr != null) Destroy(sr.gameObject);
            _tileHighlights.Clear();
        }
    }
}
