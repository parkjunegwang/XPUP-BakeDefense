using System.Collections;
using UnityEngine;
using TMPro;

namespace Underdark
{
    public class DamagePopup : MonoBehaviour
    {
        private static DamagePopup _prefab;

        public static void Create(Vector3 worldPos, float damage, bool isCrit = false)
        {
            if (_prefab == null) BuildPrefab();

            float xOff = Random.Range(-0.2f, 0.2f);
            float yOff = isCrit ? 0.5f : 0.3f;
            var go = Instantiate(_prefab.gameObject,
                worldPos + new Vector3(xOff, yOff, 0f),
                Quaternion.identity);
            go.SetActive(true);
            var popup = go.GetComponent<DamagePopup>();
            if (popup._tmp == null)
                popup._tmp = go.GetComponent<TextMeshPro>() ?? go.AddComponent<TextMeshPro>();
            popup.Setup(damage, isCrit);
        }

        public TextMeshPro _tmp;
        private float   _elapsed;
        private Vector3 _vel;
        private bool    _isCrit;

        private void Awake()
        {
            _tmp = GetComponent<TextMeshPro>();
            if (_tmp == null) _tmp = gameObject.AddComponent<TextMeshPro>();
        }

public void Setup(float damage, bool isCrit) { _isCrit = isCrit; _elapsed = 0f; bool isMiss = damage <= 0f; if (isMiss) { _tmp.text = "<i>Miss</i>"; _tmp.fontSize = 2.8f; _tmp.color = new Color(0.7f, 0.7f, 0.7f, 0.9f); _tmp.outlineWidth = 0.15f; _tmp.outlineColor = new Color(0f, 0f, 0f, 0.5f); transform.localScale = Vector3.one; _vel = new Vector3(Random.Range(-0.3f, 0.3f), 1.5f, 0f); Destroy(gameObject, 0.65f); } else if (isCrit) { string num = damage >= 1f ? Mathf.RoundToInt(damage).ToString() : damage.ToString("F1"); _tmp.text = $"<b>{num}!</b>"; _tmp.fontSize = 5.5f; _tmp.color = new Color(1f, 0.55f, 0.05f); _tmp.outlineWidth = 0.35f; _tmp.outlineColor = new Color(0.5f, 0.1f, 0f, 1f); transform.localScale = Vector3.one * 1.6f; _vel = new Vector3(Random.Range(-0.3f, 0.3f), 3.2f, 0f); Destroy(gameObject, 1.1f); } else { string num = damage >= 1f ? Mathf.RoundToInt(damage).ToString() : damage.ToString("F1"); _tmp.text = num; _tmp.fontSize = 3.0f; _tmp.color = Color.white; _tmp.outlineWidth = 0.25f; _tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f); transform.localScale = Vector3.one; _vel = new Vector3(Random.Range(-0.5f, 0.5f), 2.2f, 0f); Destroy(gameObject, 0.85f); } _tmp.sortingOrder = SLayer.Projectile; _tmp.alignment = TextAlignmentOptions.Center; _tmp.enableWordWrapping = false; StartCoroutine(Animate()); }

private IEnumerator Animate() { bool isMiss = _tmp.text.Contains("Miss"); float duration = _isCrit ? 1.0f : isMiss ? 0.6f : 0.8f; Color startCol = _tmp.color; while (_elapsed < duration) { _elapsed += Time.deltaTime; float t = _elapsed / duration; _vel.y -= (_isCrit ? 6f : isMiss ? 3f : 4.5f) * Time.deltaTime; transform.position += _vel * Time.deltaTime; if (_isCrit) { float scl = t < 0.12f ? Mathf.Lerp(1.6f, 1.0f, t / 0.12f) : 1.0f; transform.localScale = Vector3.one * scl; } float fadeStart = _isCrit ? 0.55f : isMiss ? 0.4f : 0.6f; float alpha = t < fadeStart ? 1f : Mathf.Lerp(1f, 0f, (t - fadeStart) / (1f - fadeStart)); _tmp.color = new Color(startCol.r, startCol.g, startCol.b, alpha); yield return null; } }

        private static void BuildPrefab()
        {
            var go  = new GameObject("DamagePopup");
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.fontSize         = 3.0f;
            tmp.color            = Color.white;
            tmp.outlineWidth     = 0.25f;
            tmp.outlineColor     = Color.black;
            tmp.sortingOrder     = SLayer.Projectile;
            tmp.enableWordWrapping = false;

            var popup  = go.AddComponent<DamagePopup>();
            popup._tmp = tmp;
            go.SetActive(false);
            _prefab = popup;
            DontDestroyOnLoad(go);
        }
    }
}
