using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Underdark
{
    public class MeleeTurret : TurretBase
    {
        private static readonly Vector2Int[] Dirs = {
            new Vector2Int( 0, -1), // Down
            new Vector2Int(-1,  0), // Left
            new Vector2Int( 0,  1), // Up
            new Vector2Int( 1,  0), // Right
        };

        private int _dirIndex    = 0;
        private int _attackTiles = 1;

        public Vector2Int FacingDir => Dirs[_dirIndex];

        [Header("Visual Effects")]
        public SpriteRenderer arrowRenderer; // 프리팹에서 할당 (없으면 자동 생성)
        public SpriteRenderer slashRenderer;

        private SpriteRenderer _arrowSr;
        private SpriteRenderer _slashSr;

        protected override void Awake()
        {
            turretType = TurretType.MeleeTurret;
            if (statData == null) { damage = 20f; range = 1.5f; fireRate = 1.2f; hp = 100f; }
            base.Awake();

            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = SLayer.Turret;

            _arrowSr = arrowRenderer != null ? arrowRenderer : BuildArrow();
            _slashSr = slashRenderer != null ? slashRenderer : BuildSlash();
            RefreshArrow();
        }

        private SpriteRenderer BuildArrow()
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(transform);
            go.transform.localScale = Vector3.one * 0.22f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameSetup.WhiteSquareStatic();
            sr.color  = new Color(1f, 0.95f, 0.3f, 0.9f);
            sr.sortingOrder = SLayer.Effect;
            return sr;
        }

        private SpriteRenderer BuildSlash()
        {
            var go = new GameObject("Slash");
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.75f, 0.18f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameSetup.WhiteSquareStatic();
            sr.color  = new Color(1f, 0.85f, 0.1f, 0f);
            sr.sortingOrder = SLayer.Effect;
            return sr;
        }

        private void RefreshArrow()
        {
            Vector2Int dir = FacingDir;
            if (_arrowSr != null)
                _arrowSr.transform.localPosition = new Vector3(dir.x * 0.32f, dir.y * 0.32f, 0f);

            if (_slashSr != null)
            {
                float step = MapManager.Instance != null
                    ? MapManager.Instance.tileSize + MapManager.Instance.tileGap : 1.05f;
                float dist = _attackTiles * step * 0.5f;
                _slashSr.transform.localPosition = new Vector3(dir.x * dist, dir.y * dist, 0f);
                _slashSr.transform.localRotation = dir.x != 0
                    ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
                float len = 0.75f * _attackTiles;
                _slashSr.transform.localScale = new Vector3(len, 0.18f, 1f);
            }
        }

        protected override void Update()
        {
            if (GameManager.Instance.CurrentState == GameState.Preparation)
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.rightButton.wasPressedThisFrame)
                {
                    Vector2 screen   = mouse.position.ReadValue();
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
                    if (Vector2.Distance(transform.position, worldPos) < 0.6f)
                    {
                        RotateDirection();
                        return;
                    }
                }
            }
            base.Update();
        }

        public void RotateDirection()
        {
            _dirIndex = (_dirIndex + 1) % Dirs.Length;
            RefreshArrow();
            string[] names = { "Down", "Left", "Up", "Right" };
            UIManager.Instance?.ShowMessage($"Direction: {names[_dirIndex]}");
        }

        protected override void OnTick()
        {
            var targets = GetMonstersInFront();
            if (targets.Count == 0) return;
            foreach (var m in targets) m.TakeDamage(damage);
            StartCoroutine(SlashRoutine());
        }

        private List<Monster> GetMonstersInFront()
        {
            var result = new List<Monster>();
            if (currentTile == null) return result;
            Vector2Int dir  = FacingDir;
            float step      = MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            float maxDist   = _attackTiles * step + 0.3f;
            var monsters    = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                Vector2 toM  = (Vector2)m.transform.position - (Vector2)transform.position;
                Vector2 dir2 = new Vector2(dir.x, dir.y);
                if (Vector2.Dot(toM.normalized, dir2) > 0.6f && toM.magnitude <= maxDist)
                    result.Add(m);
            }
            return result;
        }

        private IEnumerator SlashRoutine()
        {
            if (_slashSr == null) yield break;
            _slashSr.color = new Color(1f, 0.9f, 0.1f, 0.9f);
            float t = 0f;
            while (t < 0.13f)
            {
                _slashSr.color = new Color(1f, 0.9f, 0.1f, Mathf.Lerp(0.9f, 0f, t / 0.13f));
                t += Time.deltaTime;
                yield return null;
            }
            _slashSr.color = new Color(1f, 0.9f, 0.1f, 0f);
        }

        protected override void OnLevelUp()
        {
            // statData에 attackTiles 정의되어 있으면 거기서 가져옴
            if (statData != null)
            {
                var s = statData.GetLevel(level);
                _attackTiles = Mathf.Max(1, s.attackTiles);
            }
            else
            {
                if (level >= 2) _attackTiles = 2;
                if (level >= 4) _attackTiles = 3;
            }
            RefreshArrow();
        }
    }
}
