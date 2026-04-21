using System.Collections;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// GameScene 진입 연출.
    /// CameraFitMap이 카메라 위치/사이즈를 확정한 뒤,
    /// 카메라를 맵 아래에서 맵 중앙으로 올리며 로비에서 넘어온 느낌을 줌.
    /// 이동 중 로비 배경이 카메라를 따라다니므로 빈 공간이 보이지 않음.
    /// </summary>
    public class GameSceneIntro : MonoBehaviour
    {
        /// <summary>카메라 인트로가 완전히 끝나면 true</summary>
        public static bool IsComplete { get; private set; }

        [Header("카메라 진입 연출")]
        [Tooltip("올라오기 전 잠깐 멈추는 시간 (초) - 로비 배경 보여주는 시간")]
        public float startDelay = 0.4f;

        [Tooltip("올라오는 데 걸리는 시간")]
        public float duration = 0.8f;

        [Tooltip("이징 커브 (비워두면 SmoothStep 사용)")]
        public AnimationCurve easeCurve;

        [Header("로비 배경 눈속임")]
        [Tooltip("로비와 같은 배경 스프라이트. 비워두면 Resources/Image/testBackground 자동 로드")]
        public Sprite lobbyBgSprite;

        private Camera     _cam;
        private GameObject _bgGo;

        private void OnEnable()
        {
            IsComplete = false;
        }

        private void Start()
        {
            _cam = GetComponent<Camera>();
            _cam.enabled =true;
            // CameraFitMap이 Start()에서 1프레임 뒤에 FitToMap()을 호출하므로
            // 그 다음 프레임까지 기다렸다가 인트로 시작
            StartCoroutine(WaitForCameraFitThenPlay());
        }

        private IEnumerator WaitForCameraFitThenPlay()
        {
            // CameraFitMap.FitNextFrame()이 yield return null 한 번만 기다리므로
            // 여기서 2프레임 대기하면 FitToMap() 완료 후 실행 보장
            yield return null;
            yield return null;

            PlayIntroSetup();
            StartCoroutine(PlayIntro());
        }

        private void PlayIntroSetup()
        {
            // 이 시점에 _cam.orthographicSize, transform.position 모두 CameraFitMap이 확정한 값
            // 카메라 화면 높이(orthographicSize * 2)만큼 아래에서 시작 → 로비 배경이 화면을 가득 채운 채 보임
            float startOffsetY = _cam != null ? _cam.orthographicSize * 2f : 10f;

            Vector3 targetPos = transform.position; // 맵 중앙 (CameraFitMap이 설정한 위치)

            // 카메라를 화면 한 개 높이만큼 아래로 이동
            transform.position = new Vector3(targetPos.x, targetPos.y - startOffsetY, targetPos.z);

            // 로비 배경 생성 (카메라 뷰에 꽉 차게, 카메라 자식으로)
            SpawnLobbyBackground();
        }

        private void SpawnLobbyBackground()
        {
            var spr = lobbyBgSprite != null
                ? lobbyBgSprite
                : Resources.Load<Sprite>("Image/testBackground");

            if (spr == null)
            {
                Debug.LogWarning("[GameSceneIntro] 로비 배경 스프라이트 없음 (Resources/Image/testBackground)");
                return;
            }

            _bgGo = new GameObject("IntroBg");
            // 카메라 자식으로 붙이면 카메라가 움직여도 항상 화면을 채움
            _bgGo.transform.SetParent(transform, false);
            _bgGo.transform.localPosition = new Vector3(0f, 0f, 12f); // 카메라보다 앞 (2D에서 z=12면 충분)

            var sr = _bgGo.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = -100;

            // 카메라 뷰를 꽉 채우도록 스케일
            float orthoSize = _cam != null ? _cam.orthographicSize : 6.5f;
            float aspect    = _cam != null ? _cam.aspect : (float)Screen.width / Screen.height;
            float viewW     = orthoSize * 2f * aspect;
            float viewH     = orthoSize * 2f;

            float sprW = spr.bounds.size.x;
            float sprH = spr.bounds.size.y;

            _bgGo.transform.localScale = new Vector3(viewW / sprW, viewH / sprH, 1f);
        }

        private IEnumerator PlayIntro()
        {
            float startOffsetY = _cam != null ? _cam.orthographicSize * 2f : 10f;

            Vector3 startPos  = transform.position;
            Vector3 targetPos = new Vector3(startPos.x, startPos.y + startOffsetY, startPos.z);

            // 딜레이 (로비 배경 잠깐 보여주기)
            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);

            // 카메라 올라오기 (배경이 자식이라 같이 올라감)
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = Mathf.Clamp01(elapsed / duration);
                float eased = easeCurve != null && easeCurve.length > 0
                    ? easeCurve.Evaluate(t)
                    : Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            transform.position = targetPos;

            // 배경 제거 (맵 배경이 드러남)
            if (_bgGo != null) Destroy(_bgGo);

            IsComplete = true;
        }
    }
}
