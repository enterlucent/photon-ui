using PhotonUI.Controls;
using SDL3;
using System.Numerics;

namespace PhotonUI.Events.Platform
{
    public class PointerWheelEventArgs(Window window, Control source, Vector2 delta, SDL.FPoint position, SDL.Event e)
        : PlatformEventArgs(e)
    {
        public Window Window { get; } = window;
        public Control Source { get; } = source;
        public SDL.FPoint Position { get; } = position;
        public Vector2 Delta { get; } = delta;
    }
}