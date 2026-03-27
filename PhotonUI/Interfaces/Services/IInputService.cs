using PhotonUI.Controls;
using PhotonUI.Events;
using SDL3;

namespace PhotonUI.Interfaces.Services
{
    public interface IInputService
    {
        public void Input(Window window, SDL.Event e);
        public void Update(Window window);
        public void CancelAll(Window window, PlatformEventArgs e);
    }
}