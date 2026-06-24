using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Utilities
{
    /// <summary>
    /// PC 플랫폼에서 사용하눈 키보드, 마우스 입력을 묶어놓은 유틸 클래스 
    /// 목표: 쉽고 직관적으로 사용할 수 있도록 기능들을 묶어 놓는 것
    /// </summary>
    // 아래처럼 작업하는 디자인 패턴을 Wrapper 패턴이라고 부름. "Wrapping 한다" 라고 표현함. 
    public static class SimpleInput // 어디서든 호출 가능하도록 static으로 선언
    {
        #region 키보드 입략
        public static bool GetKeyDown(Key key)
        {
            return Keyboard.current[key].wasPressedThisFrame;
        }

        public static bool GetKeyUp(Key key)
        {
            return Keyboard.current[key].wasReleasedThisFrame;
        }

        public static bool GetKey(Key key)
        {
            return Keyboard.current[key].isPressed;
        }

        public static bool AnyKeyDown()
        {
            return Keyboard.current.anyKey.wasPressedThisFrame;
        }
        #endregion

        #region 마우스 입력
        /// <summary>
        /// 마우스 버튼 종류를 정의하는 열거형
        /// </summary>
        public enum MouseButton { Left = 0, Right, Middle }

        public static ButtonControl GetMouseControl(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return Mouse.current.leftButton;
                case MouseButton.Right:
                    return Mouse.current.rightButton;
                case MouseButton.Middle:
                    return Mouse.current.middleButton;
            }

            return null;
        }

        public static bool GetMouseButtonDown(MouseButton button)
        {
            return GetMouseControl(button).wasPressedThisFrame;
        }

        public static bool GetMouseButtonUp(MouseButton button)
        {
            return GetMouseControl(button).wasReleasedThisFrame;
        }

        public static bool GetMouseButton(MouseButton button)
        {
            return GetMouseControl(button).isPressed;
        }

        public static Vector2 GetMousePosition()
        {
            return Mouse.current.position.ReadValue();
        }

        /// <returns>마우스가 1프레임에 이동한 위치 변화량 반환</returns>
        public static Vector2 GetMouseDelta()
        {
            return Mouse.current.delta.ReadValue();
        }

        public static float GetMouseScrollDeltaRaw()
        {
            return Mouse.current.scroll.ReadValue().y;
        }

        /// <returns>마우스 휠 스크롤을 -1, 0, 1 단위로 반환</returns>
        public static float GetMouseScrollDelta()
        {
            float raw = GetMouseScrollDeltaRaw();
            return Mathf.Approximately(raw, 0f) ? 0f : Mathf.Sign(raw);
        }
        #endregion

        #region 축 입력 (방향키)
        /// <summary>
        /// 좌/우 화살표 키 대응
        /// (레거시에서 Input.GetAxisRaw("Horizontal")과 동일)
        /// </summary>
        /// <returns>수평 축 원시 입력값을 반환 (-1, 0, 1)</returns>
        public static float GetAxisHorizontalRaw()
        {
            float axis = 0.0f;

            if (Keyboard.current.rightArrowKey.isPressed)
                axis += 1.0f;
            if (Keyboard.current.leftArrowKey.isPressed)
                axis -= 1.0f;

            return axis;
        }

        /// <summary>
        /// 상/하 화살표 키 대응
        /// (레거시에서 Input.GetAxisRaw("Vertical")과 동일)
        /// </summary>
        /// <returns>수직 축 원시 입력값을 반환 (-1, 0, 1)</returns>
        public static float GetAxisVerticalRaw()
        {
            float axis = 0.0f;

            if (Keyboard.current.upArrowKey.isPressed)
                axis += 1.0f;
            if (Keyboard.current.downArrowKey.isPressed)
                axis -= 1.0f;

            return axis;
        }

        /// <summary>
        /// 수평/수직 입력 Raw 입력을 합쳐 정규화된 Vector2로 반환,
        /// 대각선 입력 시 속도가 조금 빨라지는(루트2) 현상을 보정하여 반환
        /// </summary>
        public static Vector2 GetMoveAxisRaw()
        {
            Vector2 axis = new Vector2();
            axis.x = GetAxisHorizontalRaw();
            axis.y = GetAxisVerticalRaw();

            if (axis.sqrMagnitude > 1f) return axis.normalized;
            return axis;
        }
        #endregion
    }
}
