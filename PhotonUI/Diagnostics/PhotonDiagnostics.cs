using PhotonUI.Diagnostics.Events;
using System.Diagnostics;
using System.Reflection;

namespace PhotonUI.Diagnostics
{
    public interface IPhotonDiagnostics
    {
        void OnEvent(DiagnosticEventArgs e);
    }

    public class PhotonDiagnostics
    {
        public static IPhotonDiagnostics? Sink;

        [Conditional("PHOTON_DIAGNOSTICS")]
        public static void Emit(DiagnosticEventArgs e)
        {
            e.StackFrame = new StackFrame(1, true);
            e.MethodInfo = e.StackFrame.GetMethod() as MethodInfo;

            Sink?.OnEvent(e);
        }
    }
}