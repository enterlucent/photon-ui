using PhotonUI.Controls;
using SDL3;

namespace PhotonUI.Events.Platform
{
    public class MouseWheelEventArgs(Window window, Control wheeled, SDL.Event e)
        : PlatformEventArgs(e)
    {
        public Window Window = window;
        public Control Wheeled = wheeled;
    }
}