using PhotonUI.Controls;

namespace PhotonUI.Events.Framework
{
    public class PointerReleased(Window window, Control released)
        : FrameworkEventArgs
    {
        public Window Window = window;
        public Control Released = released;
    }
}