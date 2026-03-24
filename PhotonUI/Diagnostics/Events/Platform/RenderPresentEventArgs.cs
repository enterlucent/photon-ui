using PhotonUI.Controls;
using PhotonUI.Diagnostics.Events.Framework;

namespace PhotonUI.Diagnostics.Events.Platform
{
    public class RenderPresentEventArgs(Control control, DiagnosticPhase phase = DiagnosticPhase.Atomic)
        : ControlEventArgs(control, phase)
    { }
}