using PhotonUI.Controls;
using PhotonUI.Events;
using PhotonUI.Interfaces.Services;
using SDL3;

namespace PhotonUI.Services
{
    public interface IGestureRecognizer
    {
        public bool NeedsUpdates { get; }

        public void Process(Window window, SDL.Event e);
        public void Update(Window window);
        public void Cancel(Window window, PlatformEventArgs e);
    }

    public class InputService(IEnumerable<IGestureRecognizer> gestureRecognizers)
        : IInputService
    {
        protected IEnumerable<IGestureRecognizer> Recognizers = gestureRecognizers;

        public void Input(Window window, SDL.Event e)
        {
            foreach (IGestureRecognizer recognizer in this.Recognizers)
                recognizer.Process(window, e);
        }
        public void Update(Window window)
        {
            foreach (IGestureRecognizer recognizer in this.Recognizers)
                if (recognizer.NeedsUpdates)
                    recognizer.Update(window);
        }
        public void CancelAll(Window window, PlatformEventArgs e)
        {
            foreach (IGestureRecognizer recognizer in this.Recognizers)
                recognizer.Cancel(window, e);
        }
    }
}