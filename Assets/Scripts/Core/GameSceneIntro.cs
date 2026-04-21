using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Underdark
{
    /// <summary>
    /// GameScene 진입 연출.
    /// Main Camera에 붙여두면 씬 시작 시 검은 화면에서 페이드인하며 카메라가 아래서 위로 올라온다.
    /// </summary>
    public class GameSceneIntro : MonoBehaviour
    {
        /// <summary>카메라 인트로가 완전히 끝나면 true</summary>
        public static bool IsComplete { get; private set; }

        [Header("카메라 진입 연출")]
        [Tooltip("얼마나 아래에서 시작할지 (orthographicSize 배수 기준, 기본 2 = 화면 2개 아래)")]
        public float startOffsetMultiplier = 2f;

        [Tooltip("목표 Y 위치 (원래 카메라 위치)")]
        public float targetY = 0f;

        [Tooltip("올라오기 전 잠깐 멈추는 시간 (초)")]
        public float startDelay = 0.3f;

        [Tooltip("올라오는 데 걸리는 시간")]
        public float duration = 0.8f;

        [Tooltip("페이드인 시간 (검은 화면 → 게임 화면)")]
        public float fadeInDuration = 0.25f;

        [Tooltip("이징 커브 (비워두면 SmoothStep 사용)")]
        public AnimationCurve easeCurve;

        private Camera _cam;
        private Image  _blackPanel;

        private void OnEnable()
        {
            IsComplete = false;
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();

            // 카메라를 시작 위치로 이동
            float orthoSize = _cam != null ? _cam.orthographicSize : 6.5f;
            float startY    = targetY - orthoSize * startOffsetMultiplier * 2f;
            transform.position = new Vector3(transform.position.x, startY, transform.position.z);

            // 검은 패널 생성 (Screen Space - Overlay Canvas)
            _blackPanel = CreateBlackPanel();
        }

        private void Start()
        {
            StartCoroutine(PlayIntro());
        }

        private Image CreateBlackPanel()
        {
            var canvasGo = new GameObject("IntroBlackCanvas");
            DontDestroyOnLoad(canvasGo);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("BlackPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);

            var rt = panelGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = panelGo.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;

            return img;
        }

        private IEnumerator PlayIntro()
        {
            float orthoSize = _cam != null ? _cam.orthographicSize : 6.5f;
            float startY    = targetY - orthoSize * startOffsetMultiplier * 2f;

            Vector3 startPos  = new Vector3(transform.position.x, startY,  transform.position.z);
            Vector3 targetPos = new Vector3(transform.position.x, targetY, transform.position.z);

            // 딜레이 대기
            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);

            // 페이드인 (검은 화면 → 투명)
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeInDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeInDuration);
                if (_blackPanel != null)
                    _blackPanel.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            if (_blackPanel != null)
                Destroy(_blackPanel.transform.parent.gameObject);

            // 카메라 올라오기
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = easeCurve != null && easeCurve.length > 0
                    ? easeCurve.Evaluate(t)
                    : Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            transform.position = targetPos;
            IsComplete = true;
        }
    }
}
