using PhotonUI.Controls;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;

namespace PhotonUI.Behaviors
{
    public interface IScrollHandler
    {
        void OnScrollbarUpdated(ScrollContext context);
    }

    public class ScrollbarBehavior<T>(T control) where T : Control, IScrollHandler, IScrollProperties
    {
        protected T Control = control;

        protected ulong MouseDownTicks;

        protected bool IsScrollDirty = true;
        protected IntPtr TextureCache = IntPtr.Zero;

        protected bool IsVerticalCaretDown;
        protected bool IsHorizontalCaretDown;

        protected bool WasVerticalTrackClicked;
        protected bool WasHorizontalTrackClicked;

        protected SDL.FRect? VerticalTrackRect;
        protected SDL.FRect? VerticalCaretRect;
        protected SDL.FRect? HorizontalTrackRect;
        protected SDL.FRect? HorizontalCaretRect;

        protected float ExtentX;
        protected float OffsetX;
        protected float ExtentY;
        protected float OffsetY;

        public float ExtentWidth
        {
            get => this.ExtentX;
            set
            {
                if (value < 0f) value = 0f;

                if (this.ExtentX != value)
                {
                    this.ComputeRightAnchorUpdate(this.ExtentX, value);
                    this.ExtentX = value;
                    this.RequestRender();
                }
            }
        }
        public float HorizontalOffset
        {
            get => this.OffsetX;
            set
            {
                if (this.OffsetX != value)
                {
                    this.OffsetX = value;
                    this.RequestRender();
                }
            }
        }
        public float ExtentHeight
        {
            get => this.ExtentY;
            set
            {
                if (value < 0f) value = 0f;

                if (this.ExtentY != value)
                {
                    this.ComputeBottomAnchorUpdate(this.ExtentY, value);
                    this.ExtentY = value;
                    this.RequestRender();
                }
            }
        }
        public float VerticalOffset
        {
            get => this.OffsetY;
            set
            {
                if (this.OffsetY != value)
                {
                    this.OffsetY = value;
                    this.RequestRender();
                }
            }
        }

        protected bool HasMinAffordance(SDL.FRect viewport)
        {
            Size minSize = this.Control.MinScrollviewSize;

            return viewport.W >= minSize.Width && viewport.H >= minSize.Height;
        }
        protected bool ContentFitsHorizontally(SDL.FRect viewport)
        {
            return this.ExtentWidth <= this.Control.DrawRect.W - viewport.W;
        }
        protected bool ContentFitsVertically(SDL.FRect viewport)
        {
            return this.ExtentHeight <= this.Control.DrawRect.H - viewport.H;
        }

        public Size GetScrollbarSize(SDL.FRect viewport)
        {
            int laneW = (int)Math.Max(
                this.Control.ScrollTrackThickness.Horizontal,
                this.Control.ScrollCaretThickness.Horizontal);

            int laneH = (int)Math.Max(
                this.Control.ScrollTrackThickness.Vertical,
                this.Control.ScrollCaretThickness.Vertical);

            int scrollbarWidth = 0;
            int scrollbarHeight = 0;

            if (this.HasMinAffordance(viewport))
            {
                if (!this.ContentFitsVertically(viewport))
                    if (this.Control.ScrollDirection == ScrollDirection.Vertical ||
                        this.Control.ScrollDirection == ScrollDirection.Both) scrollbarWidth = laneW;

                if (!this.ContentFitsHorizontally(viewport))
                    if (this.Control.ScrollDirection == ScrollDirection.Horizontal ||
                        this.Control.ScrollDirection == ScrollDirection.Both) scrollbarHeight = laneH;
            }

            return new Size(scrollbarWidth, scrollbarHeight);
        }

        public void ScrollToTop()
        {
            this.VerticalOffset = 0f;

            this.Control.OnScrollbarUpdated(new ScrollContext(this.HorizontalOffset, this.ExtentWidth, this.VerticalOffset, this.ExtentHeight));
        }
        public void ScrollToFraction(float fraction)
        {
            if (fraction < 0f) fraction = 0f;
            if (fraction > 1f) fraction = 1f;

            float targetOffset = this.ExtentHeight * fraction;

            this.VerticalOffset = targetOffset;

            this.Control.OnScrollbarUpdated(new ScrollContext(this.HorizontalOffset, this.ExtentWidth, this.VerticalOffset, this.ExtentHeight));
        }
        public void ScrollToBottom()
        {
            float maxOffset = Math.Max(0, this.ExtentHeight);

            this.VerticalOffset = maxOffset;

            this.Control.OnScrollbarUpdated(new ScrollContext(this.HorizontalOffset, this.ExtentWidth, this.VerticalOffset, this.ExtentHeight));
        }

        #region ScrollbarBehavior: Input Handlers

        protected virtual void HandleMouseDown(SDL.Event e)
        {
            float px = e.Button.X;
            float py = e.Button.Y;

            this.MouseDownTicks = SDL.GetTicks();

            this.IsVerticalCaretDown = false;
            this.WasVerticalTrackClicked = false;
            this.IsHorizontalCaretDown = false;
            this.WasHorizontalTrackClicked = false;

            if (this.VerticalCaretRect.HasValue && Photon.HitTest(this.VerticalCaretRect.Value, px, py))
            {
                this.IsVerticalCaretDown = true;

                this.Control.Window?.CaptureMouse(this.Control);

                return;
            }
            if (this.VerticalTrackRect.HasValue && Photon.HitTest(this.VerticalTrackRect.Value, px, py))
            {
                this.WasVerticalTrackClicked = true;

                return;
            }
            if (this.HorizontalCaretRect.HasValue && Photon.HitTest(this.HorizontalCaretRect.Value, px, py))
            {
                this.Control.Window?.CaptureMouse(this.Control);

                this.IsHorizontalCaretDown = true;

                return;
            }
            if (this.HorizontalTrackRect.HasValue && Photon.HitTest(this.HorizontalTrackRect.Value, px, py))
            {
                this.WasHorizontalTrackClicked = true;

                return;
            }
        }
        protected virtual void HandleMouseMotion(SDL.Event e)
        {
            float px = e.Motion.X;
            float py = e.Motion.Y;

            if (this.IsVerticalCaretDown && this.VerticalTrackRect.HasValue)
            {
                SDL.FRect track = this.VerticalTrackRect.Value;

                float normY = Math.Clamp((py - track.Y) / track.H, 0f, 1f);
                float rawOffsetY = normY * this.ExtentHeight;
                float stepY = this.Control.ScrollStepY;
                float steppedOffsetY = stepY > 0f ? MathF.Round(rawOffsetY / stepY) * stepY : rawOffsetY;

                ScrollContext context = new(
                    this.HorizontalOffset,
                    this.ExtentWidth,
                    steppedOffsetY,
                    this.ExtentHeight
                );

                this.VerticalOffset = steppedOffsetY;

                this.Control.OnScrollbarUpdated(context);
            }
            else if (this.IsHorizontalCaretDown && this.HorizontalTrackRect.HasValue)
            {
                SDL.FRect track = this.HorizontalTrackRect.Value;

                float normX = Math.Clamp((px - track.X) / track.W, 0f, 1f);
                float rawOffsetX = normX * this.ExtentWidth;
                float stepX = this.Control.ScrollStepX;
                float steppedOffsetX = stepX > 0f ? MathF.Round(rawOffsetX / stepX) * stepX : rawOffsetX;

                ScrollContext context = new(
                    steppedOffsetX,
                    this.ExtentWidth,
                    this.VerticalOffset,
                    this.ExtentHeight
                );

                this.HorizontalOffset = steppedOffsetX;

                this.Control.OnScrollbarUpdated(context);
            }
        }
        protected virtual void HandleMouseUp(SDL.Event e)
        {
            ulong elapsed = SDL.GetTicks() - this.MouseDownTicks;

            if (elapsed < 200)
            {
                float px = e.Button.X;
                float py = e.Button.Y;

                if (this.WasVerticalTrackClicked && this.VerticalTrackRect.HasValue)
                {
                    SDL.FRect track = this.VerticalTrackRect.Value;

                    float normY = Math.Clamp((py - track.Y) / track.H, 0f, 1f);
                    float vOffset = normY * this.ExtentHeight;

                    ScrollContext context = new(
                        this.HorizontalOffset,
                        this.ExtentWidth,
                        vOffset,
                        this.ExtentHeight);

                    this.VerticalOffset = vOffset;

                    this.Control?.OnScrollbarUpdated(context);
                }
                else if (this.WasHorizontalTrackClicked && this.HorizontalTrackRect.HasValue)
                {
                    SDL.FRect track = this.HorizontalTrackRect.Value;

                    float normX = Math.Clamp((px - track.X) / track.W, 0f, 1f);
                    float hOffset = normX * this.ExtentWidth;

                    ScrollContext context = new(
                        hOffset,
                        this.ExtentWidth,
                        this.VerticalOffset,
                        this.ExtentHeight);

                    this.HorizontalOffset = hOffset;

                    this.Control?.OnScrollbarUpdated(context);
                }
            }

            this.Control?.Window?.ReleaseMouse();

            this.IsVerticalCaretDown = false;
            this.WasVerticalTrackClicked = false;
            this.IsHorizontalCaretDown = false;
            this.WasHorizontalTrackClicked = false;
        }
        protected virtual void HandleMouseWheel(SDL.Event e)
        {
            int wheelY = (int)e.Wheel.Y;

            float newHOffset = this.HorizontalOffset;
            float newVOffset = this.VerticalOffset;

            bool isShiftHeld = (SDL.GetModState() & SDL.Keymod.Shift) != 0;

            if (this.Control.ScrollDirection == ScrollDirection.Vertical)
            {
                float vOffset = this.VerticalOffset - wheelY * this.Control.WheelScrollStepY;

                newVOffset = Math.Clamp(vOffset, 0f, this.ExtentHeight);

                this.VerticalOffset = newVOffset;
            }
            else if (this.Control.ScrollDirection == ScrollDirection.Horizontal)
            {
                float hOffset = this.HorizontalOffset - wheelY * this.Control.WheelScrollStepX;

                newHOffset = Math.Clamp(hOffset, 0f, this.ExtentWidth);

                this.HorizontalOffset = newHOffset;
            }
            else if (this.Control.ScrollDirection == ScrollDirection.Both)
            {
                if (isShiftHeld)
                {
                    float hOffset = this.HorizontalOffset - wheelY * this.Control.WheelScrollStepX;

                    newHOffset = Math.Clamp(hOffset, 0f, this.ExtentWidth);

                    this.HorizontalOffset = newHOffset;
                }
                else
                {
                    float vOffset = this.VerticalOffset - wheelY * this.Control.WheelScrollStepY;

                    newVOffset = Math.Clamp(vOffset, 0f, this.ExtentHeight);

                    this.VerticalOffset = newVOffset;
                }
            }

            ScrollContext context = new(newHOffset, this.ExtentWidth, newVOffset, this.ExtentHeight);

            this.Control.OnScrollbarUpdated(context);
        }

        #endregion

        #region ScrollbarBehavior: Framework

        public virtual void RequestRender()
        {
            this.IsScrollDirty = true;
        }

        #endregion

        #region ScrollbarBehavior: Hook

        public virtual void OnRender(Window window, SDL.Rect viewport)
        {
            SDL.RectToFRect(viewport, out SDL.FRect destination);

            if (!this.HasMinAffordance(destination)) return;

            if (this.IsScrollDirty)
            {
                SDL.RectToFRect(viewport, out SDL.FRect outport);

                this.DrawScrollbars(window, outport);
                this.IsScrollDirty = false;
            }

            Photon.DrawControlTexture(this.Control, this.TextureCache, destination, viewport);
        }
        public virtual void OnEvent(Window window, SDL.Event e)
        {
            switch (e.Type)
            {
                case (uint)SDL.EventType.MouseButtonDown:
                    this.HandleMouseDown(e);
                    break;
                case (uint)SDL.EventType.MouseMotion:
                    this.HandleMouseMotion(e);
                    break;
                case (uint)SDL.EventType.MouseButtonUp:
                    this.HandleMouseUp(e);
                    break;
                case (uint)SDL.EventType.MouseWheel:
                    this.HandleMouseWheel(e);
                    break;
            }
        }

        #endregion

        #region ScrollbarBehavior: Helpers

        public void ComputeBottomAnchorUpdate(float previousExtentY, float currentExtentY)
        {
            float deltaY = currentExtentY - previousExtentY;

            if (currentExtentY <= this.Control.DrawRect.H || deltaY == 0f)
                return;

            switch (this.Control.ScrollViewportAnchor)
            {
                case ScrollViewportAnchor.Bottom:
                case ScrollViewportAnchor.BottomLeft:
                case ScrollViewportAnchor.BottomRight:
                    this.Control.OnScrollbarUpdated(new ScrollContext(
                        this.HorizontalOffset,
                        this.ExtentWidth,
                        Math.Clamp(this.OffsetY + deltaY, 0f, currentExtentY),
                        currentExtentY));
                    break;
            }
        }
        public void ComputeRightAnchorUpdate(float previousExtentX, float currentExtentX)
        {
            float deltaX = currentExtentX - previousExtentX;

            if (currentExtentX <= this.Control.DrawRect.W || deltaX == 0f)
                return;

            switch (this.Control.ScrollViewportAnchor)
            {
                case ScrollViewportAnchor.Right:
                case ScrollViewportAnchor.TopRight:
                case ScrollViewportAnchor.BottomRight:
                    this.Control.OnScrollbarUpdated(new ScrollContext(
                        Math.Clamp(this.HorizontalOffset + deltaX, 0f, currentExtentX),
                        currentExtentX,
                        this.OffsetY,
                        this.ExtentHeight));
                    break;
            }
        }

        protected virtual void DrawScrollbars(Window window, SDL.FRect viewport)
        {
            if (this.TextureCache != IntPtr.Zero)
            {
                SDL.DestroyTexture(this.TextureCache);

                this.TextureCache = IntPtr.Zero;
            }



            this.TextureCache = Photon.CreateTexture(window.Renderer, (int)viewport.W, (int)viewport.H);

            if (this.TextureCache == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create cached texture for scrollbar rendering.");

            IntPtr previousTarget = SDL.GetRenderTarget(window.Renderer);

            SDL.SetRenderTarget(window.Renderer, this.TextureCache);
            SDL.SetRenderDrawColor(window.Renderer, 0, 0, 0, 0);
            SDL.RenderClear(window.Renderer);
            SDL.SetRenderTarget(window.Renderer, previousTarget);

            if (!this.ContentFitsVertically(viewport))
                this.DrawVertical(window, viewport);

            if (!this.ContentFitsHorizontally(viewport))
                this.DrawHorizontal(window, viewport);
        }
        protected virtual void DrawHorizontal(Window window, SDL.FRect viewport)
        {
            bool hasVertical = this.Control.ScrollDirection == ScrollDirection.Vertical || this.Control.ScrollDirection == ScrollDirection.Both;
            bool hasHorizontal = this.Control.ScrollDirection == ScrollDirection.Horizontal || this.Control.ScrollDirection == ScrollDirection.Both;

            if (!hasHorizontal || !this.HasMinAffordance(viewport))
            {
                this.HorizontalTrackRect = null;
                this.HorizontalCaretRect = null;

                return;
            }

            float laneH = Math.Max(this.Control.ScrollTrackThickness.Vertical, this.Control.ScrollCaretThickness.Vertical);
            float laneY = viewport.H - laneH;
            float effectiveWidth = viewport.W;

            if (hasVertical)
                effectiveWidth -= Math.Max(this.Control.ScrollTrackThickness.Horizontal, this.Control.ScrollCaretThickness.Horizontal);

            if (laneH < 1f || effectiveWidth < 1f)
            {
                this.HorizontalTrackRect = null;
                this.HorizontalCaretRect = null;
                return;
            }

            SDL.FRect trackRect = new()
            {
                X = 0f,
                Y = laneY + (laneH - this.Control.ScrollTrackThickness.Vertical) / 2f,
                W = effectiveWidth,
                H = this.Control.ScrollTrackThickness.Vertical
            };
            Photon.DrawRectangle(window, trackRect, this.Control.ScrollTrackBackgroundColor, this.TextureCache);

            this.HorizontalTrackRect = new SDL.FRect
            {
                X = viewport.X + trackRect.X,
                Y = viewport.Y + trackRect.Y,
                W = trackRect.W,
                H = trackRect.H
            };

            float caretW = this.Control.ScrollCaretThickness.Horizontal;
            float caretTravelX = Math.Max(0f, effectiveWidth - caretW);
            float normX = this.ExtentWidth > 0f ? Math.Clamp(this.HorizontalOffset / this.ExtentWidth, 0f, 1f) : 0f;
            float caretX = normX * caretTravelX;

            SDL.FRect caretRect = new()
            {
                X = caretX,
                Y = laneY + (laneH - this.Control.ScrollCaretThickness.Vertical) / 2f,
                W = caretW,
                H = this.Control.ScrollCaretThickness.Vertical
            };
            Photon.DrawRectangle(window, caretRect, this.Control.ScrollCaretBackgroundColor, this.TextureCache);

            this.HorizontalCaretRect = new SDL.FRect
            {
                X = viewport.X + caretRect.X,
                Y = viewport.Y + caretRect.Y,
                W = caretRect.W,
                H = caretRect.H
            };
        }
        protected virtual void DrawVertical(Window window, SDL.FRect viewport)
        {
            bool hasVertical = this.Control.ScrollDirection == ScrollDirection.Vertical || this.Control.ScrollDirection == ScrollDirection.Both;
            bool hasHorizontal = this.Control.ScrollDirection == ScrollDirection.Horizontal || this.Control.ScrollDirection == ScrollDirection.Both;

            if (!hasVertical || !this.HasMinAffordance(viewport))
            {
                this.VerticalTrackRect = null;
                this.VerticalCaretRect = null;

                return;
            }

            float laneW = Math.Max(this.Control.ScrollTrackThickness.Horizontal, this.Control.ScrollCaretThickness.Horizontal);
            float laneX = viewport.W - laneW;
            float effectiveHeight = viewport.H;

            if (hasHorizontal)
                effectiveHeight -= Math.Max(this.Control.ScrollTrackThickness.Vertical, this.Control.ScrollCaretThickness.Vertical);

            if (laneW < 1f || effectiveHeight < 1f)
            {
                this.VerticalTrackRect = null;
                this.VerticalCaretRect = null;
                return;
            }

            SDL.FRect trackRect = new()
            {
                X = laneX + (laneW - this.Control.ScrollTrackThickness.Horizontal) / 2f,
                Y = 0f,
                W = this.Control.ScrollTrackThickness.Horizontal,
                H = effectiveHeight
            };
            Photon.DrawRectangle(window, trackRect, this.Control.ScrollTrackBackgroundColor, this.TextureCache);

            this.VerticalTrackRect = new SDL.FRect
            {
                X = viewport.X + trackRect.X,
                Y = viewport.Y + trackRect.Y,
                W = trackRect.W,
                H = trackRect.H
            };

            float caretH = this.Control.ScrollCaretThickness.Vertical;
            float caretTravelY = Math.Max(0f, effectiveHeight - caretH);
            float normY = this.ExtentHeight > 0f ? Math.Clamp(this.VerticalOffset / this.ExtentHeight, 0f, 1f) : 0f;
            float caretY = (normY * caretTravelY);

            SDL.FRect caretRect = new()
            {
                X = laneX + (laneW - this.Control.ScrollCaretThickness.Horizontal) / 2f,
                Y = caretY,
                W = this.Control.ScrollCaretThickness.Horizontal,
                H = caretH
            };
            Photon.DrawRectangle(window, caretRect, this.Control.ScrollCaretBackgroundColor, this.TextureCache);

            this.VerticalCaretRect = new SDL.FRect
            {
                X = viewport.X + caretRect.X,
                Y = viewport.Y + caretRect.Y,
                W = caretRect.W,
                H = caretRect.H
            };
        }

        #endregion
    }
}