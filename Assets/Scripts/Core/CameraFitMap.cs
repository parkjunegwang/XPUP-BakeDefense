using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 카메라를 맵 전체가 화면에 꽉 차도록 자동 조정.
    /// MapManager.GenerateMap() 완료 후 호출하거나, Start()에서 자동 실행.
    /// 
    /// 동작:
    /// - 맵의 월드 크기를 계산
    /// - 화면 비율에 맞게 orthographicSize 조정 (맵이 항상 화면 안에 들어오도록)
    /// - 카메라 위치를 맵 중앙으로 이동
    /// - 여백(padding) 옵션으로 약간의 공간 확보 가능
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFitMap : MonoBehaviour
    {
        [Header("Fit Settings")]
        [Tooltip("맵 가장자리 여백 (월드 유닛)")]
        public float padding = 0.3f;

        [Tooltip("카메라 Z 위치")]
        public float cameraZ = -10f;

        [Tooltip("맵 하단 UI 영역 보정 (화면 하단 % - 인벤토리 패널 공간)")]
        [Range(0f, 0.4f)]
        public float bottomUIRatio = 0.13f;

        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
        }

        private void Start()
        {
            // MapManager가 Start()에서 GenerateMap을 호출하므로
            // 한 프레임 뒤에 맞추기
            StartCoroutine(FitNextFrame());
        }

        private System.Collections.IEnumerator FitNextFrame()
        {
            yield return null;
            FitToMap();
        }

public void FitToMap()
        {
            var map = MapManager.Instance;
            if (map == null) return;

            float step    = map.tileSize + map.tileGap;
            float mapW    = map.columns * step;
            float mapH    = map.rows    * step;

            Vector3 topRight  = map.GridToWorld(map.columns - 1, map.rows - 1);
            Vector3 botLeft   = map.GridToWorld(0, 0);
            Vector3 mapCenter = (topRight + botLeft) * 0.5f;

            float screenAspect = (float)Screen.width / Screen.height;

            float sizeForHeight = (mapH * 0.5f + padding);
            float sizeForWidth  = (mapW * 0.5f + padding) / screenAspect;
            float orthoSize     = Mathf.Max(sizeForHeight, sizeForWidth);

            float uiWorldHeight = orthoSize * 2f * bottomUIRatio;
            Vector3 camPos = new Vector3(mapCenter.x, mapCenter.y - uiWorldHeight * 0.5f, cameraZ);

            _cam.orthographicSize = orthoSize;
            transform.position    = camPos;

            Debug.Log($"[CameraFitMap] OrthoSize={orthoSize:F2} Aspect={screenAspect:F2}");

            // 카메라 크기 확정 후 배경 생성
            var bg = GetComponent<MapBackground>();
            if (bg != null) bg.CreateBackground();
        }

#if UNITY_EDITOR
        // 에디터에서 해상도 바꿀 때도 즉시 반영
        private void OnValidate()
        {
            if (Application.isPlaying && MapManager.Instance != null)
                FitToMap();
        }
#endif
    }
}
