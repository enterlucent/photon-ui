using PhotonUI.Controls;

namespace PhotonUI.Events.Framework
{
    public class FocusGainedEventArgs(Control target)
        : FrameworkEventArgs
    {
        public Control Target = target;
    }
}