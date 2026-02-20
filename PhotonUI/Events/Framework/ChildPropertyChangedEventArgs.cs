using PhotonUI.Controls;
using System.ComponentModel;

namespace PhotonUI.Events.Framework
{
    public class ChildPropertyChangedEventArgs(Control child, PropertyChangedEventArgs propertyArgs) : FrameworkEventArgs
    {
        public Control Child { get; } = child
            ?? throw new ArgumentNullException(nameof(child));
        public PropertyChangedEventArgs PropertyArgs { get; } = propertyArgs
            ?? throw new ArgumentNullException(nameof(propertyArgs));
        public string PropertyName { get; } = propertyArgs.PropertyName
            ?? string.Empty;
    }
}