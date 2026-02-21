using PhotonUI.Controls;
using PhotonUI.Interfaces.Services;
using SDL3;

namespace PhotonUI.Services
{
    public class KeyBindingService : IKeyBindingService
    {
        private readonly Dictionary<(SDL.Keycode, SDL.Keymod), Action> globalBindings = [];
        private readonly Dictionary<Window, Dictionary<(SDL.Keycode, SDL.Keymod), Action>> windowBindings = [];
        private readonly Dictionary<Control, Dictionary<(SDL.Keycode, SDL.Keymod), Action>> controlBindings = [];

        public void RegisterGlobal(SDL.Keycode key, SDL.Keymod mod, Action action)
            => this.globalBindings[(key, mod)] = action;

        public void RegisterForWindow(Window window, SDL.Keycode key, SDL.Keymod mod, Action action)
        {
            if (!this.windowBindings.TryGetValue(window, out Dictionary<(SDL.Keycode, SDL.Keymod), Action>? map))
                this.windowBindings[window] = map = [];
            map[(key, mod)] = action;
        }

        public void RegisterForControl(Control control, SDL.Keycode key, SDL.Keymod mod, Action action)
        {
            if (!this.controlBindings.TryGetValue(control, out Dictionary<(SDL.Keycode, SDL.Keymod), Action>? map))
                this.controlBindings[control] = map = [];
            map[(key, mod)] = action;
        }

        public void HandleKeyPress(SDL.Keycode key, SDL.Keymod mod, Window? activeWindow = null, Control? focusedControl = null)
        {
            //Debug.WriteLine($"HandleKeyPress: key={key}, rawMod={mod}");

            // Control‑scoped
            if (focusedControl != null &&
                this.controlBindings.TryGetValue(focusedControl, out Dictionary<(SDL.Keycode, SDL.Keymod), Action>? ctrlMap))
            {
                foreach ((SDL.Keycode, SDL.Keymod) binding in ctrlMap.Keys)
                {
                    SDL.Keymod normalized = NormalizeModifiers(mod, binding.Item2);
                    if (binding.Item1 == key && normalized == binding.Item2)
                    {
                        //Debug.WriteLine($"Triggering control binding {key}+{binding.Item2}");
                        ctrlMap[binding].Invoke();
                        return;
                    }
                }
            }

            // Window‑scoped
            if (activeWindow != null &&
                this.windowBindings.TryGetValue(activeWindow, out Dictionary<(SDL.Keycode, SDL.Keymod), Action>? winMap))
            {
                foreach ((SDL.Keycode, SDL.Keymod) binding in winMap.Keys)
                {
                    SDL.Keymod normalized = NormalizeModifiers(mod, binding.Item2);
                    if (binding.Item1 == key && normalized == binding.Item2)
                    {
                        //Debug.WriteLine($"Triggering window binding {key}+{binding.Item2}");
                        winMap[binding].Invoke();
                        return;
                    }
                }
            }

            // Global‑scoped
            foreach ((SDL.Keycode, SDL.Keymod) binding in this.globalBindings.Keys)
            {
                SDL.Keymod normalized = NormalizeModifiers(mod, binding.Item2);
                if (binding.Item1 == key && normalized == binding.Item2)
                {
                    //Debug.WriteLine($"Triggering global binding {key}+{binding.Item2}");
                    this.globalBindings[binding].Invoke();
                    return;
                }
            }

            //Debug.WriteLine($"No binding matched for {key}+{mod}");
        }

        private static SDL.Keymod NormalizeModifiers(SDL.Keymod mod, SDL.Keymod bindingMask)
        {
            const SDL.Keymod coreMask = SDL.Keymod.Ctrl | SDL.Keymod.Shift | SDL.Keymod.Alt | SDL.Keymod.GUI;
            const SDL.Keymod lockMask = SDL.Keymod.Caps | SDL.Keymod.Num | SDL.Keymod.Scroll;

            SDL.Keymod normalized = mod & coreMask;
            SDL.Keymod requestedLocks = bindingMask & lockMask;

            if (requestedLocks != 0)
                normalized |= (mod & requestedLocks);

            return normalized;
        }
    }
}