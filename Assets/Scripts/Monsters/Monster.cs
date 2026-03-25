using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    public class Monster : MonoBehaviour
    {
        public float maxHp  = 10f;
        public float speed  = 2f;
        public int   reward = 5;
        public bool  IsAlive => _hp > 0f;

        public SpriteRenderer bodyRenderer;
        public SpriteRenderer hpBarFill;

        // HP바 초기 값 캐시
        private float _hpBarInitScaleX;
        private float _hpBarInitPosX;

        private float        _hp;
        private List<Vector3> _path    = new List<Vector3>();
        private int           _pathIdx = 0;

        private void Awake()
        {
            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = SLayer.Monster;
        }

        public void Init(List<Vector2Int> gridPath, Color color)
        {
            _hp      = maxHp;
            _pathIdx = 0;
            _path.Clear();
            if (bodyRenderer != null) bodyRenderer.color = color;
            foreach (var gp in gridPath)
                _path.Add(MapManager.Instance.GridToWorld(gp.x, gp.y));
            if (_path.Count > 0) transform.position = _path[0];

            // HP바 초기값 코드에서 저장
            if (hpBarFill != null)
            {
                _hpBarInitScaleX = hpBarFill.transform.localScale.x;
                _hpBarInitPosX   = hpBarFill.transform.localPosition.x;
            }

            RefreshHpBar();
        }

        /// <summary>
        /// 경로 재계산 시 호출. 현재 위치에서 가장 가까운 앞쪽 웨이포인트부터 이어감.
        /// </summary>
        public void UpdatePath(List<Vector2Int> newGrid)
        {
            var newWorld = new List<Vector3>();
            foreach (var gp in newGrid)
                newWorld.Add(MapManager.Instance.GridToWorld(gp.x, gp.y));

            // 현재 위치에서 가장 가까운 웨이포인트 찾기
            float minDist = float.MaxValue;
            int   bestIdx = 0;
            for (int i = 0; i < newWorld.Count; i++)
            {
                float d = Vector3.Distance(transform.position, newWorld[i]);
                if (d < minDist) { minDist = d; bestIdx = i; }
            }

            _path    = newWorld;
            _pathIdx = bestIdx;
        }

        private void Update()
        {
            if (!IsAlive) return;
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return;
            if (_pathIdx >= _path.Count) return;

            Vector3 target = _path[_pathIdx];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                _pathIdx++;
                if (_pathIdx >= _path.Count)
                    ReachEnd();
            }
        }

        public void TakeDamage(float dmg)
        {
            if (!IsAlive) return;
            _hp -= dmg;
            RefreshHpBar();
            if (_hp <= 0f) Die();
        }

        private void Die()
        {
            _hp = 0f;
            GameManager.Instance.OnMonsterKilled();
            MonsterManager.Instance.OnMonsterDied(this);
            StartCoroutine(DieAnim());
        }

        private IEnumerator DieAnim()
        {
            Vector3 orig = transform.localScale;
            float t = 0f;
            while (t < 0.2f)
            {
                transform.localScale = Vector3.Lerp(orig, Vector3.zero, t / 0.2f);
                t += Time.deltaTime;
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private void ReachEnd()
        {
            gameObject.SetActive(false);
            GameManager.Instance.TriggerGameOver();
        }

private void RefreshHpBar() { if (hpBarFill == null) return; float ratio = Mathf.Clamp01(_hp / maxHp); var tf = hpBarFill.transform; float newScaleX = _hpBarInitScaleX * ratio; var ls = tf.localScale; ls.x = newScaleX; tf.localScale = ls; float offset = _hpBarInitScaleX * (1f - ratio) * 0.5f; var lp = tf.localPosition; lp.x = _hpBarInitPosX - offset; tf.localPosition = lp; hpBarFill.color = Color.Lerp(Color.red, Color.green, ratio); }
    }
}
