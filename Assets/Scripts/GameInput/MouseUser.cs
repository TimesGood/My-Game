
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInput{

    public enum MouseButton {
        Left, Right
    }

    //用户鼠标输入处理

    public class MouseUser : MonoBehaviour {
        private InputActions _inputActions;

        public Vector2 MousePosition { get; private set; }//鼠标位置（世界坐标）
        public Vector2 MouseInWorldPosition => Camera.main.ScreenToWorldPoint(MousePosition);//鼠标位置（单元格）
        private bool _isLeftMouseButtonPressed;
        private bool _isRightMouseButtonPressed;

        //示例
        //public event Action<InputAction.CallbackContext> xxx {
        //    add {
        //        _inputActions.Player.Xxx.performed += value;
        //    }
        //    remove {
        //        _inputActions.Player.Xxx.performed -= value;
        //    }
        //}

        private void OnEnable() {
            _inputActions = InputActions.Instance;
            //订阅事件
            _inputActions.World.MousePosition.performed += OnMousePositionPerformed;
            _inputActions.World.PreformAction.performed += OnPerformActionPerformed;
            _inputActions.World.PreformAction.canceled += OnPerformActionCanceled;
            _inputActions.World.CancelAction.performed += OnCancelActionPerformed;
            _inputActions.World.CancelAction.canceled += OnCancelActionCanceled;
        }

        private void OnDisable() {
            _inputActions.World.MousePosition.performed -= OnMousePositionPerformed;
            _inputActions.World.PreformAction.performed -= OnPerformActionPerformed;
            _inputActions.World.PreformAction.canceled -= OnPerformActionCanceled;
            _inputActions.World.CancelAction.performed -= OnCancelActionPerformed;
            _inputActions.World.CancelAction.canceled -= OnCancelActionCanceled;
        }
        //鼠标移动事件
        private void OnMousePositionPerformed(InputAction.CallbackContext ctx) {
            MousePosition = ctx.ReadValue<Vector2>();
        }
        //左键按下（注：具体是什么键位主要由InputSystemPackage菜单栏中所绑定为主）
        private void OnPerformActionPerformed(InputAction.CallbackContext ctx) {
            _isLeftMouseButtonPressed = true;
        }
        //左键松开
        private void OnPerformActionCanceled(InputAction.CallbackContext ctx) {
            _isLeftMouseButtonPressed = false;
        }
        //右键按下
        private void OnCancelActionPerformed(InputAction.CallbackContext ctx) {
            _isRightMouseButtonPressed = true;
        }
        //右键松开
        private void OnCancelActionCanceled(InputAction.CallbackContext ctx) {
            _isRightMouseButtonPressed = false;
        }

        //指定按钮是否被按下
        public bool IsMouseButtonPressed(MouseButton button) {
            return button == MouseButton.Left ? _isLeftMouseButtonPressed : _isRightMouseButtonPressed;
        }

        //移动输入
        public Vector2 MovementInput() {
            return _inputActions.Player.Move.ReadValue<Vector2>();
        }
    }

}

