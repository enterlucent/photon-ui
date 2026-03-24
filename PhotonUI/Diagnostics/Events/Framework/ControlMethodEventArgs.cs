using PhotonUI.Controls;

namespace PhotonUI.Diagnostics.Events.Framework
{
    public class ControlMethodEventArgs(Control control, List<object?>? parameters = null, DiagnosticPhase phase = DiagnosticPhase.Atomic)
        : ControlEventArgs(control, phase)
    {
        public List<object?> Parameters { get; } = parameters ?? [];
    }
}