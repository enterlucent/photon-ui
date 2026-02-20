using PhotonUI.Controls;
using SDL3;

namespace PhotonUI.Events.Platform
{
    public class MouseClickEventArgs(Window window, Control clicked, SDL.Event e)
        : PlatformEventArgs(e)
    {
        public Window Window = window;
        public Control Clicked = clicked;
    }
}