using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 십자(+) 4방향 동시 근접 공격 터렛.
    /// MeleeTurret과 달리 방향 선택 없이 상하좌우를 한 번에 공격.
    /// </summary>
    public class CrossMeleeTurret : TurretBase
    {
        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int( 0,  1),  // Up
            new Vector2Int( 0, -1),  // Down
            new Vector2Int(-1,  0),  // Left
            new Vector2Int( 1,  0),  // Right
        };

        private int _attackTiles = 1;
        public  int AttackTiles => _attackTiles;

        // 각 방향당 슬래시 이펙트 SpriteRenderer
        private SpriteRenderer[] _slashSrs = new SpriteRenderer[4];

        protected override void Awake()
        {
            turretType = TurretType.CrossMeleeTurret;
            if (statData == null) { damage = 15f; range = 1.5f; fireRate = 1.0f; hp = 120f; }
            base.Awake();

            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = SLayer.Turret;

            for (int i = 0; i < Dirs.Length; i++)
                _slashSrs[i] = BuildSlash(i);

            RefreshSlashes();
        }

        private SpriteRenderer BuildSlash(int dirIndex)
        {
            var go = new GameObject($"Slash_{dirIndex}");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameSetup.WhiteSquareStatic();
            sr.color  = new Color(1f, 0.85f, 0.1f, 0f);
            sr.sortingOrder = SLayer.Effect;
            return sr;
        }

        private void RefreshSlashes()
        {
            float step = MapManager.Instance != null
                ? MapManager.Instance.tileSize + MapManager.Instance.tileGap
                : 1.05f;

            for (int i = 0; i < Dirs.Length; i++)
            {
                if (_slashSrs[i] == null) continue;
                var dir  = Dirs[i];
                float dist = _attackTiles * step * 0.5f;
                float len  = 0.75f * _attackTiles;

                _slashSrs[i].transform.localPosition =
                    new Vector3(dir.x * dist, dir.y * dist, 0f);

                // 좌우 방향이면 세로로 회전
                _slashSrs[i].transform.localRotation = (dir.x != 0)
                    ? Quaternion.Euler(0f, 0f, 90f)
                    : Quaternion.identity;

                _slashSrs[i].transform.localScale = new Vector3(len, 0.18f, 1f);
            }
        }

        protected override void OnTick()
        {
            var targets = GetMonstersInCross();
            if (targets.Count == 0) return;

            float dmg = RollDamage(out bool isCrit);
            foreach (var m in targets)
                m.TakeDamage(dmg, isCrit);

            StartCoroutine(CrossSlashRoutine(isCrit));
        }

        private List<Monster> GetMonstersInCross()
        {
            var result = new List<Monster>();
            if (currentTile == null) return result;

            var map      = MapManager.Instance;
            float step     = map.tileSize + map.tileGap;
            float halfTile = step * 0.6f;

            // 4방향 공격 타일 수집
            var attackTiles = new List<Tile>();
            foreach (var dir in Dirs)
            {
                for (int i = 1; i <= _attackTiles; i++)
                {
                    var t = map.GetTile(
                        currentTile.gridX + dir.x * i,
                        currentTile.gridY + dir.y * i);
                    if (t != null) attackTiles.Add(t);
                }
            }
            if (attackTiles.Count == 0) return result;

            var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                Vector2 mp = m.transform.position;
                foreach (var tile in attackTiles)
                {
                    Vector2 tp = tile.transform.position;
                    if (Mathf.Abs(mp.x - tp.x) <= halfTile &&
                        Mathf.Abs(mp.y - tp.y) <= halfTile)
                    {
                        result.Add(m);
                        break;
                    }
                }
            }
            return result;
        }

        private IEnumerator CrossSlashRoutine(bool isCrit)
        {
            Color col = isCrit
                ? new Color(1f, 0.9f, 0.1f, 0.95f)
                : new Color(1f, 0.9f, 0.1f, 0.9f);
            float dur = isCrit ? 0.22f : 0.14f;

            // 4방향 동시 켜기
            foreach (var sr in _slashSrs)
                if (sr != null) sr.color = col;

            float t = 0f;
            while (t < dur)
            {
                float a = Mathf.Lerp(col.a, 0f, t / dur);
                foreach (var sr in _slashSrs)
                    if (sr != null) sr.color = new Color(col.r, col.g, col.b, a);
                t += Time.deltaTime;
                yield return null;
            }

            foreach (var sr in _slashSrs)
                if (sr != null) sr.color = new Color(col.r, col.g, col.b, 0f);
        }

        protected override void OnLevelUp()
        {
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
            RefreshSlashes();
        }
    }
}
