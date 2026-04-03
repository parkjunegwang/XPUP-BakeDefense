using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 터렛 사정거리 표시기.
    /// 드래그 중 / 설치 후 클릭 시 모두 동일한 비주얼로 표시.
    /// - 채워진 반투명 원/사각형 (SpriteRenderer)
    /// - 외곽 실선 (LineRenderer)
    /// </summary>
    public class RangeIndicator : MonoBehaviour
    {
        public static RangeIndicator Instance { get; private set; }

        private const int   CircleSegs = 64;
        private const int   TopOrder   = 9998;

        // ── LineRenderer (외곽선) ─────────────────────────────────────
        private LineRenderer _lr;

        // ── 채워진 원형 fill ──────────────────────────────────────────
        // 원 근사: 고해상도 polygon mesh 대신 큰 SpriteRenderer를 원처럼 보이게
        // → 실제로는 정사각형 sprite를 쓰되 원 outline + 반투명 fill로 표현
        // fill은 outline 색상보다 훨씬 연하게
        private GameObject     _fillGo;
        private SpriteRenderer _fillSr;

        // ── 사각형 fill (DragonStatue 좌우) ───────────────────────────
        private GameObject     _rectGoL;
        private SpriteRenderer _rectSrL;
        private GameObject     _rectGoR;
        private SpriteRenderer _rectSrR;

        // ── 타일 하이라이트 (SpikeTrap 등) ────────────────────────────
        private readonly List<SpriteRenderer> _tileHighlights = new List<SpriteRenderer>();
        private GameObject _tileRoot;

        // ── 페이드 ────────────────────────────────────────────────────
        private float _timer    = 0f;
        private float _fadeTime = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // LineRenderer
            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.useWorldSpace    = true;
            _lr.loop             = false;
            _lr.positionCount    = 0;
            _lr.startWidth       = 0.055f;
            _lr.endWidth         = 0.055f;
            _lr.sortingLayerName = "Default";
            _lr.sortingOrder     = TopOrder;
            _lr.material         = new Material(Shader.Find("Sprites/Default"));

            // 원형 fill
            _fillGo = new GameObject("Fill");
            _fillGo.transform.SetParent(transform, false);
            _fillSr = _fillGo.AddComponent<SpriteRenderer>();
            _fillSr.sprite       = GameSetup.WhiteSquareStatic();
            _fillSr.sortingOrder = TopOrder - 1;
            _fillGo.SetActive(false);

            // 사각형 fill L / R (DragonStatue)
            _rectGoL = new GameObject("RectL");
            _rectGoL.transform.SetParent(transform, false);
            _rectSrL = _rectGoL.AddComponent<SpriteRenderer>();
            _rectSrL.sprite       = GameSetup.WhiteSquareStatic();
            _rectSrL.sortingOrder = TopOrder - 1;
            _rectGoL.SetActive(false);

            _rectGoR = new GameObject("RectR");
            _rectGoR.transform.SetParent(transform, false);
            _rectSrR = _rectGoR.AddComponent<SpriteRenderer>();
            _rectSrR.sprite       = GameSetup.WhiteSquareStatic();
            _rectSrR.sortingOrder = TopOrder - 1;
            _rectGoR.SetActive(false);

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

        /// <summary>채워진 원 + 외곽선 표시 (일반 터렛)</summary>
        public void ShowCircle(Vector3 center, float radius, Color col, float duration = 0f)
        {
            ClearAll();
            gameObject.SetActive(true);
            SetFade(duration);

            // 외곽선
            DrawCircleLR(center, radius, col);

            // 채워진 원 (반투명)
            Color fillCol = new Color(col.r, col.g, col.b, col.a * 0.18f);
            _fillGo.transform.position   = new Vector3(center.x, center.y, -0.05f);
            _fillGo.transform.localScale = Vector3.one * radius * 2f;
            _fillSr.color                = fillCol;
            _fillGo.SetActive(true);
        }

        /// <summary>타일 기반 하이라이트 (SpikeTrap, ElectricGate 등)</summary>
        public void ShowTiles(List<Tile> tiles, Color col, float duration = 0f)
        {
            ClearAll();
            gameObject.SetActive(true);
            SetFade(duration);

            float tileSize = MapManager.Instance.tileSize;
            foreach (var t in tiles)
            {
                var go = new GameObject("TH");
                go.transform.SetParent(_tileRoot.transform);
                go.transform.position   = t.transform.position;
                go.transform.localScale = Vector3.one * tileSize * 0.92f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = GameSetup.WhiteSquareStatic();
                sr.color        = col;
                sr.sortingOrder = TopOrder;
                _tileHighlights.Add(sr);
            }
        }

        /// <summary>방향 표시 (MeleeTurret)</summary>
        public void ShowDirection(Vector3 origin, Vector2Int dir, float length, Color col, float width = 0.7f, float duration = 0f)
        {
            ClearAll();
            gameObject.SetActive(true);
            SetFade(duration);

            Vector3 d    = new Vector3(dir.x, dir.y, 0f).normalized;
            Vector3 perp = new Vector3(-d.y, d.x, 0f) * width * 0.5f;
            Vector3 tip  = origin + d * length;

            _lr.loop          = true;
            _lr.positionCount = 4;
            _lr.startColor    = col;
            _lr.endColor      = col;
            _lr.startWidth    = 0.055f;
            _lr.endWidth      = 0.055f;

            float startW = 0.3f;
            float z      = -0.1f;
            _lr.SetPosition(0, origin + new Vector3(-d.y, d.x, 0f) * startW * 0.5f + Vector3.forward * z);
            _lr.SetPosition(1, tip    + perp + Vector3.forward * z);
            _lr.SetPosition(2, tip    - perp + Vector3.forward * z);
            _lr.SetPosition(3, origin - new Vector3(-d.y, d.x, 0f) * startW * 0.5f + Vector3.forward * z);

            // fill: 삼각형 방향 사각형 (반투명)
            Color fillCol = new Color(col.r, col.g, col.b, col.a * 0.18f);
            Vector3 rectCenter = (origin + tip) * 0.5f;
            _fillGo.transform.position   = new Vector3(rectCenter.x, rectCenter.y, -0.05f);
            _fillGo.transform.localScale = new Vector3(width, length, 1f);
            // 방향에 맞게 회전
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
            _fillGo.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _fillSr.color              = fillCol;
            _fillGo.SetActive(true);
        }

        /// <summary>좌우 브레스 사각형 (DragonStatue)</summary>
        public void ShowBreathRect(Vector3 center, float rangeX, float rangeY, Color col, float duration = 0f)
        {
            ClearAll();
            gameObject.SetActive(true);
            SetFade(duration);

            float hw = rangeY * 0.5f;
            float z  = -0.1f;

            // 외곽선 (기존 방식)
            _lr.loop          = false;
            _lr.positionCount = 10;
            _lr.startColor    = col;
            _lr.endColor      = col;
            _lr.startWidth    = 0.055f;
            _lr.endWidth      = 0.055f;
            _lr.SetPosition(0, new Vector3(center.x,          center.y + hw, z));
            _lr.SetPosition(1, new Vector3(center.x - rangeX, center.y + hw, z));
            _lr.SetPosition(2, new Vector3(center.x - rangeX, center.y - hw, z));
            _lr.SetPosition(3, new Vector3(center.x,          center.y - hw, z));
            _lr.SetPosition(4, new Vector3(center.x,          center.y + hw, z));
            _lr.SetPosition(5, new Vector3(center.x,          center.y - hw, z));
            _lr.SetPosition(6, new Vector3(center.x + rangeX, center.y - hw, z));
            _lr.SetPosition(7, new Vector3(center.x + rangeX, center.y + hw, z));
            _lr.SetPosition(8, new Vector3(center.x,          center.y + hw, z));
            _lr.SetPosition(9, new Vector3(center.x,          center.y - hw, z));

            // fill: 좌우 사각형 반투명
            Color fillCol = new Color(col.r, col.g, col.b, col.a * 0.18f);

            float centerL = center.x - rangeX * 0.5f;
            float centerR = center.x + rangeX * 0.5f;

            _rectGoL.transform.position   = new Vector3(centerL, center.y, -0.05f);
            _rectGoL.transform.localScale = new Vector3(rangeX, rangeY, 1f);
            _rectGoL.transform.rotation   = Quaternion.identity;
            _rectSrL.color                = fillCol;
            _rectGoL.SetActive(true);

            _rectGoR.transform.position   = new Vector3(centerR, center.y, -0.05f);
            _rectGoR.transform.localScale = new Vector3(rangeX, rangeY, 1f);
            _rectGoR.transform.rotation   = Quaternion.identity;
            _rectSrR.color                = fillCol;
            _rectGoR.SetActive(true);
        }

        public void Hide()
        {
            ClearAll();
            gameObject.SetActive(false);
            _fadeTime = 0f;
            _timer    = 0f;
        }

        // ── 내부 ──────────────────────────────────────────────────────
        private void ClearAll()
        {
            _lr.positionCount = 0;
            _lr.loop          = false;
            _fillGo.SetActive(false);
            _fillGo.transform.rotation = Quaternion.identity;
            _rectGoL.SetActive(false);
            _rectGoR.SetActive(false);
            ClearTileHighlights();
        }

        private void SetFade(float duration) { _fadeTime = duration; _timer = 0f; }

        private void DrawCircleLR(Vector3 center, float radius, Color col)
        {
            _lr.loop          = true;
            _lr.positionCount = CircleSegs;
            _lr.startColor    = col;
            _lr.endColor      = col;
            _lr.startWidth    = 0.055f;
            _lr.endWidth      = 0.055f;
            for (int i = 0; i < CircleSegs; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegs;
                _lr.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius, -0.1f));
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
