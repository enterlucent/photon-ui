using PhotonUI.Animations;
using PhotonUI.Diagnostics;
using PhotonUI.Diagnostics.Events;
using PhotonUI.Diagnostics.Events.Framework;
using PhotonUI.Diagnostics.Events.Platform;
using PhotonUI.Events;
using PhotonUI.Events.Framework;
using PhotonUI.Events.Platform;
using PhotonUI.Extensions;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using SDL3;
using System.Linq.Expressions;

namespace PhotonUI.Controls
{
    public enum WindowMode
    {
        None,
        Tangible,
        Logical
    }

    public partial class Window(IServiceProvider serviceProvider, IBindingService bindingService, IAnimationBuilder animationBulder)
        : Presenter(serviceProvider, bindingService)
    {
        protected IAnimationBuilder AnimationBulder = animationBulder;
        protected WindowMode WindowMode = WindowMode.Tangible;
        protected IntPtr WindowBackTexture;

        protected Control? FocusedControl;
        protected Control? PointerCapturedControl;
        protected readonly List<AnimationHandle> ActiveAnimations = [];

        public WindowMode Mode => this.WindowMode;
        public bool HasTangibleWindow => this.Mode == WindowMode.Tangible && this.Handle != IntPtr.Zero;
        public IntPtr BackTexture => this.WindowBackTexture;

        public Control? Focused => this.FocusedControl;
        public Control? CapturedControl => this.PointerCapturedControl;

        public Control KeyboardInputTarget =>
            this.FocusedControl ??
            this;

        #region Window: Platform

        public string DefaultTitle { get; protected set; } = "PhotonUI";
        public Size DefaultSize { get; protected set; } = new Size(800, 600);
        public SDL.WindowFlags DefaultFlags { get; protected set; } = SDL.WindowFlags.Resizable;
        public bool SuppressRendering { get; set; } = false;

        public IntPtr Handle { get; protected set; }
        public IntPtr Renderer { get; protected set; }

        #endregion

        #region Window: External

        public virtual Window CreateLogicalWindow(int width, int height)
        {
            if (this.Mode != WindowMode.Tangible)
                throw new InvalidOperationException("Logical windows must be created from a tangible window.");

            Window logical = this.Create<Window>();

            logical.OnInitialize(this);
            logical.WindowMode = WindowMode.Logical;
            logical.Renderer = this.Renderer;
            logical.SetWindowSize(width, height);

            return logical;
        }

        public virtual void Initialize(string title, Size size, SDL.WindowFlags flags)
        {
            this.DefaultTitle = title;
            this.DefaultSize = size;
            this.DefaultFlags = flags;

            this.OnInitialize(this);
        }
        public virtual void Tick()
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.ApplyTick();
            this.ApplyAnimationRequests();
            this.ApplyIntrinsicRequests();
            this.ApplyMeasureRequests();
            this.ApplyArrangeRequests();
            this.ApplyLateTick();

            if (this.NeedsRendering())
            {
                this.ApplyClearRequests();
                this.ApplyRenderRequests();

                SDL.SetTextureBlendMode(this.BackTexture, SDL.BlendMode.Blend);
                SDL.SetRenderDrawColor(this.Renderer, 0, 0, 0, 0);

                SDL.RenderClear(this.Renderer);
                SDL.RenderTexture(this.Renderer, this.BackTexture, IntPtr.Zero, IntPtr.Zero);
                SDL.RenderPresent(this.Renderer);

                PhotonDiagnostics.Emit(new RenderPresentEventArgs(this));
            }

            this.ApplyPostTick();

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        public virtual void Event(SDL.Event e)
        {
            PhotonDiagnostics.Emit(new PlatformEventEventArgs(new PlatformEventArgs(e), DiagnosticPhase.Start));

            try
            {
                SDL.EventType eventType = (SDL.EventType)e.Type;

                switch (eventType)
                {
                    case SDL.EventType.MouseMotion:
                        this.PointerMotionHandler(this, e);
                        this.GetFocusedControl(e.Motion)?.OnEvent(this, new PlatformEventArgs(e));
                        return;

                    case SDL.EventType.MouseButtonDown:
                    case SDL.EventType.MouseButtonUp:
                        this.PointerButtonHandler(this, e);
                        this.GetFocusedControl(e.Motion)?.OnEvent(this, new PlatformEventArgs(e));
                        return;

                    case SDL.EventType.TextInput:
                        break;
                }

                this.KeyboardInputTarget.OnEvent(this, new PlatformEventArgs(e));
            }
            finally
            {
                PhotonDiagnostics.Emit(new PlatformEventEventArgs(new PlatformEventArgs(e), DiagnosticPhase.End));
            }
        }

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
        public Control GetFocusedControl(SDL.MouseMotionEvent motion)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [motion], DiagnosticPhase.Start));

            try
            {
                Control? c = Photon.ResolveHitControl(this, motion.X, motion.Y);

                return this.PointerCapturedControl ?? c ?? this;
            }
            finally
            {
                PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [motion], DiagnosticPhase.End));
            }
        }

        #endregion

        #region Window: Internal

        public virtual void SetFocus(Control? control)
        {
            if (this.FocusedControl == control) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [control], DiagnosticPhase.Start));

            this.FocusedControl?.OnEvent(this, new FocusLostEventArgs(this.FocusedControl));
            this.FocusedControl = control;
            this.FocusedControl?.OnEvent(this, new FocusGainedEventArgs(this.FocusedControl));

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [control], DiagnosticPhase.End));
        }
        public virtual void SetWindowSize(int width, int height)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [width, height], DiagnosticPhase.Start));

            if (this.BackTexture != IntPtr.Zero)
            {
                SDL.DestroyTexture(this.BackTexture);

                this.WindowBackTexture = IntPtr.Zero;
            }

            this.IntrinsicSize = new() { Width = width, Height = height };
            this.DrawRect = new() { X = 0, Y = 0, W = width, H = height };

            this.WindowBackTexture = SDL.CreateTexture(this.Renderer, SDL.PixelFormat.RGBA8888, SDL.TextureAccess.Target, width, height);

            if (this.BackTexture == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create back texture for window resize.");

            this.RequestMeasure();
            this.RequestArrange();
            this.RequestRender();

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [width, height], DiagnosticPhase.End));
        }
        public virtual void SetRenderer(IntPtr renderer)
        {
            if (this.Renderer == renderer)
                return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [renderer], DiagnosticPhase.Start));

            if (this.BackTexture != IntPtr.Zero)
            {
                SDL.DestroyTexture(this.BackTexture);

                this.WindowBackTexture = IntPtr.Zero;
            }

            this.Renderer = renderer;

            this.SetWindowSize((int)this.DrawRect.W, (int)this.DrawRect.H);

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [renderer], DiagnosticPhase.End));
        }

        public PropertyAnimation<TTarget, TProp> Animate<TTarget, TProp>(TTarget target, Expression<Func<TTarget, TProp>> selector)
            where TTarget : Control
        {
            return this.AnimationBulder.BuildPropertyAnimation(target, selector);
        }
        public AnimationHandle AnimationEnqueue(AnimationBase animation)
        {
            AnimationHandle handle = new(animation);

            this.ActiveAnimations.Add(handle);

            if (handle.State == AnimationState.Ready)
                handle.Start();

            return handle;
        }
        public void CancelAllAnimations() => this.ActiveAnimations.Clear();

        public virtual void CapturePointer(Control control)
        {
            ArgumentNullException.ThrowIfNull(control, nameof(control));

            this.ReleasePointer();

            this.PointerCapturedControl = control;

            control.OnEvent(this, new PointerCaptured(this, control));
        }
        public virtual void ReleasePointer()
        {
            if (this.PointerCapturedControl == null) return;

            Control released = this.PointerCapturedControl;

            this.PointerCapturedControl = null;

            released.OnEvent(this, new PointerReleased(this, released));
        }

        #endregion

        #region Window: Traversal

        protected bool NeedsRendering()
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            try
            {
                if (this.SuppressRendering)
                    return false;

                bool needsRender = false;

                this.TunnelControls((c) =>
                {
                    if (c.IsRenderDirty)
                    {
                        needsRender = true;

                        return false;
                    }
                    return true;
                });

                return needsRender;
            }
            finally
            {
                PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
            }
        }

        protected void ApplyTick()
        {
            if (this.Child == null) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.Child.TunnelControls((c) =>
            {
                c.OnTick(this);
                c.TickAction?.Invoke(c);

                return true;
            });

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected void ApplyAnimationRequests()
        {
            if (this.ActiveAnimations.Count == 0) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            foreach (AnimationHandle? handle in this.ActiveAnimations.ToList())
            {
                if (!handle.IsValid ||
                    handle.State == AnimationState.Invalid ||
                    handle.State == AnimationState.Completed ||
                    handle.State == AnimationState.Stopped ||
                    handle.State == AnimationState.Canceled)
                {
                    this.ActiveAnimations.Remove(handle);
                    handle.Invalidate();
                    continue;
                }

                if (handle.State == AnimationState.Running)
                    handle.Update();
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected void ApplyIntrinsicRequests()
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.TunnelControls(control =>
            {
                if (control.IsIntrinsicDirty)
                {
                    if (control == this)
                    {
                        control.OnIntrinsic(this, this.IntrinsicSize);
                    }
                    else
                    {
                        if (control.Parent == null)
                            throw new NullReferenceException($"Broken Branch: Child {control.Name}:{control.GetType()} has no parent");

                        control.OnIntrinsic(this, control.IntrinsicSize);
                    }
                }
                return true;
            });

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected void ApplyMeasureRequests()
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.TunnelControls(control =>
            {
                if (control.IsMeasureDirty)
                {
                    if (control == this)
                    {
                        control.OnMeasure(this);
                    }
                    else
                    {
                        if (control.Parent == null)
                            throw new NullReferenceException($"Broken Branch: Child {control.Name}:{control.GetType()} has no parent");

                        control.OnMeasure(this);
                    }
                }
                return true;
            });

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected void ApplyArrangeRequests()
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.TunnelControls(control =>
            {
                if (control.IsLayoutDirty)
                {
                    if (control == this)
                    {
                        control.OnArrange(this, default);
                    }
                    else
                    {
                        if (control.Parent == null)
                            throw new NullReferenceException($"Broken Branch: Child {control.Name}:{control.GetType()} has no parent");

                        SDL.FRect contentRect = control.Parent.DrawRect.Deflate(control.Parent.PaddingExtent);

                        SDL.FPoint anchor = new() { X = contentRect.X, Y = contentRect.Y };

                        control.OnArrange(this, anchor);
                    }
                }
                return true;
            });

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected void ApplyLateTick()
        {
            if (this.Child == null) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.Child.TunnelControls((c) =>
            {
                c.OnLateTick(this);
                c.LateTickAction?.Invoke(c);

                return true;
            });

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected void ApplyRenderRequests()
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.TunnelControls(control =>
            {
                if (control.IsRenderDirty)
                {
                    if (control == this)
                    {
                        SDL.Rect clipRect = this.DrawRect.ToRect();

                        this.OnRender(this, clipRect);
                    }
                    else
                    {
                        if (control.Parent == null)
                            throw new NullReferenceException($"Broken Branch: Child {control.Name}:{control.GetType()} has no parent");

                        SDL.FRect contentRect = control.Parent.DrawRect.Deflate(control.Parent.PaddingExtent);

                        SDL.Rect clipRect = contentRect.ToRect();

                        control.OnRender(this, clipRect);
                    }
                }

                return true;
            });

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected void ApplyPostTick()
        {
            if (this.Child == null) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.Child.TunnelControls((c) =>
            {
                c.OnPostTick(this);
                c.PostTickAction?.Invoke(c);

                return true;
            });

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected void ApplyClearRequests()
        {
            if (this.Child == null) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            this.Child.TunnelControls((Func<Control, bool>)((c) =>
            {
                if (c.IsBoundryDirty)
                {
                    SDL.FRect renderRect = FRect.Deflate(c.DrawRect, c.Margin);

                    Photon.ClearRectangle(this, renderRect, this.BackTexture, default);

                    c.IsBoundryDirty = false;
                    c.IsRenderDirty = true;
                }
                return true;
            }));

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }

        #endregion

        #region Window: Framework

        protected virtual void PointerMotionHandler(Window window, SDL.Event e)
        {
            if (e.Type != (uint)SDL.EventType.MouseMotion)
                return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            SDL.MouseMotionEvent motion = e.Motion;

            Control? target = Photon.ResolveHitControl(window, motion.X, motion.Y);

            List<Control> currentPath = target != null ? Photon.GetAncestors(target) : [];

            bool focusSet = false;

            for (int i = currentPath.Count - 1; i >= 0; i--)
            {
                if (!focusSet)
                {
                    focusSet = true;
                }
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }
        protected virtual void PointerButtonHandler(Window window, SDL.Event e)
        {
            if (e.Type != (uint)SDL.EventType.MouseButtonDown &&
                e.Type != (uint)SDL.EventType.MouseButtonUp)
                return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.Start));

            Control target = this.GetFocusedControl(e.Motion);

            List<Control>? path = Photon.GetControlPath(window, c => c == target);

            if (target != null && path != null)
            {
                // Preview phase
                PointerClickEventArgs previewArgs = new(window, target, e)
                {
                    Preview = true
                };
                foreach (Control control in path)
                    control?.OnEvent(window, previewArgs);

                // Bubble phase
                PointerClickEventArgs bubbleArgs = new(window, target, e);
                target.FrameworkEventBubble(window, bubbleArgs);
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, null, DiagnosticPhase.End));
        }

        #endregion

        #region Window: Hooks

        public override void OnInitialize(Window window)
        {
            if (window.IsInitialized == true) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window], DiagnosticPhase.Start));

            try
            {
                if (window.Mode == WindowMode.Tangible)
                {
                    window.Handle = SDL.CreateWindow(
                        window.DefaultTitle,
                        (int)window.DefaultSize.Width,
                        (int)window.DefaultSize.Height,
                        window.DefaultFlags);

                    if (!window.HasTangibleWindow)
                    {
                        SDL.Quit();

                        return;
                    }

                    window.Renderer = SDL.CreateRenderer(window.Handle, null);

                    if (window.Renderer == IntPtr.Zero)
                    {
                        SDL.DestroyWindow(window.Handle);
                        SDL.Quit();

                        return;
                    }

                    SDL.GetWindowSize(window.Handle, out int width, out int height);

                    window.SetWindowSize(width, height);
                }

                window.Name = window.Name;
                window.HorizontalAlignment = Models.Properties.HorizontalAlignment.Stretch;
                window.VerticalAlignment = Models.Properties.VerticalAlignment.Stretch;
                window.IsInitialized = true;
                window.Child?.OnInitialize(window);
            }
            finally
            {
                PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window], DiagnosticPhase.End));
            }
        }

        #endregion
    }
}