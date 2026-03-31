using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 맵 크기에 딱 맞는 배경 스프라이트를 자동 생성.
    /// MapManager.GenerateMap() 완료 후 CameraFitMap과 함께 Start()에서 호출됨.
    /// 
    /// 사용법:
    /// 1. GameScene Main Camera에 이 컴포넌트 추가
    /// 2. backgroundSpriteName에 Resources/Image 안의 파일명 입력 (확장자 제외)
    ///    예: "bg_dungeon" → Assets/Resources/Image/bg_dungeon.png
    /// 3. 없으면 단색 배경으로 폴백
    /// </summary>
    public class MapBackground : MonoBehaviour
    {
        [Header("Background Settings")]
        [Tooltip("Resources/Image/ 안의 파일명 (확장자 제외). 비워두면 단색 배경.")]
        public string backgroundSpriteName = "";

        [Tooltip("단색 배경 색상 (스프라이트 없을 때 사용)")]
        public Color fallbackColor = new Color(0.08f, 0.06f, 0.12f); // 어두운 던전 느낌

        [Tooltip("맵 가장자리 추가 여백 (월드 유닛)")]
        public float extraPadding = 0.0f;

        [Tooltip("배경 정렬 순서 (타일보다 낮게 - 기본 -10)")]
        public int sortingOrder = -10;

        private GameObject _bgObject;

        private void Start()
        {
            StartCoroutine(CreateNextFrame());
        }

        private System.Collections.IEnumerator CreateNextFrame()
        {
            // MapManager가 GenerateMap을 마칠 때까지 대기
            yield return null;
            yield return null; // 한 프레임 더 (CameraFitMap과 동일 타이밍)
            CreateBackground();
        }

        public void CreateBackground()
        {
            var map = MapManager.Instance;
            if (map == null) return;

            // 기존 배경 제거
            if (_bgObject != null) Destroy(_bgObject);

            // 맵 월드 크기 계산
            float step = map.tileSize + map.tileGap;
            float mapW = map.columns * step + extraPadding * 2f;
            float mapH = map.rows    * step + extraPadding * 2f;

            // 맵 중앙 좌표
            Vector3 topRight = map.GridToWorld(map.columns - 1, map.rows - 1);
            Vector3 botLeft  = map.GridToWorld(0, 0);
            Vector3 center   = (topRight + botLeft) * 0.5f;
            center.z = 1f; // 타일보다 뒤

            // 배경 오브젝트 생성
            _bgObject = new GameObject("MapBackground");
            _bgObject.transform.position = center;

            var sr = _bgObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            // 스프라이트 로드
            Sprite sprite = null;
            if (!string.IsNullOrEmpty(backgroundSpriteName))
                sprite = Resources.Load<Sprite>($"Image/{backgroundSpriteName}");

            if (sprite != null)
            {
                sr.sprite = sprite;
                // 스프라이트를 맵 크기에 딱 맞게 스케일 조정
                float spriteW = sr.sprite.bounds.size.x;
                float spriteH = sr.sprite.bounds.size.y;
                float scaleX  = mapW / spriteW;
                float scaleY  = mapH / spriteH;
                _bgObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                Debug.Log($"[MapBackground] Sprite '{backgroundSpriteName}' fitted: {mapW:F2}x{mapH:F2}");
            }
            else
            {
                // 스프라이트 없으면 단색 사각형
                sr.sprite = GameSetup.WhiteSquareStatic();
                sr.color  = fallbackColor;
                _bgObject.transform.localScale = new Vector3(mapW, mapH, 1f);
                Debug.Log($"[MapBackground] No sprite → fallback color {fallbackColor}");
            }
        }

        /// <summary>게임 중 배경 스프라이트 교체</summary>
        public void SetBackground(string spriteName)
        {
            backgroundSpriteName = spriteName;
            CreateBackground();
        }
    }
}
