using PhotonUI.Controls;
using SDL3;

namespace PhotonUI.Events.Platform
{
    public class PointerPressEventArgs(Window window, Control pressed, int clickCount, SDL.FPoint position, SDL.Event e)
        : PlatformEventArgs(e)
    {
        public Window Window { get; } = window;
        public Control Pressed { get; } = pressed;
        public int ClickCount { get; } = clickCount;
        public SDL.FPoint Position { get; } = position;
    }
}