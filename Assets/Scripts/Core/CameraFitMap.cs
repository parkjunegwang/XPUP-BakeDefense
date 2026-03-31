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

            // 맵 월드 중심 계산
            // GenerateMap에서 offsetX = -(columns-1)*step*0.5f 이므로
            float centerX = 0f; // 대칭이라 항상 0
            float centerY = -(map.rows - 1) * step * 0.5f + (map.rows - 1) * step * 0.5f;
            // = 0, 역시 대칭
            // 실제로는 GridToWorld로 첫/마지막 타일 평균
            Vector3 topRight  = map.GridToWorld(map.columns - 1, map.rows - 1);
            Vector3 botLeft   = map.GridToWorld(0, 0);
            Vector3 mapCenter = (topRight + botLeft) * 0.5f;

            // 카메라 위치: 맵 중앙, UI 공간만큼 위로 올려서 맵이 UI에 가리지 않게
            // bottomUIRatio만큼 화면 하단이 UI에 가려지므로 맵을 위로 약간 이동
            float screenAspect = (float)Screen.width / Screen.height;

            // 필요한 orthographicSize 계산 (맵 전체가 들어오는 최소 크기)
            float sizeForHeight = (mapH * 0.5f + padding);
            float sizeForWidth  = (mapW * 0.5f + padding) / screenAspect;

            // 둘 중 더 큰 값 선택 (맵 전체가 보이도록)
            float orthoSize = Mathf.Max(sizeForHeight, sizeForWidth);

            // 하단 UI 보정: UI가 차지하는 화면 비율만큼 카메라를 위로 올림
            // UI 높이 = orthoSize * 2 * bottomUIRatio
            float uiWorldHeight = orthoSize * 2f * bottomUIRatio;
            Vector3 camPos = new Vector3(mapCenter.x, mapCenter.y + uiWorldHeight * 0.5f, cameraZ);

            _cam.orthographicSize = orthoSize;
            transform.position    = camPos;

            Debug.Log($"[CameraFitMap] Map={mapW:F1}x{mapH:F1} OrthoSize={orthoSize:F2} Aspect={screenAspect:F2} Pos={camPos}");
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
