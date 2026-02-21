using PhotonUI.Controls;
using SDL3;

namespace PhotonUI.Interfaces.Services
{
    public interface IKeyBindingService
    {
        void HandleKeyPress(SDL.Keycode key, SDL.Keymod mod, Window? activeWindow = null, Control? focusedControl = null);
        void RegisterForControl(Control control, SDL.Keycode key, SDL.Keymod mod, Action action);
        void RegisterForWindow(Window window, SDL.Keycode key, SDL.Keymod mod, Action action);
        void RegisterGlobal(SDL.Keycode key, SDL.Keymod mod, Action action);
    }
}