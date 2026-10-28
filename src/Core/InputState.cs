using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace KnowledgeRPG.Core
{
    /// <summary>
    /// Tracks keyboard state between frames so gameplay code can ask for
    /// "just pressed" keys without repeating the bookkeeping.
    /// </summary>
    public sealed class InputState
    {
        private KeyboardState _current;
        private KeyboardState _previous;

        public void Update()
        {
            _previous = _current;
            _current = Keyboard.GetState();
        }

        public bool IsDown(Keys key) => _current.IsKeyDown(key);

        public bool WasPressed(Keys key) =>
            _current.IsKeyDown(key) && _previous.IsKeyUp(key);

        public bool AnyPressed(params Keys[] keys)
        {
            foreach (Keys key in keys)
            {
                if (WasPressed(key))
                    return true;
            }

            return false;
        }

        public bool ConfirmPressed =>
            AnyPressed(Keys.Enter, Keys.Space);

        public int ChoicePressed()
        {
            if (AnyPressed(Keys.D1, Keys.NumPad1)) return 0;
            if (AnyPressed(Keys.D2, Keys.NumPad2)) return 1;
            if (AnyPressed(Keys.D3, Keys.NumPad3)) return 2;
            if (AnyPressed(Keys.D4, Keys.NumPad4)) return 3;
            return -1;
        }

        public IReadOnlyList<Keys> HeldKeys
        {
            get
            {
                var keys = new List<Keys>();
                foreach (Keys key in _current.GetPressedKeys())
                    keys.Add(key);
                return keys;
            }
        }
    }
}
