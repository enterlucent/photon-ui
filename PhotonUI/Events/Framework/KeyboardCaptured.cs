using PhotonUI.Controls;

namespace PhotonUI.Events.Framework
{
    public class KeyboardCaptured(Window window, Control captured)
        : FrameworkEventArgs
    {
        public Window Window = window;
        public Control Captured = captured;
    }
}