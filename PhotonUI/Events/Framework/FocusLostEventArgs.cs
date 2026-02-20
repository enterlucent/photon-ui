using PhotonUI.Controls;

namespace PhotonUI.Events.Framework
{
    public class FocusLostEventArgs(Control target)
        : FrameworkEventArgs
    {
        public Control Target = target;
    }
}