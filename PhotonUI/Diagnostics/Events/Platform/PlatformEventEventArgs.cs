using PhotonUI.Events;

namespace PhotonUI.Diagnostics.Events.Platform
{
    public class PlatformEventEventArgs(PlatformEventArgs e, DiagnosticPhase phase = DiagnosticPhase.Atomic)
        : DiagnosticEventArgs(phase)
    {
        public PlatformEventArgs Event { get; } = e;
    }
}