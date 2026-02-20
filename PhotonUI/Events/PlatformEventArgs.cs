using SDL3;

namespace PhotonUI.Events
{
    public class PlatformEventArgs(SDL.Event nativeEvent)
        : FrameworkEventArgs
    {
        public SDL.Event NativeEvent { get; } = nativeEvent;
    }
}