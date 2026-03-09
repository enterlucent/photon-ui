using PhotonUI.Controls;

namespace PhotonUI.Events.Framework
{
    public class PointerCaptured(Window window, Control captured)
        : FrameworkEventArgs
    {
        public Window Window = window;
        public Control Captured = captured;
    }
}