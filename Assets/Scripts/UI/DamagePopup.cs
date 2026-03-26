using System.Collections;
using UnityEngine;
using TMPro;

namespace Underdark
{
    /// <summary>
    /// 데미지 숫자 팝업. Monster.TakeDamage()에서 호출.
    /// TextMeshPro 없이도 동작 (fallback SpriteRenderer 숫자 대신 TMP 사용 시 더 예쁨).
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private static DamagePopup _prefab;

        /// <summary>데미지 숫자 팝업 생성</summary>
public static void Create(Vector3 worldPos, float damage, bool isCrit = false) { if (_prefab == null) BuildPrefab(); var go = Instantiate(_prefab.gameObject, worldPos + new Vector3(Random.Range(-0.15f, 0.15f), 0.35f, 0f), Quaternion.identity); go.SetActive(true); var popup = go.GetComponent<DamagePopup>(); if (popup._tmp == null) popup._tmp = go.GetComponent<TextMeshPro>() ?? go.AddComponent<TextMeshPro>(); popup.Setup(damage, isCrit); }

        private TextMeshPro _tmp;
        private float       _elapsed;
        private Vector3     _vel;

private void Awake() { _tmp = GetComponent<TextMeshPro>(); if (_tmp == null) _tmp = gameObject.AddComponent<TextMeshPro>(); }

        public void Setup(float damage, bool isCrit)
        {
            // 소수점 없이 표시 (1 이상), 작은 데미지는 소수점 1자리
            string txt = damage >= 1f ? Mathf.RoundToInt(damage).ToString()
                                      : damage.ToString("F1");
            _tmp.text = isCrit ? $"<b>{txt}!</b>" : txt;

            if (isCrit)
            {
                _tmp.fontSize  = 4.2f;
                _tmp.color     = new Color(1f, 0.9f, 0.1f);
            }
            else
            {
                _tmp.fontSize  = 3.0f;
                _tmp.color     = Color.white;
            }

            _tmp.outlineWidth = 0.25f;
            _tmp.outlineColor = new Color(0f, 0f, 0f, 0.8f);
            _tmp.sortingOrder = SLayer.Projectile; // 최상위

            // 랜덤 튀어오르는 방향
            _vel = new Vector3(Random.Range(-0.5f, 0.5f), 2.2f, 0f);
            _elapsed = 0f;
            Destroy(gameObject, 0.85f);
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float duration = 0.8f;
            Color startCol = _tmp.color;

            while (_elapsed < duration)
            {
                _elapsed += Time.deltaTime;
                float t = _elapsed / duration;

                // 위로 올라가다가 중력 느끼듯 감속
                _vel.y -= 4.5f * Time.deltaTime;
                transform.position += _vel * Time.deltaTime;

                // 페이드 아웃 (후반 40%에서)
                float alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                _tmp.color = new Color(startCol.r, startCol.g, startCol.b, alpha);

                // 작아지면서 사라짐
                float scale = t < 0.1f ? Mathf.Lerp(0.5f, 1f, t / 0.1f) : 1f;
                transform.localScale = Vector3.one * scale;

                yield return null;
            }
        }

        // ── 프리팹 동적 생성 ─────────────────────────────────────────
private static void BuildPrefab() { var go = new GameObject("DamagePopup"); var tmp = go.AddComponent<TextMeshPro>(); tmp.alignment = TextAlignmentOptions.Center; tmp.fontSize = 3.0f; tmp.color = Color.white; tmp.outlineWidth = 0.25f; tmp.outlineColor = Color.black; tmp.sortingOrder = SLayer.Projectile; tmp.enableWordWrapping = false; var popup = go.AddComponent<DamagePopup>(); popup._tmp = tmp; go.SetActive(false); _prefab = popup; DontDestroyOnLoad(go); }
    }
}
