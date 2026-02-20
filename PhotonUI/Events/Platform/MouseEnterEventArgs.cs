using PhotonUI.Controls;
using SDL3;

namespace PhotonUI.Events.Platform
{
    public class MouseEnterEventArgs(Window window, Control entered, SDL.Event e)
        : PlatformEventArgs(e)
    {
        public Window Window = window;
        public Control Entered = entered;
    }
}