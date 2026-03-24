namespace PhotonUI.Diagnostics.Events.Framework
{
    public class PhotonMethodEventArgs(List<object?>? parameters = null, DiagnosticPhase phase = DiagnosticPhase.Atomic)
        : DiagnosticEventArgs(phase)
    {
        public List<object?> Parameters { get; } = parameters ?? [];
    }
}