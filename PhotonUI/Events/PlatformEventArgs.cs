using SDL3;

namespace NucleusAF.Photon.Events
{
    public class PlatformEventArgs(SDL.Event nativeEvent)
        : FrameworkEventArgs
    {
        public SDL.Event NativeEvent { get; } = nativeEvent;
    }
}