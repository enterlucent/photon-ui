using PhotonUI.Events.Framework;
using PhotonUI.Events.Platform;
using SDL3;

namespace PhotonUI.Controls
{
    public record WindowTabStopEntry(int Stop, Control Control, int InsertionOrder);

    public partial class Window(IServiceProvider serviceProvider)
        : Presenter(serviceProvider)
    {
        protected IntPtr WindowBackTexture;

        protected Control? FocusedControl;
        protected Control? HoveredControl;
        protected Control? KeyboardCapturedControl;
        protected Control? MouseCapturedControl;

        protected readonly List<WindowTabStopEntry> TabStops = [];
        protected int TabStopIndex = -1;
        protected int TabStopInsertIndex = 0;

        protected List<Control> PreviousHoverPath = [];

        public IntPtr BackTexture => this.WindowBackTexture;

        public Control? Focused => this.FocusedControl;
        public Control? Hovered => this.HoveredControl;
        public Control? CapturedMouse => this.MouseCapturedControl;
        public Control? CapturedKeyboard => this.KeyboardCapturedControl;

        public Control KeyboardInputTarget =>
            this.KeyboardCapturedControl ??
            this.FocusedControl ??
            this;

        #region Window: Platform

        public IntPtr Handle { get; protected set; }
        public IntPtr Renderer { get; protected set; }

        #endregion

        #region Window: External

        protected virtual void AdvanceTabFocus(int direction)
        {
            if (this.TabStops.Count == 0)
                return;

            if (this.TabStopIndex == -1)
            {
                this.TabStopIndex = 0;
                this.SetFocus(this.TabStops[this.TabStopIndex].Control);
                return;
            }

            int attempts = 0;

            while (attempts < this.TabStops.Count)
            {
                this.TabStopIndex += direction;

                if (this.TabStopIndex >= this.TabStops.Count)
                    this.TabStopIndex = 0;
                else if (this.TabStopIndex < 0)
                    this.TabStopIndex = this.TabStops.Count - 1;

                Control next = this.TabStops[this.TabStopIndex].Control;

                if (next.IsInteractable)
                {
                    this.SetFocus(next);

                    return;
                }

                attempts++;
            }
        }
        public virtual void TabStopForward() => this.AdvanceTabFocus(+1);
        public virtual void TabStopBackward() => this.AdvanceTabFocus(-1);

        public void GetScreenshot(string path)
        {
            ArgumentNullException.ThrowIfNull(this);

            if (this.BackTexture == IntPtr.Zero)
                throw new InvalidOperationException("BackTexture not created.");

            IntPtr renderer = this.Renderer;
            IntPtr texture = this.BackTexture;

            if (!SDL.GetTextureSize(texture, out float texWf, out float texHf))
                throw new InvalidOperationException("Failed to query texture size.");

            int texW = (int)texWf;
            int texH = (int)texHf;

            if (texW <= 0 || texH <= 0) throw new InvalidOperationException("Invalid texture size.");

            IntPtr prevTarget = SDL.GetRenderTarget(renderer);

            SDL.SetRenderTarget(renderer, texture);
            SDL.Rect fullRect = new() { X = 0, Y = 0, W = texW, H = texH };

            IntPtr surface = SDL.RenderReadPixels(renderer, fullRect);

            if (surface == IntPtr.Zero)
            {
                SDL.SetRenderTarget(renderer, prevTarget);

                throw new InvalidOperationException($"RenderReadPixels failed: {SDL.GetError()}");
            }

            SDL.SetRenderTarget(renderer, prevTarget);

            try
            {
                if (!SDL.SavePNG(surface, path))
                    throw new InvalidOperationException($"SavePNG failed: {SDL.GetError()}");
            }
            finally
            {
                try
                { SDL.DestroySurface(surface); }
                catch
                { try { SDL.Free(surface); } catch { } }
            }
        }
        public Control GetMouseFocus(SDL.MouseMotionEvent motion)
        {
            Control? c = Photon.ResolveHitControl(this, motion.X, motion.Y);

            return this.MouseCapturedControl ??
                this.HoveredControl ??
                c ??
                this;
        }

        #endregion

        #region Window: Internal

        public virtual void SetFocus(Control? control)
        {
            if (this.FocusedControl == control) return;

            this.FocusedControl?.OnEvent(this, new FocusLostEventArgs(this.FocusedControl));
            this.FocusedControl = control;
            this.FocusedControl?.OnEvent(this, new FocusGainedEventArgs(this.FocusedControl));

            if (control != null)
            {
                int idx = this.TabStops.FindIndex(x => x.Control == control);

                if (idx >= 0)
                    this.TabStopIndex = idx;
            }
        }
        public virtual void SetTabStop(Control control, int stop)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (stop < 0)
            {
                this.TabStops.RemoveAll(x => x.Control == control);

                if (this.TabStopIndex >= this.TabStops.Count)
                    this.TabStopIndex = this.TabStops.Count - 1;

                return;
            }

            this.TabStops.RemoveAll(x => x.Control == control);
            this.TabStops.Add(new WindowTabStopEntry(stop, control, this.TabStopInsertIndex++));
            this.TabStops.Sort((a, b) =>
            {
                int cmp = a.Stop.CompareTo(b.Stop);
                if (cmp != 0) return cmp;
                return b.InsertionOrder.CompareTo(a.InsertionOrder);
            });

            Control? focused = this.Focused;

            if (focused != null)
            {
                int idx = this.TabStops.FindIndex(x => x.Control == focused);

                if (idx >= 0)
                {
                    this.TabStopIndex = idx;

                    return;
                }
            }

            if (this.TabStops.Count > 0)
                this.TabStopIndex = this.TabStops.Count - 1;
            else
                this.TabStopIndex = -1;
        }
        public virtual void CaptureMouse(Control control)
        {
            ArgumentNullException.ThrowIfNull(control, nameof(control));

            this.ReleaseMouse();

            this.MouseCapturedControl = control;

            control.OnEvent(this, new MouseCaptured(this, control));
        }
        public virtual void ReleaseMouse()
        {
            if (this.MouseCapturedControl == null) return;

            Control released = this.MouseCapturedControl;

            this.MouseCapturedControl = null;

            released.OnEvent(this, new MouseReleased(this, released));
        }

        public virtual void CaptureKeyboard(Control control)
        {
            ArgumentNullException.ThrowIfNull(control, nameof(control));

            this.ReleaseKeyboard();

            this.KeyboardCapturedControl = control;

            control.OnEvent(this, new KeyboardCaptured(this, control));
        }
        public virtual void ReleaseKeyboard()
        {
            if (this.KeyboardCapturedControl == null) return;

            Control released = this.KeyboardCapturedControl;

            this.KeyboardCapturedControl = null;

            released.OnEvent(this, new KeyboardReleased(this, released));
        }

        #endregion

        #region Window: Framework

        protected virtual void MouseMotionHandler(Window window, SDL.Event e)
        {
            if (e.Type != (uint)SDL.EventType.MouseMotion)
                return;

            SDL.MouseMotionEvent motion = e.Motion;

            Control? target = Photon.ResolveHitControl(window, motion.X, motion.Y);

            this.HoveredControl?.OnEvent(this, new FocusLostEventArgs(this.HoveredControl));
            this.HoveredControl = null;

            List<Control> currentPath = target != null ? Photon.GetAncestors(target) : [];

            bool focusSet = false;

            for (int i = currentPath.Count - 1; i >= 0; i--)
            {
                Control control = currentPath[i];

                if (control.FocusOnHover == true && !focusSet)
                {
                    this.HoveredControl = control;
                    this.HoveredControl.OnEvent(this, new FocusGainedEventArgs(this.HoveredControl));

                    focusSet = true;
                }
            }

            foreach (Control? entered in currentPath.Except(this.PreviousHoverPath))
                entered?.OnEvent(window, new MouseEnterEventArgs(window, entered, e));

            foreach (Control? exited in this.PreviousHoverPath.Except(currentPath))
                exited?.OnEvent(window, new MouseExitEventArgs(window, exited, e));

            this.PreviousHoverPath = currentPath;
        }
        protected virtual void MouseButtonHandler(Window window, SDL.Event e)
        {
            if (e.Type != (uint)SDL.EventType.MouseButtonDown &&
                e.Type != (uint)SDL.EventType.MouseButtonUp)
                return;

            Control target = this.GetMouseFocus(e.Motion);

            List<Control>? path = Photon.GetControlPath(window, c => c == target);

            if (target != null && path != null)
            {
                // Preview phase
                MouseClickEventArgs previewArgs = new(window, target, e)
                {
                    Preview = true
                };
                foreach (Control control in path)
                    control?.OnEvent(window, previewArgs);

                // Bubble phase
                MouseClickEventArgs bubbleArgs = new(window, target, e);
                target.FrameworkEventBubble(window, bubbleArgs);
            }
        }
        protected virtual void MouseWheelHandler(Window window, SDL.Event e)
        {
            if (e.Type != (uint)SDL.EventType.MouseWheel)
                return;

            Control target = this.GetMouseFocus(e.Motion);

            List<Control>? path = Photon.GetControlPath(window, c => c == target);

            if (target != null && path != null)
            {
                // Preview phase
                MouseWheelEventArgs previewArgs = new(window, target, e)
                {
                    Preview = true
                };
                foreach (Control control in path)
                    control?.OnEvent(window, previewArgs);

                // Bubble phase
                MouseWheelEventArgs bubbleArgs = new(window, target, e);

                target.FrameworkEventBubble(window, bubbleArgs);
            }
        }

        #endregion
    }
}