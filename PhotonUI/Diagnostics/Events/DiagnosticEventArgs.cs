using System.Diagnostics;
using System.Reflection;

namespace PhotonUI.Diagnostics.Events
{
    public enum DiagnosticPhase
    {
        Start,
        End,
        Atomic
    }

    public abstract class DiagnosticEventArgs(DiagnosticPhase phase = DiagnosticPhase.Atomic)
        : EventArgs
    {
        public MethodInfo? MethodInfo = null;
        public StackFrame? StackFrame = null;

        public DiagnosticPhase Phase { get; } = phase;
    }
}