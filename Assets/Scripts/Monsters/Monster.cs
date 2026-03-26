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
        public bool  IsAlive => _hp > 0f && gameObject.activeInHierarchy;

        public SpriteRenderer bodyRenderer;
        public SpriteRenderer hpBarFill;

        public string PrefabName { get; private set; }
        public void SetPrefabName(string name) => PrefabName = name;

        // HP바 초기 값 캐시
        private float _hpBarInitScaleX;
        private float _hpBarInitPosX;

        private float   _baseSpeed;
        private float   _slowTimer;
        private Vector3 _navOffset;
        private Vector3 _smoothVel;
        private float   _hp;
        private SpriteRenderer[] _allRenderers;
        private Animator         _animator;
        private int[]            _rendererBaseOrders;
        private Vector3          _originalScale;
        private float            _lastPopupTime;
        private List<Vector3> _path    = new List<Vector3>();
        private int           _pathIdx = 0;

private void Awake() { if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(); _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(); _originalScale = transform.localScale; CacheRenderers(); }

private void CacheRenderers() { _allRenderers = GetComponentsInChildren<SpriteRenderer>(true); _rendererBaseOrders = new int[_allRenderers.Length]; for (int i = 0; i < _allRenderers.Length; i++) _rendererBaseOrders[i] = _allRenderers[i].sortingOrder; }


public void Init(List<Vector2Int> gridPath, Color color) { _hp = maxHp; _pathIdx = 0; _path.Clear(); _baseSpeed = speed; _slowTimer = 0f; gameObject.SetActive(true); transform.localScale = _originalScale; if (bodyRenderer != null && color != Color.white) bodyRenderer.color = color; else if (bodyRenderer != null) bodyRenderer.color = Color.white; float step = MapManager.Instance.tileSize + MapManager.Instance.tileGap; float maxOffset = step * 0.28f; _navOffset = new Vector3(Random.Range(-maxOffset, maxOffset), Random.Range(-maxOffset, maxOffset), 0f); foreach (var gp in gridPath) _path.Add(MapManager.Instance.GridToWorld(gp.x, gp.y)); if (_path.Count > 0) transform.position = _path[0] + _navOffset; if (hpBarFill != null) { _hpBarInitScaleX = hpBarFill.transform.localScale.x; _hpBarInitPosX = hpBarFill.transform.localPosition.x; } RefreshHpBar(); UpdateSortingOrder(); }

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

private void Update() { if (!IsAlive) return; if (GameManager.Instance.CurrentState != GameState.WaveInProgress) return; if (_slowTimer > 0f) { _slowTimer -= Time.deltaTime; if (_slowTimer <= 0f) { speed = _baseSpeed; if (bodyRenderer != null) UpdateSortingOrder(); } } if (_pathIdx >= _path.Count) return; bool isLast = (_pathIdx == _path.Count - 1); Vector3 currentWP = isLast ? _path[_pathIdx] : _path[_pathIdx] + _navOffset; float distToCurrent = Vector3.Distance(transform.position, currentWP); if (!isLast && distToCurrent < 0.35f) { _pathIdx++; return; } if (isLast && distToCurrent < 0.08f) { ReachEnd(); return; } Vector3 moveTarget = currentWP; if (!isLast && _pathIdx + 1 < _path.Count) { bool nextIsLast = (_pathIdx + 1 == _path.Count - 1); Vector3 nextWP = nextIsLast ? _path[_pathIdx + 1] : _path[_pathIdx + 1] + _navOffset; float blend = Mathf.Clamp01(1f - distToCurrent / 0.6f); moveTarget = Vector3.Lerp(currentWP, nextWP, blend * 0.6f); } Vector3 dir = moveTarget - transform.position; if (Mathf.Abs(dir.x) > 0.01f && bodyRenderer != null) bodyRenderer.flipX = dir.x < 0; transform.position = Vector3.SmoothDamp(transform.position, moveTarget, ref _smoothVel, 0.08f, speed * 3f); UpdateSortingOrder(); }

public void TakeDamage(float dmg) { if (!IsAlive) return; _hp -= dmg; RefreshHpBar(); if (_animator != null) _animator.SetTrigger("Hit"); float now = Time.time; if (now - _lastPopupTime > 0.12f) { _lastPopupTime = now; DamagePopup.Create(transform.position, dmg); } if (_hp <= 0f) Die(); }

private void Die() { _hp = 0f; if (_animator != null) { _animator.SetTrigger("Dead"); } GameManager.Instance.OnMonsterKilled(); MonsterManager.Instance.OnMonsterDied(this); StartCoroutine(DieAnim()); }

private System.Collections.IEnumerator DieAnim() { if (_animator != null) { float waitTime = 0.5f; var clips = _animator.runtimeAnimatorController?.animationClips; if (clips != null) foreach (var c in clips) if (c.name.ToLower().Contains("dead") || c.name.ToLower().Contains("die")) { waitTime = c.length; break; } yield return new WaitForSeconds(waitTime); } else { Vector3 orig = transform.localScale; float t = 0f; while (t < 0.2f) { transform.localScale = Vector3.Lerp(orig, Vector3.zero, t / 0.2f); t += Time.deltaTime; yield return null; } transform.localScale = _originalScale; } MonsterManager.Instance.ReturnToPool(this, PrefabName); }

private void ReachEnd() { MonsterManager.Instance.ReturnToPool(this, PrefabName); GameManager.Instance.TriggerGameOver(); }

private void RefreshHpBar() { if (hpBarFill == null) return; float ratio = Mathf.Clamp01(_hp / maxHp); var tf = hpBarFill.transform; float newScaleX = _hpBarInitScaleX * ratio; var ls = tf.localScale; ls.x = newScaleX; tf.localScale = ls; float offset = _hpBarInitScaleX * (1f - ratio) * 0.5f; var lp = tf.localPosition; lp.x = _hpBarInitPosX - offset; tf.localPosition = lp; hpBarFill.color = Color.Lerp(Color.red, Color.green, ratio); }
    

private void UpdateSortingOrder() { if (bodyRenderer == null || _allRenderers == null) return; int baseOrder = Mathf.RoundToInt(500f - transform.position.y * 10f); for (int i = 0; i < _allRenderers.Length; i++) { if (_allRenderers[i] == null) continue; _allRenderers[i].sortingOrder = baseOrder + _rendererBaseOrders[i]; } }


public void ApplySlow(float factor, float duration) { _baseSpeed = _baseSpeed > 0 ? _baseSpeed : speed; speed = _baseSpeed * factor; _slowTimer = duration; if (bodyRenderer != null) bodyRenderer.color = new Color(0.4f, 0.7f, 1f); }
}
}
