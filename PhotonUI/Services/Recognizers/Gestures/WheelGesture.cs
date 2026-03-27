using PhotonUI.Controls;
using PhotonUI.Events;
using PhotonUI.Events.Platform;
using SDL3;
using System.Numerics;

namespace PhotonUI.Services.Recognizers.Gestures
{
    public class WheelGesture : IGestureRecognizer
    {
        public bool NeedsUpdates => false;

        public void Process(Window window, SDL.Event e)
        {
            if (e.Type != (uint)SDL.EventType.MouseWheel)
                return;

            SDL.GetMouseState(out float mouseX, out float mouseY);

            SDL.FPoint position = new() { X = mouseX, Y = mouseY };
            Vector2 delta = new() { X = e.Wheel.X, Y = e.Wheel.Y };

            Control? target = Photon.ResolveHitControl(window, mouseX, mouseY);

            if (target == null)
                return;

            PointerWheelEventArgs args = new(window, target, delta, position, e);

            window.DispatchToControl(args, target);
        }
        public void Update(Window window) { }
        public void Cancel(Window window, PlatformEventArgs e) { }
    }
}