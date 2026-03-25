using UnityEngine;
using UnityEngine.InputSystem;

namespace Underdark
{
    public class MapToolCamera : MonoBehaviour
    {
        public float zoomSpeed = 2f;
        public float minSize   = 3f;
        public float maxSize   = 20f;

        private Camera  _cam;
        private Vector2 _lastMousePos;
        private bool    _isPanning;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // 줌 (마우스 휠)
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _cam.orthographicSize = Mathf.Clamp(
                    _cam.orthographicSize - scroll * zoomSpeed * 0.01f * _cam.orthographicSize,
                    minSize, maxSize);

            var kb = Keyboard.current;
            bool altHeld = kb != null && kb.leftAltKey.isPressed;

            // 패닝: 미들클릭 or Alt+좌클릭
            bool panBtn = mouse.middleButton.isPressed ||
                         (mouse.leftButton.isPressed && altHeld);

            if (panBtn)
            {
                if (!_isPanning)
                {
                    _isPanning    = true;
                    _lastMousePos = mouse.position.ReadValue();
                }
                else
                {
                    Vector2 cur   = mouse.position.ReadValue();
                    Vector2 delta = cur - _lastMousePos;
                    float   scale = _cam.orthographicSize / Screen.height * 2f;
                    transform.position -= new Vector3(delta.x * scale, delta.y * scale, 0f);
                    _lastMousePos = cur;
                }
            }
            else _isPanning = false;
        }
    }
}
