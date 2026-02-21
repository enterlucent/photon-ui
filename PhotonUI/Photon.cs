using PhotonUI.Controls;
using PhotonUI.Extensions;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;

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
        public static int GetControlDepth(Control c)
        {
            int depth = 0;
            Control? parent = c.Parent;

            while (parent != null)
            {
                depth++;

                parent = parent.Parent;
            }

            return depth;
        }
        public static List<Control> GetAncestors(Control c)
        {
            List<Control> list = [];
            Control? current = c;

            while (current != null)
            {
                list.Insert(0, current);

                current = current.Parent;
            }

            return list;
        }
        public static string GetAncestorPath(Control c)
            => string.Join("/", GetAncestors(c).Select(a => a.Name));

        public static bool HitTest(SDL.FRect bounds, float px, float py)
        {
            return px >= bounds.X && px < bounds.X + bounds.W &&
                   py >= bounds.Y && py < bounds.Y + bounds.H;
        }
        public static bool AncestorHitTest(Control control, float px, float py)
        {
            Control? parent = control.Parent;

            while (parent != null)
            {
                if (parent is Window) break;

                if (!HitTest(parent.DrawRect, px, py))
                    return false;

                parent = parent.Parent;
            }

            return true;
        }

        public static List<Control> GetHitControls(Window window, float px, float py)
        {
            List<Control> hits = [];

            window.TunnelControls((Func<Control, bool>)(c =>
            {
                if (c.IsVisible && c.IsHitTestVisible)
                    if (HitTest((SDL.FRect)c.DrawRect, px, py))
                        hits.Add(c);

                return true;
            }));

            return hits;
        }
        public static Control? ResolveHitControl(Window window, float px, float py)
        {
            List<Control> hits = GetHitControls(window, px, py);

            if (hits == null || hits.Count == 0)
                return null;

            IOrderedEnumerable<Control> ordered = hits
                .Where(c => c.IsVisible && c.IsHitTestVisible)
                .OrderByDescending(GetAncestorPath)
                .ThenByDescending(c => c.ZIndex)
                .ThenByDescending(GetControlDepth);

            foreach (Control control in ordered)
                if (AncestorHitTest(control, px, py))
                    return control;

            return null;
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

        public static float GetStretchedWidth(HorizontalAlignment alignment, float width, float available, float offsetX = 0)
        {
            if (alignment != HorizontalAlignment.Stretch)
                return width;

            float usable = available - offsetX;
            if (usable >= width)
                return usable;

            return width;
        }
        public static float GetStretchedHeight(VerticalAlignment alignment, float height, float available, float offsetY = 0)
        {
            if (alignment != VerticalAlignment.Stretch)
                return height;

            float usable = available - offsetY;
            if (usable >= height)
                return usable;

            return height;
        }

        public static float GetHorizontalAlignment(HorizontalAlignment alignment, float intial, float width, float availableWidth)
        {
            switch (alignment)
            {
                case HorizontalAlignment.Center:
                    return intial + (availableWidth - width) / 2;
                case HorizontalAlignment.Right:
                    return intial + (availableWidth - width);
                default:
                    return intial;
            }
        }
        public static float GetVerticalAlignment(VerticalAlignment alignment, float intial, float height, float availableHeight)
        {
            switch (alignment)
            {
                case VerticalAlignment.Center:
                    return intial + (availableHeight - height) / 2;
                case VerticalAlignment.Bottom:
                    return intial + (availableHeight - height);
                default:
                    return intial;
            }
        }

        #endregion

        #region Photon: Control Layout Helpers

        public static float ApplyHorizontalScroll(Control control, float scrollX, SDL.FRect viewport)
        {
            SDL.FRect scrolled = control.DrawRect;
            float baseX = scrolled.X;

            if (scrolled.W <= viewport.W)
            {
                scrolled.X = baseX;
                control.DrawRect = scrolled;
                return 0;
            }
            else
            {
                float maxScrollX = scrolled.W - viewport.W;
                scrollX = Math.Clamp(scrollX, 0, maxScrollX);
                scrolled.X = baseX - scrollX;
                control.DrawRect = scrolled;
                return scrollX;
            }
        }
        public static float ApplyVerticalScroll(Control control, float scrollY, SDL.FRect viewport)
        {
            SDL.FRect scrolled = control.DrawRect;

            float baseY = scrolled.Y;

            if (scrolled.H <= viewport.H)
            {
                scrolled.Y = baseY;
                control.DrawRect = scrolled;
                return 0;
            }
            else
            {
                float maxScrollY = scrolled.H - viewport.H;
                scrollY = Math.Clamp(scrollY, 0, maxScrollY);
                scrolled.Y = baseY - scrollY;
                control.DrawRect = scrolled;
                return scrollY;
            }
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

        public static IntPtr CreateTexture(IntPtr renderer, int width, int height, SDL.PixelFormat format = SDL.PixelFormat.ARGB8888, SDL.TextureAccess access = SDL.TextureAccess.Target)
        {
            if (renderer == IntPtr.Zero)
                throw new ArgumentException("Renderer pointer must not be null.", nameof(renderer));

            IntPtr texture = SDL.CreateTexture(renderer, format, access, width, height);

            if (texture == IntPtr.Zero)
                throw new InvalidOperationException($"CreateTexture failed: {SDL.GetError()}");

            return texture;
        }

        public static void ClearRectangle(Window window, SDL.FRect rect, IntPtr target, SDL.Color baseColor = new SDL.Color())
        {
            IntPtr previousTarget = SDL.GetRenderTarget(window.Renderer);

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(window.Renderer, target);

            SDL.GetRenderDrawBlendMode(window.Renderer, out SDL.BlendMode prevBlend);

            SDL.SetRenderDrawBlendMode(window.Renderer, SDL.BlendMode.None);
            SDL.SetRenderDrawColor(window.Renderer, baseColor.R, baseColor.G, baseColor.B, baseColor.A);
            SDL.RenderFillRect(window.Renderer, rect);

            SDL.SetRenderDrawBlendMode(window.Renderer, prevBlend);

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(window.Renderer, previousTarget);
        }
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
        public static void DrawBorder(Window window, SDL.FRect controlRect, Thickness borderThickness, BorderColors colors, IntPtr target)
        {
            IntPtr previousTarget = SDL.GetRenderTarget(window.Renderer);

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(window.Renderer, target);

            if (colors.Left.A > 0)
            {
                SDL.SetRenderDrawColor(window.Renderer, colors.Left.R, colors.Left.G, colors.Left.B, colors.Left.A);
                SDL.RenderFillRect(window.Renderer, new SDL.FRect
                {
                    X = controlRect.X,
                    Y = controlRect.Y,
                    W = borderThickness.Left,
                    H = controlRect.H
                });
            }

            if (colors.Top.A > 0)
            {
                SDL.SetRenderDrawColor(window.Renderer, colors.Top.R, colors.Top.G, colors.Top.B, colors.Top.A);
                SDL.RenderFillRect(window.Renderer, new SDL.FRect
                {
                    X = controlRect.X,
                    Y = controlRect.Y,
                    W = controlRect.W,
                    H = borderThickness.Top
                });
            }

            if (colors.Right.A > 0)
            {
                SDL.SetRenderDrawColor(window.Renderer, colors.Right.R, colors.Right.G, colors.Right.B, colors.Right.A);
                SDL.RenderFillRect(window.Renderer, new SDL.FRect
                {
                    X = controlRect.X + controlRect.W - borderThickness.Right,
                    Y = controlRect.Y,
                    W = borderThickness.Right,
                    H = controlRect.H
                });
            }

            if (colors.Bottom.A > 0)
            {
                SDL.SetRenderDrawColor(window.Renderer, colors.Bottom.R, colors.Bottom.G, colors.Bottom.B, colors.Bottom.A);
                SDL.RenderFillRect(window.Renderer, new SDL.FRect
                {
                    X = controlRect.X,
                    Y = controlRect.Y + controlRect.H - borderThickness.Bottom,
                    W = controlRect.W,
                    H = borderThickness.Bottom
                });
            }

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(window.Renderer, previousTarget);
        }
        public static void DrawTexture(Window window, IntPtr texture, SDL.FRect destination, IntPtr target, SDL.FRect? sourceRect = null, SDL.Rect? clipRect = null)
        {
            if (texture == IntPtr.Zero)
                throw new InvalidOperationException("Cannot draw: texture is null.");

            IntPtr renderer = window.Renderer;
            IntPtr previousTarget = SDL.GetRenderTarget(renderer);

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(renderer, target);

            SDL.GetRenderClipRect(renderer, out SDL.Rect previousClipRect);

            if (clipRect.HasValue)
                ApplyControlClipRect(window, clipRect.Value);

            bool result;
            if (sourceRect.HasValue)
                result = SDL.RenderTexture(renderer, texture, sourceRect.Value, in destination);
            else
                result = SDL.RenderTexture(renderer, texture, IntPtr.Zero, in destination);

            ApplyControlClipRect(window, previousClipRect);

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(renderer, previousTarget);

            if (!result)
                throw new InvalidOperationException($"RenderTexture failed: {SDL.GetError()}");
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
        public static void DrawControlBackground<T>(T control, ControlProperties props) where T : Control, IControlProperties
        {
            EnsureRootWindow(control);

            SDL.Color adjusted = new()
            {
                R = props.BackgroundColor.R,
                G = props.BackgroundColor.G,
                B = props.BackgroundColor.B,
                A = (byte)(props.BackgroundColor.A * props.Opacity)
            };

            SDL.FRect drawSurface = control.DrawRect;

            DrawRectangle(control.Window!, drawSurface, adjusted, control.Window?.BackTexture ?? default);
        }

        public static void DrawControlBorder<T>(T control) where T : Control, IBorderProperties, IControlProperties
        {
            EnsureRootWindow(control);

            BorderColors adjusted = control.BorderColors.WithOpacity(control.Opacity);

            SDL.FRect drawSurface = control.DrawRect;

            DrawBorder(control.Window!, drawSurface, control.BorderThickness, adjusted, control.Window!.BackTexture);
        }
        public static void DrawControlBorder<T>(T control, BorderProperties props, ControlProperties controlProps)
            where T : Control, IBorderProperties
        {
            EnsureRootWindow(control);

            BorderColors adjusted = props.BorderColors.WithOpacity(controlProps.Opacity);

            SDL.FRect drawSurface = control.DrawRect;

            DrawBorder(control.Window!, drawSurface, props.BorderThickness, adjusted, control.Window!.BackTexture);
        }

        public static void DrawControlTexture<T>(T control, IntPtr texture, SDL.FRect destination, SDL.Rect? clipRect = null) where T : Control, IControlProperties
        {
            EnsureRootWindow(control);

            SDL.GetTextureAlphaMod(texture, out byte originalAlpha);

            byte newAlpha = (byte)(originalAlpha * control.Opacity);

            SDL.SetTextureAlphaMod(texture, newAlpha);

            DrawTexture(control.Window!, texture, destination, control.Window!.BackTexture, null, clipRect);

            SDL.SetTextureAlphaMod(texture, originalAlpha);
        }

        #endregion
    }
}