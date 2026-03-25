using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 에디터 시작 시 흰 사각형 스프라이트를 자동 생성해서 SpriteRenderer에 할당.
    /// Tile, 포탑, 몬스터 등 모든 스프라이트 오브젝트에 사용.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AutoSprite : MonoBehaviour
    {
        private void Awake()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null)
                sr.sprite = CreateWhiteSquare();
        }

        public static Sprite CreateWhiteSquare(int size = 32)
        {
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
