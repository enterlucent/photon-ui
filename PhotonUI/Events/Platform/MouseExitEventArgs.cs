using PhotonUI.Controls;
using SDL3;

namespace PhotonUI.Events.Platform
{
    public class MouseExitEventArgs(Window window, Control exited, SDL.Event e)
        : PlatformEventArgs(e)
    {
        public Window Window = window;
        public Control Exited = exited;
    }
}