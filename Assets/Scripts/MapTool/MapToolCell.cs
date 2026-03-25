using UnityEngine;

namespace Underdark
{
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class MapToolCell : MonoBehaviour
    {
        public enum Type { Empty, Floor, Spawn, End, Wall }

        public int  GridX        { get; private set; }
        public int  GridY        { get; private set; }
        public Type CellType     { get; private set; } = Type.Empty;
        public Sprite CurrentSprite { get; private set; }
        public bool IsWallOrigin { get; private set; }
        public MapData.WallSizeType WallSize { get; private set; }

        private SpriteRenderer    _sr;
        private SpriteRenderer    _borderSr;
        private MapToolController _tool;

        static readonly Color ColEmpty  = new Color(0.18f, 0.18f, 0.28f);
        static readonly Color ColSpawn  = new Color(0.1f,  0.7f,  0.2f);
        static readonly Color ColEnd    = new Color(0.8f,  0.1f,  0.1f);
        static readonly Color ColWall   = new Color(0.55f, 0.45f, 0.35f);
        static readonly Color ColFloor  = new Color(0.38f, 0.38f, 0.52f);
        static readonly Color ColBorder = new Color(0.5f,  0.5f,  0.7f, 0.6f);

        public void Init(int x, int y, MapToolController tool)
        {
            GridX = x; GridY = y; _tool = tool;
            _sr = GetComponent<SpriteRenderer>();
            BuildBorder();
            RefreshVisual();
        }

        /// <summary>타일 테두리 오브젝트 생성</summary>
        private void BuildBorder()
        {
            // 이미 있으면 스킵
            var existing = transform.Find("Border");
            if (existing != null) { _borderSr = existing.GetComponent<SpriteRenderer>(); return; }

            var go = new GameObject("Border");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            // 타일보다 살짝 크게 → 테두리처럼 보임
            go.transform.localScale = Vector3.one * 1.0f;

            _borderSr = go.AddComponent<SpriteRenderer>();
            _borderSr.sprite       = MakeWhiteSquare();
            _borderSr.color        = ColBorder;
            _borderSr.sortingOrder = -1; // 타일 뒤에
        }

        private Sprite MakeWhiteSquare()
        {
            var tex = new Texture2D(4, 4);
            var pix = new Color32[16];
            for (int i = 0; i < pix.Length; i++) pix[i] = Color.white;
            tex.SetPixels32(pix); tex.Apply();
            return Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f), 4f);
        }

        public void SetFloor(Sprite sprite) { CellType = Type.Floor; CurrentSprite = sprite; RefreshVisual(); }
        public void SetSpawn()              { CellType = Type.Spawn; RefreshVisual(); }
        public void SetEnd()                { CellType = Type.End;   RefreshVisual(); }

        public void SetWall(MapData.WallSizeType size, bool isOrigin)
        {
            CellType = Type.Wall; WallSize = size; IsWallOrigin = isOrigin; RefreshVisual();
        }

        public void Erase()
        {
            CellType = Type.Empty; CurrentSprite = null; IsWallOrigin = false; RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (_sr == null) return;

            // ── 배경 ──
            switch (CellType)
            {
                case Type.Empty:
                    _sr.sprite = null; _sr.color = ColEmpty; break;
                case Type.Floor:
                    _sr.sprite = CurrentSprite;
                    _sr.color  = CurrentSprite != null ? Color.white : ColFloor;
                    break;
                case Type.Spawn: _sr.sprite = null; _sr.color = ColSpawn; break;
                case Type.End:   _sr.sprite = null; _sr.color = ColEnd;   break;
                case Type.Wall:
                    _sr.sprite = null;
                    _sr.color  = ColWall * (IsWallOrigin ? 1.0f : 0.75f);
                    break;
            }

            // ── 테두리 색 ── (타입별로 조금씩 다르게)
            if (_borderSr != null)
            {
                _borderSr.color = CellType switch
                {
                    Type.Spawn => new Color(0.05f, 0.5f,  0.1f,  0.8f),
                    Type.End   => new Color(0.6f,  0.05f, 0.05f, 0.8f),
                    Type.Wall  => new Color(0.3f,  0.25f, 0.15f, 0.8f),
                    Type.Floor => new Color(0.25f, 0.25f, 0.4f,  0.5f),
                    _          => ColBorder,
                };
            }

            UpdateLabel();
        }

        private void UpdateLabel()
        {
            var existing = transform.Find("Label");
            bool needLabel = CellType == Type.Spawn || CellType == Type.End ||
                             (CellType == Type.Wall && IsWallOrigin);

            if (!needLabel) { if (existing != null) Destroy(existing.gameObject); return; }

            GameObject go = existing != null ? existing.gameObject : null;
            if (go == null)
            {
                go = new GameObject("Label");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale    = Vector3.one * 0.48f;
                var tmp = go.AddComponent<TMPro.TextMeshPro>();
                tmp.alignment  = TMPro.TextAlignmentOptions.Center;
                tmp.fontSize   = 3f;
                tmp.color      = Color.white;
                tmp.sortingOrder = 5;
            }

            var t = go.GetComponent<TMPro.TextMeshPro>();
            if (t == null) return;
            t.text = CellType switch
            {
                Type.Spawn => $"S\n({GridX},{GridY})",
                Type.End   => $"E\n({GridX},{GridY})",
                Type.Wall  => WallSize switch
                {
                    MapData.WallSizeType.Wall1x1 => "W1",
                    MapData.WallSizeType.Wall2x1 => "2x1",
                    MapData.WallSizeType.Wall1x2 => "1x2",
                    MapData.WallSizeType.Wall2x2 => "2x2",
                    _ => "W"
                },
                _ => ""
            };
        }
    }
}
