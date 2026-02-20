using PhotonUI.Controls;
using PhotonUI.Extensions;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;

namespace PhotonUI
{
    public static class Photon
    {
        public static void EnsureRootWindow(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (control.Window is null)
                throw new InvalidOperationException($"Control '{control.Name}' has no RootWindow.");
        }
        public static void InvalidateRenderChain(Control control)
        {
            if (!control.IsInitialized) return;

            Control? boundary = null;

            control.Parent?.BubbleControls(ancestor =>
            {
                ancestor.IsRenderDirty = true;

                if (ancestor.IsVisible && ancestor.IsOpaque)
                {
                    boundary = ancestor;
                    return false;
                }
                return true;
            });

            boundary ??= control.Window;

            if (boundary?.Window != null)
            {
                boundary.RequestRender(false);
                boundary.IsBoundryDirty = true;
            }
        }

        #region Photon: Hit Testing

        public static List<Control> GetControlPath(Control root, Func<Control, bool> predicate)
        {
            List<Control> path = [];

            root.TunnelControls(control =>
            {
                bool value = predicate(control);

                path.Add(control);

                if (value == true)
                    return false;
                return true;

            }, TunnelDirection.TopDown);

            return path;
        }

        #endregion

        #region Photon: Layout Helpers

        public static Size GetMinimumSize(Control control, Size? childSize = null)
        {
            float coreWidth = childSize?.Width ?? control.MinWidth;
            float coreHeight = childSize?.Height ?? control.MinHeight;

            float effectiveWidth = Math.Clamp(coreWidth, control.MinWidth, control.MaxWidth);
            float effectiveHeight = Math.Clamp(coreHeight, control.MinHeight, control.MaxHeight);

            float width = effectiveWidth + control.PaddingExtent.Horizontal;
            float height = effectiveHeight + control.PaddingExtent.Vertical;

            return new Size { Width = width, Height = height };
        }

        #endregion

        #region Photon: Clip Management Helpers

        public static void GetControlClipRect(SDL.FRect controlRect, bool clipToBounds, SDL.Rect? clipRect, out SDL.Rect? rect)
        {
            if (!clipToBounds)
            {
                rect = clipRect;
                return;
            }

            if (clipRect.HasValue)
            {
                if (SDL.GetRectIntersection(controlRect.ToRect(), clipRect.Value, out SDL.Rect newClipRect))
                {
                    rect = newClipRect;

                    return;
                }
                else
                {
                    rect = null;

                    return;
                }
            }

            rect = controlRect.ToRect();
        }
        public static void ApplyControlClipRect(Window window, SDL.Rect? intersection)
        {
            IntPtr previousTarget = SDL.GetRenderTarget(window.Renderer);

            if (window.BackTexture != IntPtr.Zero)
                SDL.SetRenderTarget(window.Renderer, window.BackTexture);

            if (intersection.HasValue)
                SDL.SetRenderClipRect(window.Renderer, intersection.Value);
            else
                SDL.SetRenderClipRect(window.Renderer, IntPtr.Zero);

            SDL.SetRenderTarget(window.Renderer, previousTarget);
        }

        #endregion

        #region Photon: Drawing Helpers

        public static void DrawRectangle(Window window, SDL.FRect rect, SDL.Color color, IntPtr target)
        {
            if (color.A < 1) return;

            IntPtr previousTarget = SDL.GetRenderTarget(window.Renderer);

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(window.Renderer, target);

            SDL.GetRenderDrawBlendMode(window.Renderer, out SDL.BlendMode previousBlendMode);

            SDL.SetRenderDrawBlendMode(window.Renderer, SDL.BlendMode.Blend);

            if (color.A > 0)
            {
                SDL.SetRenderDrawColor(window.Renderer, color.R, color.G, color.B, color.A);

                SDL.RenderFillRect(window.Renderer, rect);
            }

            SDL.SetRenderDrawBlendMode(window.Renderer, previousBlendMode);

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(window.Renderer, previousTarget);
        }

        #endregion

        #region Photon: Control Drawing Helpers

        public static void DrawControlBackground<T>(T control) where T : Control, IControlProperties
        {
            EnsureRootWindow(control);

            SDL.Color adjusted = new()
            {
                R = control.BackgroundColor.R,
                G = control.BackgroundColor.G,
                B = control.BackgroundColor.B,
                A = (byte)(control.BackgroundColor.A * control.Opacity)
            };

            SDL.FRect drawSurface = control.DrawRect;

            DrawRectangle(control.Window!, drawSurface, adjusted, control.Window?.BackTexture ?? default);
        }

        #endregion
    }
}