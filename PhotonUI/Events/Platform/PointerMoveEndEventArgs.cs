using PhotonUI.Controls;
using PhotonUI.Models;
using SDL3;
using System.Numerics;

namespace PhotonUI.Events.Platform
{
    public class PointerMoveEndEventArgs(Window window, InputDeviceKey device, Control source, Control current, Vector2 delta, SDL.FPoint position, SDL.Event e)
        : PlatformEventArgs(e)
    {
        public Window Window { get; } = window;
        public InputDeviceKey DeviceKey { get; } = device;
        public Control Source { get; } = source;
        public SDL.FPoint Position { get; } = position;
        public Control Current { get; } = current;
        public Vector2 Delta { get; } = delta;
    }
}