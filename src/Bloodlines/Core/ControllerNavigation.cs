using System;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    public enum MenuDirection { None, Up, Down, Left, Right }

    /// <summary>One step on deflection, then a delayed repeat; ignores stick drift.</summary>
    public sealed class ControllerNavigation
    {
        private MenuDirection _held;
        private int _nextRepeat;
        public void Reset() { _held = MenuDirection.None; _nextRepeat = 0; }
        public MenuDirection Update(float x, float y, int now, bool up = false, bool down = false, bool left = false, bool right = false)
        {
            if (up || down || left || right)
            {
                x = (right ? 1f : 0f) - (left ? 1f : 0f);
                y = (down ? 1f : 0f) - (up ? 1f : 0f);
            }
            var direction = Math.Max(Math.Abs(x), Math.Abs(y)) < 0.55f ? MenuDirection.None :
                Math.Abs(y) >= Math.Abs(x) ? (y < 0 ? MenuDirection.Up : MenuDirection.Down) :
                (x < 0 ? MenuDirection.Left : MenuDirection.Right);
            if (direction == MenuDirection.None) { Reset(); return direction; }
            if (direction != _held) { _held = direction; _nextRepeat = now + 400; return direction; }
            if (now < _nextRepeat) return MenuDirection.None;
            _nextRepeat = now + 140;
            return direction;
        }
    }

    public static class ControllerInput
    {
        public static bool Pressed(Control control) =>
            Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, (int)control) || Game.IsControlPressed(control);
        public static bool JustPressed(Control control) =>
            Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, (int)control) || Game.IsControlJustPressed(control);
        public static float Axis(Control control) => Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, (int)control);
    }
}
