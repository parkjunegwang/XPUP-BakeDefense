using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class LavaPuddle : MonoBehaviour
    {
        private float _damage;
        private float _duration;
        private float _elapsed;
        private float _tickInterval = 0.5f;
        private float _tickTimer;
        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = GameSetup.WhiteSquareStatic();
            _sr.color = new Color(1f, 0.3f, 0.0f, 0.75f);
            _sr.sortingOrder = SLayer.TrapEffect;
        }

public void Init(float damage, float duration, float size)
        {
            _damage   = damage;
            _duration = duration;
            transform.localScale = Vector3.one * size;

            // LavaRain_area_01 ~ 03 랜덤 선택
            int idx = Random.Range(1, 4); // 1, 2, 3
            var sprite = Resources.Load<Sprite>($"Image/LavaRain_area_0{idx}");
            if (sprite != null)
            {
                _sr.sprite = sprite;
                _sr.color  = Color.white; // 스프라이트 색상 그대로
            }
            else
            {
                // 로드 실패 시 fallback
                _sr.sprite = GameSetup.WhiteSquareStatic();
                _sr.color  = new Color(1f, 0.3f, 0f, 0.75f);
            }
        }

private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return;

            _elapsed   += Time.deltaTime;
            _tickTimer += Time.deltaTime;

            // 페이드 아웃
            float alpha = Mathf.Lerp(0.85f, 0f, _elapsed / _duration);
            var c = _sr.color;
            _sr.color = new Color(c.r, c.g, c.b, alpha);

            if (_tickTimer >= _tickInterval)
            {
                _tickTimer = 0f;

                // 타일 크기의 절반을 판정 반경으로 사용
                float half = transform.localScale.x * 0.5f;

                var monsters = new List<Monster>(MonsterManager.Instance.ActiveMonsters);
                foreach (var m in monsters)
                {
                    if (m == null || !m.IsAlive) continue;

                    // 스윗 판정: 이전프레임→현재 선분이 용암 AABB를 통과해도 적중
                    if (SweepCheck(m.LastPosition, m.transform.position,
                                   transform.position, half))
                        m.TakeDamage(_damage);
                }
            }

            if (_elapsed >= _duration) Destroy(gameObject);
        }

        private bool SweepCheck(Vector2 prev, Vector2 cur, Vector2 center, float half)
        {
            if (Mathf.Abs(cur.x  - center.x) <= half && Mathf.Abs(cur.y  - center.y) <= half) return true;
            if (Mathf.Abs(prev.x - center.x) <= half && Mathf.Abs(prev.y - center.y) <= half) return true;

            float dx = cur.x - prev.x, dy = cur.y - prev.y;
            if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return false;

            float txMin = Mathf.Abs(dx) < 0.0001f ? float.NegativeInfinity : (center.x - half - prev.x) / dx;
            float txMax = Mathf.Abs(dx) < 0.0001f ? float.PositiveInfinity : (center.x + half - prev.x) / dx;
            float tyMin = Mathf.Abs(dy) < 0.0001f ? float.NegativeInfinity : (center.y - half - prev.y) / dy;
            float tyMax = Mathf.Abs(dy) < 0.0001f ? float.PositiveInfinity : (center.y + half - prev.y) / dy;

            if (txMin > txMax) { float tmp = txMin; txMin = txMax; txMax = tmp; }
            if (tyMin > tyMax) { float tmp = tyMin; tyMin = tyMax; tyMax = tmp; }

            float tEnter = Mathf.Max(txMin, tyMin);
            float tExit  = Mathf.Min(txMax, tyMax);
            return tEnter <= tExit && tExit >= 0f && tEnter <= 1f;
        }
    }

    /// <summary>
    /// 6. 마그마 비 터렛 - 하늘로 마그마를 쏴 빈 타일에 용암 웅덩이 생성
    /// </summary>
    public class LavaRainTurret : TurretBase
    {
        [Header("Lava Rain Settings")]
        [Tooltip("한 번에 떨어지는 마그마 수")]
        public int   lavaCount    = 3;
        [Tooltip("용암 웅덩이 지속 시간")]
        public float pudleDuration = 4f;
        [Tooltip("마그마가 떨어지는 반경 (타일 수)")]
        public float dropRadius   = 3f;

        protected override void Awake()
        {
            turretType = TurretType.LavaRain;
            if (statData == null) { damage = 12f; range = 99f; fireRate = 0.5f; hp = 90f; }
            base.Awake();
        }

        protected override void OnTick()
        {
            StartCoroutine(LavaRainRoutine());
        }

        private IEnumerator LavaRainRoutine()
        {
            var map = MapManager.Instance;
            if (map == null) yield break;

            // 빈 타일 목록 수집
            var emptyTiles = new List<Tile>();
            for (int y = 0; y < map.rows; y++)
            for (int x = 0; x < map.columns; x++)
            {
                var t = map.GetTile(x, y);
                if (t == null) continue;
                if (t.tileType != TileType.Empty) continue;
                if (t.placedTurret != null) continue;
                // 터렛 주변 dropRadius 이내
                float dist = Vector2.Distance(transform.position, map.GridToWorld(x, y));
                if (dist <= dropRadius) emptyTiles.Add(t);
            }

            if (emptyTiles.Count == 0) yield break;

            for (int i = 0; i < lavaCount; i++)
            {
                // 랜덤 빈 타일 선택
                var tile = emptyTiles[Random.Range(0, emptyTiles.Count)];
                Vector3 landPos = tile.transform.position;

                // 낙하 애니메이션 (위에서 떨어짐)
                StartCoroutine(DropLava(landPos));
                yield return new WaitForSeconds(0.15f);
            }
        }

        private IEnumerator DropLava(Vector3 landPos)
        {
            // 낙하 이펙트
            var drop = new GameObject("LavaDrop");
            drop.transform.position = landPos + Vector3.up * 5f;
            var sr = drop.AddComponent<SpriteRenderer>();
            sr.sprite = GameSetup.WhiteSquareStatic();
            sr.color = new Color(1f, 0.5f, 0.0f);
            sr.sortingOrder = SLayer.Effect;
            drop.transform.localScale = Vector3.one * 0.2f;

            float t = 0f;
            float dur = 0.35f;
            while (t < dur)
            {
                t += Time.deltaTime;
                drop.transform.position = Vector3.Lerp(
                    landPos + Vector3.up * 5f, landPos, t / dur);
                yield return null;
            }

            Destroy(drop);

            // 용암 웅덩이 생성
            var puddle = new GameObject("LavaPuddle");
            puddle.transform.position = landPos;
            var lp = puddle.AddComponent<LavaPuddle>();
            lp.Init(damage, pudleDuration, 0.9f);
        }
    }
}
