//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Input;


namespace BlockPuzzle {

    public enum InputAction {
        Up,
        Down,
        Left,
        Right,
        ExchangeBlock,
        RequestLine,
        Confirm,
        Cancel,
        DebugAction1,
        DebugAction2,
        DebugAction3,
        DebugAction4
    }

    /// <summary>
    /// Simple input manager that maps keyboard input to actions.  That way
    /// more than one key can trigger the same action and we won't trigger
    /// a key that was pressed more than once.
    /// </summary>
    public class InputManager {
        private Game game;
        private Dictionary<Key, InputAction> keyBindings;
        private Dictionary<InputAction, Key> keyPressed;

        public InputManager(Game game)
        {
            keyBindings = new Dictionary<Key, InputAction>();
            keyPressed = new Dictionary<InputAction, Key>();
            game.Keyboard.KeyUp += HandleKeyboardKeyUp;
            game.Keyboard.KeyDown += HandleKeyboardKeyDown;
            this.game = game;
        }

        ~InputManager()
        {
            game.Keyboard.KeyUp -= HandleKeyboardKeyUp;
            game.Keyboard.KeyDown -= HandleKeyboardKeyDown;
        }

        void HandleKeyboardKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            InputAction associatedAction;
            if (!keyBindings.TryGetValue(e.Key, out associatedAction) ||
                keyPressed.ContainsKey(associatedAction))
                return;

            keyPressed[associatedAction] = e.Key;
            game.OnActionTriggered(associatedAction);
        }

        void HandleKeyboardKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            InputAction associatedAction;
            if (!keyBindings.TryGetValue(e.Key, out associatedAction))
                return;
            Key triggeredKey;
            if (!keyPressed.TryGetValue(associatedAction, out triggeredKey) ||
                triggeredKey != e.Key)
                return;
            keyPressed.Remove(associatedAction);
            game.OnActionReleased(associatedAction);
        }

        public bool this[InputAction action] {
            get { return keyPressed.ContainsKey(action); }
        }

        public void Update(FrameEventArgs e)
        {
        }

        public void LoadDefaults()
        {
            BindKey(Key.Up, InputAction.Up);
            BindKey(Key.W, InputAction.Up);
            BindKey(Key.Down, InputAction.Down);
            BindKey(Key.S, InputAction.Down);
            BindKey(Key.Left, InputAction.Left);
            BindKey(Key.A, InputAction.Left);
            BindKey(Key.Right, InputAction.Right);
            BindKey(Key.D, InputAction.Right);
            BindKey(Key.Enter, InputAction.Confirm);
            BindKey(Key.Escape, InputAction.Cancel);
            BindKey(Key.Space, InputAction.ExchangeBlock);
            BindKey(Key.ShiftLeft, InputAction.RequestLine);
            BindKey(Key.ShiftRight, InputAction.RequestLine);
            BindKey(Key.ControlLeft, InputAction.RequestLine);
            BindKey(Key.ControlRight, InputAction.RequestLine);
            BindKey(Key.Number1, InputAction.DebugAction1);
            BindKey(Key.Number2, InputAction.DebugAction2);
            BindKey(Key.Number3, InputAction.DebugAction3);
            BindKey(Key.Number4, InputAction.DebugAction4);
        }

        public void BindKey(Key key, InputAction action)
        {
            keyBindings[key] = action;
        }
    }
}

