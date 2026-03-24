using PhotonUI.Controls;

namespace PhotonUI.Diagnostics.Events.Framework
{
    public class ControlEventArgs(Control control, DiagnosticPhase phase = DiagnosticPhase.Atomic)
        : DiagnosticEventArgs(phase)
    {
        public Control Control { get; } = control;
    }
}
