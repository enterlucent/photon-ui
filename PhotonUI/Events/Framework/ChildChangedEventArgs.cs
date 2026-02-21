using PhotonUI.Controls;

namespace PhotonUI.Events.Framework
{
    public enum ChildChangeAction
    {
        Added,
        Removed
    }

    public class ChildChangedEventArgs(Control parent, Control child, ChildChangeAction action) : FrameworkEventArgs
    {
        public Control Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));
        public Control Child { get; } = child ?? throw new ArgumentNullException(nameof(child));
        public ChildChangeAction Action { get; } = action;
    }
}