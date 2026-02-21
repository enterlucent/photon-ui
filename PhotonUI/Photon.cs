using PhotonUI.Controls;
using PhotonUI.Controls.Content;
using PhotonUI.Extensions;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using PhotonUI.Services;
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
        public static Window GetWindow(Control control)
        {
            Control? current = control;

            while (current.Parent != null)
                current = current.Parent;

            if (current is Window window)
                return window;

            throw new InvalidOperationException("Window not found");
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

        public static SDL.FRect ScaleRect(SDL.FRect rect, Vector2 scale, SDL.FPoint anchor)
        {
            float anchorX = rect.X + rect.W * anchor.X;
            float anchorY = rect.Y + rect.H * anchor.Y;

            float newW = rect.W * scale.X;
            float newH = rect.H * scale.Y;

            float newX = anchorX - newW * anchor.X;
            float newY = anchorY - newH * anchor.Y;

            return new SDL.FRect { X = newX, Y = newY, W = newW, H = newH };
        }
        public static SDL.FRect RotateRect(SDL.FRect rect, float angle, SDL.FPoint anchor)
        {
            Vector2 norm = new(rect.X + rect.W * anchor.X, rect.Y + rect.H * anchor.Y);

            Vector2[] corners =
            [
                new Vector2(rect.X, rect.Y),
                new Vector2(rect.X + rect.W, rect.Y),
                new Vector2(rect.X + rect.W, rect.Y + rect.H),
                new Vector2(rect.X, rect.Y + rect.H)
            ];

            Vector2[] rotated = [.. corners.Select(c =>
            {
                float dx = c.X - norm.X;
                float dy = c.Y - norm.Y;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                return new Vector2(
                    dx * cos - dy * sin + norm.X,
                    dx * sin + dy * cos + norm.Y
                );
            })];

            float minX = rotated.Min(p => p.X);
            float maxX = rotated.Max(p => p.X);
            float minY = rotated.Min(p => p.Y);
            float maxY = rotated.Max(p => p.Y);

            return new SDL.FRect { X = minX, Y = minY, W = maxX - minX, H = maxY - minY };
        }

        public static Size GetScaledSize(Size controlSize, Size contentSize, StretchProperties props)
        {
            float targetW = contentSize.Width;
            float targetH = contentSize.Height;

            switch (props.StretchMode)
            {
                case StretchMode.Fill:
                    targetW = controlSize.Width;
                    targetH = controlSize.Height;
                    break;

                case StretchMode.Uniform:
                    {
                        float scale = Math.Min(controlSize.Width / contentSize.Width, controlSize.Height / contentSize.Height);
                        targetW = contentSize.Width * scale;
                        targetH = contentSize.Height * scale;
                    }
                    break;

                case StretchMode.UniformToFill:
                    {
                        float scale = Math.Max(controlSize.Width / contentSize.Width, controlSize.Height / contentSize.Height);
                        targetW = contentSize.Width * scale;
                        targetH = contentSize.Height * scale;
                    }
                    break;

                case StretchMode.None:
                    break;
            }

            switch (props.StretchDirection)
            {
                case StretchDirection.UpOnly:
                    if (targetW < contentSize.Width) targetW = contentSize.Width;
                    if (targetH < contentSize.Height) targetH = contentSize.Height;
                    break;

                case StretchDirection.DownOnly:
                    if (targetW > contentSize.Width) targetW = contentSize.Width;
                    if (targetH > contentSize.Height) targetH = contentSize.Height;
                    break;
            }

            return new Size(targetW, targetH);
        }

        #region Photon: Diagnostics

        public static string BuildCompactStackLine(int trimStartCalls = 0)
        {
            StackTrace stackTrace = new(skipFrames: 2, fNeedFileInfo: false);
            StackFrame[]? frames = stackTrace.GetFrames();

            if (frames == null)
                return "stack: []";

            List<string> parts = [];

            foreach (StackFrame? frame in frames.Reverse())
            {
                MethodBase? method = frame.GetMethod();
                if (method == null)
                    continue;

                string typeName = method.DeclaringType?.Name ?? "UnknownType";
                string methodName = method.Name;

                if (methodName.Contains('<'))
                {
                    int startBracket = methodName.IndexOf('<') + 1;
                    int endBracket = methodName.IndexOf('>');
                    if (endBracket > 0)
                        methodName = methodName[startBracket..endBracket];
                }

                if (typeName.Contains('<'))
                {
                    int startBracket = typeName.IndexOf('<') + 1;
                    int endBracket = typeName.IndexOf('>');
                    if (endBracket > 0)
                        typeName = typeName[startBracket..endBracket];
                    if (string.IsNullOrEmpty(typeName))
                        typeName = "Anon";
                }

                parts.Add($"{typeName}.{methodName}");
            }

            parts = [.. parts.Select(p => p.Contains(".TunnelControls") ? "Tunneling" : p)];

            for (int i = 0; i < parts.Count - 1;)
            {
                if (parts[i] == "Tunneling")
                {
                    int collapseEnd = i;

                    while (collapseEnd < parts.Count && parts[collapseEnd] == "Tunneling")
                        collapseEnd++;

                    if (collapseEnd > i + 1)
                        parts.RemoveRange(i + 1, collapseEnd - i - 1);
                }

                i++;
            }

            if (trimStartCalls > 0 && trimStartCalls < parts.Count)
                parts = [.. parts.Skip(trimStartCalls)];

            return $"stack: {string.Join("→", parts)}";
        }

        public static string GetLayoutData(Control? control, Func<string>? additional = null)
        {
            if (control == null) return "Error: No control provided!";

            StringBuilder data = new();

            string stack = BuildCompactStackLine(3);
            string phase = string.Empty;

            if (stack.Contains("ApplyIntrinsicRequests"))
                phase = "FrameworkIntrinsic";
            else if (stack.Contains("ApplyMeasureRequests"))
                phase = "FrameworkMeasure";
            else if (stack.Contains("ApplyArrangeRequests"))
                phase = "FrameworkArrange";
            else if (stack.Contains("ApplyRenderRequests"))
                phase = "FrameworkRender";

            data.AppendLine($"layout: {control.Name} :: {control.GetType().Name}");
            data.Append("  ");
            data.Append($"phase: {phase}\n");

            data.Append("  ");
            data.Append($"intrinsic=({control.IntrinsicSize.Width:F1},{control.IntrinsicSize.Height:F1}), ");
            data.Append($"draw=({control.DrawRect.X},{control.DrawRect.Y},{control.DrawRect.W},{control.DrawRect.H})\n");

            data.Append("  ");
            data.Append($"margin=({control.Margin.Left},{control.Margin.Top},{control.Margin.Right},{control.Margin.Bottom}), ");
            data.Append($"padding=({control.Padding.Left},{control.Padding.Top},{control.Padding.Right},{control.Padding.Bottom}), ");
            data.Append($"minmax=({control.MinWidth},{control.MinHeight},{control.MaxWidth},{control.MaxHeight})\n");

            if (additional is not null)
                data.AppendLine(additional());

            return data.ToString();
        }

        #endregion

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

        public static bool IHitTest(SDL.Rect bounds, float px, float py)
        {
            return px >= bounds.X && px < bounds.X + bounds.W &&
                   py >= bounds.Y && py < bounds.Y + bounds.H;
        }
        public static bool HitTest(SDL.FRect bounds, float px, float py)
        {
            return px >= bounds.X && px < bounds.X + bounds.W &&
                   py >= bounds.Y && py < bounds.Y + bounds.H;
        }
        public static bool ControlHitTest(Control control, float px, float py)
        {
            if (!control.IsVisible || !control.IsHitTestVisible)
                return false;

            return HitTest(control.DrawRect, px, py);
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

        public static SDL.FPoint ApplyHotspotOffset(SDL.FPoint original, Size hotspotOffset)
        {
            return new SDL.FPoint
            {
                X = original.X + hotspotOffset.Width,
                Y = original.Y + hotspotOffset.Height
            };
        }

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

        public static float GetEdgeScrollHorizontal(float currentOffset, float mouseLocalX, float viewportWidth, float contentWidth, float scrollStep, float scrollMultiplier = 1f, float edgeThreshold = 0)
        {
            float maxOffset = Math.Max(0, contentWidth - viewportWidth);
            if (viewportWidth <= 0 || maxOffset <= 0) return currentOffset;

            float step = scrollStep * (mouseLocalX < 0 || mouseLocalX > viewportWidth ? scrollMultiplier : 1f);

            if ((mouseLocalX <= edgeThreshold || mouseLocalX < 0) && currentOffset > 0)
                return Math.Max(0, currentOffset - step);
            if ((mouseLocalX >= viewportWidth - edgeThreshold || mouseLocalX > viewportWidth) && currentOffset < maxOffset)
                return Math.Min(maxOffset, currentOffset + step);

            return currentOffset;
        }
        public static float GetEdgeScrollVertical(float currentOffset, float mouseLocalY, float viewportHeight, float contentHeight, float scrollStep, float scrollMultiplier = 1f, float edgeThreshold = 0)
        {
            float maxOffset = Math.Max(0, contentHeight - viewportHeight);
            if (viewportHeight <= 0 || maxOffset <= 0) return currentOffset;

            float step = scrollStep * (mouseLocalY < 0 || mouseLocalY > viewportHeight ? scrollMultiplier : 1f);

            if ((mouseLocalY <= edgeThreshold || mouseLocalY < 0) && currentOffset > 0)
                return Math.Max(0, currentOffset - step);
            if ((mouseLocalY >= viewportHeight - edgeThreshold || mouseLocalY > viewportHeight) && currentOffset < maxOffset)
                return Math.Min(maxOffset, currentOffset + step);

            return currentOffset;
        }

        #endregion

        #region Photon: Control Layout Helpers

        public static float GetMeasuredStackWidth(IReadOnlyList<Control> children)
        {
            float total = 0f;
            foreach (Control child in children)
                if (child != null) total += child.DrawRect.W + child.MarginExtent.Horizontal;
            return total;
        }
        public static float GetMeasuredStackHeight(IReadOnlyList<Control> children)
        {
            float total = 0f;
            foreach (Control child in children)
                if (child != null) total += child.DrawRect.H + child.MarginExtent.Vertical;
            return total;
        }

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
        public static bool DrawTextureRotated(Window window, IntPtr texture, SDL.FRect? sourceRect, SDL.FRect destination, double angle, SDL.FPoint center, SDL.FlipMode flip, IntPtr target, SDL.Rect? clipRect = null)
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
                result = SDL.RenderTextureRotated(renderer, texture, sourceRect.Value, in destination, angle, center, flip);
            else
                result = SDL.RenderTextureRotated(renderer, texture, IntPtr.Zero, in destination, angle, center, flip);

            ApplyControlClipRect(window, previousClipRect);

            if (target != IntPtr.Zero)
                SDL.SetRenderTarget(renderer, previousTarget);

            if (!result)
                throw new InvalidOperationException($"RenderTextureRotated failed: {SDL.GetError()}");

            return result;
        }

        public static IntPtr CreateTextureFromSurface(Window window, IntPtr surface)
        {
            IntPtr tex = SDL.CreateTextureFromSurface(window.Renderer, surface);

            SDL.DestroySurface(surface);

            if (tex == IntPtr.Zero)
                throw new InvalidOperationException($"SDL.CreateTextureFromSurface failed: {SDL.GetError()}");

            return tex;
        }
        public static IntPtr CompositeTextures(Window window, IntPtr texA, IntPtr texB, int offsetX, int offsetY)
        {
            SDL.GetTextureSize(texB, out float w, out float h);

            int fullW = (int)(w + offsetX * 2);
            int fullH = (int)(h + offsetY * 2);

            IntPtr target = SDL.CreateTexture(
                window.Renderer,
                SDL.PixelFormat.RGBA8888,
                SDL.TextureAccess.Target,
                fullW,
                fullH);

            if (target == IntPtr.Zero)
                throw new InvalidOperationException($"SDL.CreateTexture failed: {SDL.GetError()}");

            IntPtr previousTarget = SDL.GetRenderTarget(window.Renderer);

            SDL.SetTextureBlendMode(target, SDL.BlendMode.Blend);
            SDL.SetTextureBlendMode(texA, SDL.BlendMode.Blend);
            SDL.SetTextureBlendMode(texB, SDL.BlendMode.Blend);

            SDL.SetRenderTarget(window.Renderer, target);
            SDL.SetRenderDrawColor(window.Renderer, 0, 0, 0, 0);
            SDL.RenderClear(window.Renderer);

            SDL.FRect dstA = new() { X = 0, Y = 0, W = fullW, H = fullH };
            SDL.RenderTexture(window.Renderer, texA, IntPtr.Zero, dstA);

            SDL.FRect dstB = new() { X = offsetX, Y = offsetY, W = w, H = h };
            SDL.RenderTexture(window.Renderer, texB, IntPtr.Zero, dstB);

            SDL.SetRenderTarget(window.Renderer, previousTarget);

            return target;
        }
        public static IntPtr CropTexture(Window window, IntPtr sourceTexture, SDL.FRect cropRect)
        {
            if (sourceTexture == IntPtr.Zero)
                return IntPtr.Zero;

            SDL.GetTextureSize(sourceTexture, out float texW, out float texH);

            if (cropRect.X >= texW || cropRect.Y >= texH || cropRect.W <= 0 || cropRect.H <= 0)
                return IntPtr.Zero;

            if (cropRect.X <= 0 && cropRect.Y <= 0 && cropRect.W >= texW && cropRect.H >= texH)
                return IntPtr.Zero;

            cropRect.X = Math.Clamp(cropRect.X, 0, texW);
            cropRect.Y = Math.Clamp(cropRect.Y, 0, texH);
            cropRect.W = Math.Min(cropRect.W, texW - cropRect.X);
            cropRect.H = Math.Min(cropRect.H, texH - cropRect.Y);

            IntPtr croppedTexture = SDL.CreateTexture(
                window.Renderer,
                SDL.PixelFormat.RGBA8888,
                SDL.TextureAccess.Target,
                (int)cropRect.W,
                (int)cropRect.H);

            if (croppedTexture == IntPtr.Zero)
                throw new InvalidOperationException($"SDL.CreateTexture (croppedTexture) failed: {SDL.GetError()}");

            IntPtr previousTarget = SDL.GetRenderTarget(window.Renderer);
            SDL.SetTextureBlendMode(croppedTexture, SDL.BlendMode.Blend);

            SDL.SetRenderTarget(window.Renderer, croppedTexture);
            SDL.SetRenderDrawColor(window.Renderer, 0, 0, 0, 0);
            SDL.RenderClear(window.Renderer);

            SDL.FRect dst = new() { X = 0, Y = 0, W = cropRect.W, H = cropRect.H };
            SDL.RenderTexture(window.Renderer, sourceTexture, cropRect, dst);

            SDL.SetRenderTarget(window.Renderer, previousTarget);

            return croppedTexture;
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
        public static void DrawControlTexture<T>(T control, IntPtr texture, SDL.FRect destination, SDL.FRect? sourceRect = null, SDL.Rect? clipRect = null) where T : Control, IControlProperties
        {
            EnsureRootWindow(control);

            SDL.GetTextureAlphaMod(texture, out byte originalAlpha);

            byte newAlpha = (byte)(originalAlpha * control.Opacity);

            SDL.SetTextureAlphaMod(texture, newAlpha);

            DrawTexture(control.Window!, texture, destination, control.Window!.BackTexture, sourceRect, clipRect);

            SDL.SetTextureAlphaMod(texture, originalAlpha);
        }

        public static void DrawControlTextureRotated<T>(T control, IntPtr texture, SDL.FRect destination, double angle, SDL.FPoint center, SDL.FlipMode flip, SDL.Rect? clipRect = null) where T : Control, IControlProperties
        {
            EnsureRootWindow(control);

            SDL.GetTextureAlphaMod(texture, out byte originalAlpha);

            byte newAlpha = (byte)(originalAlpha * control.Opacity);

            SDL.SetTextureAlphaMod(texture, newAlpha);

            DrawTextureRotated(control.Window!, texture, null, destination, angle, center, flip, control.Window!.BackTexture, clipRect);

            SDL.SetTextureAlphaMod(texture, originalAlpha);
        }
        public static void DrawControlTextureRotated<T>(T control, IntPtr texture, SDL.FRect destination, SDL.FRect? sourceRect, double angle, SDL.FPoint center, SDL.FlipMode flip, SDL.Rect? clipRect = null) where T : Control, IControlProperties
        {
            EnsureRootWindow(control);

            SDL.GetTextureAlphaMod(texture, out byte originalAlpha);

            byte newAlpha = (byte)(originalAlpha * control.Opacity);

            SDL.SetTextureAlphaMod(texture, newAlpha);

            DrawTextureRotated(control.Window!, texture, sourceRect, destination, angle, center, flip, control.Window!.BackTexture, clipRect);

            SDL.SetTextureAlphaMod(texture, originalAlpha);
        }

        #endregion

        #region Photon: Font Helpers

        public static float GetControlLinePixelHeight(IList<TextControlLineData> lines, int start = 0, int? end = null)
        {
            if (lines == null || lines.Count == 0) return 0f;

            int actualEnd = end ?? lines.Count;

            if (actualEnd > lines.Count) actualEnd = lines.Count;
            if (start < 0) start = 0;

            float sum = 0f;

            for (int i = start; i < actualEnd; i++)
            {
                sum += lines[i].PixelHeight;
            }

            return sum;
        }
        public static float GetControlLineMaxPixelWidth(IList<TextControlLineData> lines, int start = 0, int? end = null)
        {
            if (lines == null || lines.Count == 0) return 0f;

            int actualEnd = end ?? lines.Count;

            if (actualEnd > lines.Count) actualEnd = lines.Count;
            if (start < 0) start = 0;

            float maxWidth = 0f;

            for (int i = start; i < actualEnd; i++)
            {
                float w = lines[i].PixelWidth;

                if (w > maxWidth)
                    maxWidth = w;
            }

            return maxWidth;
        }

        public static (int line, int column) GetLineAndColumnFromControlLine(List<TextControlLineData> cache, string text, int textIndex)
        {
            if (cache == null || cache.Count == 0)
                return (0, 0);

            if (textIndex < 0) textIndex = 0;
            if (textIndex > text.Length) textIndex = text.Length;

            for (int i = 0; i < cache.Count; i++)
            {
                TextControlLineData lineInfo = cache[i];

                if (textIndex >= lineInfo.StartIndex && textIndex <= lineInfo.EndIndex)
                {
                    int column = textIndex - lineInfo.StartIndex;

                    return (i, column);
                }
            }

            TextControlLineData lastLine = cache[^1];

            return (cache.Count - 1, lastLine.EndIndex - lastLine.StartIndex);
        }

        public static int GetIndexFromPixelHeight(List<TextControlLineData> lines, float pixelHeight)
        {
            if (pixelHeight < 0) return 0;

            float acc = 0f;

            for (int i = 0; i < lines.Count; i++)
            {
                float next = acc + lines[i].PixelHeight;

                if (pixelHeight < next) return i;

                acc = next;
            }

            return lines.Count - 1;
        }
        public static int GetColumnFromPixelWidth(IntPtr font, string text, float targetX, FontMetrics metrics)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            for (int i = 0; i < text.Length; i++)
            {
                float start = metrics.MeasureString(font, text[..i]).Width;

                float end = metrics.MeasureString(font, text[..(i + 1)]).Width;

                if (targetX >= start && targetX < end)
                    return i;
            }

            return text.Length;
        }

        public static int FindCharCountWithinPixelWidth(IntPtr font, string text, int startIndex, int pixelWidth, FontMetrics metrics)
        {
            if (string.IsNullOrEmpty(text) || font == IntPtr.Zero)
                return 0;

            string slice = text[startIndex..];

            if (metrics.MeasureString(font, slice, (uint)slice.Length, pixelWidth, out int _, out ulong measuredLength))
                return (int)measuredLength;

            return text.Length - startIndex;
        }
        public static int FindWordBoundaryWrap(IntPtr font, string text, int startIndex, int pixelWidth, FontMetrics metrics)
        {
            if (startIndex >= text.Length) return text.Length;

            string slice = text[startIndex..];
            if (metrics.MeasureString(font, slice, (uint)slice.Length, pixelWidth, out int _, out ulong measuredLength))
            {
                int fitCount = (int)Math.Min(measuredLength, (ulong)slice.Length);

                // Binary search for exact fit point
                int left = 0, right = fitCount;
                while (left < right)
                {
                    int mid = (left + right + 1) / 2;
                    Size size = metrics.MeasureString(font, slice[0..mid]);
                    if (size.Width <= pixelWidth) left = mid;
                    else right = mid - 1;
                }

                fitCount = left;

                // Find last space within safe fit
                int lastSpace = slice[0..fitCount].LastIndexOf(' ');
                return lastSpace != -1 ? startIndex + lastSpace + 1 : startIndex + fitCount;
            }
            return startIndex;
        }
        public static List<TextControlLineData> GetTextControlData(IntPtr font, string text, TextControlWrapType wrapType, int wrapWidth, FontMetrics metrics)
        {
            if (string.IsNullOrEmpty(text)) return [];

            if (font == IntPtr.Zero)
                throw new InvalidOperationException("GetTextControlLines: font is null.");

            List<TextControlLineData> lines = [];

            int globalIndex = 0;
            string[] paragraphs = text.Split('\n');

            foreach (string paragraph in paragraphs)
            {
                if (paragraph.Length == 0) continue;

                int start = 0;

                while (start < paragraph.Length)
                {
                    Size singleLineSize = metrics.MeasureString(font, paragraph[start..]);

                    int effectiveWrap = wrapWidth > 0 ? wrapWidth : int.MaxValue;

                    if (wrapType == TextControlWrapType.None || singleLineSize.Width <= effectiveWrap)
                    {
                        lines.Add(new TextControlLineData(
                            globalIndex + start,
                            globalIndex + paragraph.Length,
                            singleLineSize.Width,
                            singleLineSize.Height));

                        start = paragraph.Length;

                        continue;
                    }

                    int charsToTake;

                    switch (wrapType)
                    {
                        case TextControlWrapType.Soft:
                        case TextControlWrapType.AutoSoft:
                            {
                                int wrapPos = FindWordBoundaryWrap(font, paragraph, start, effectiveWrap, metrics);
                                charsToTake = wrapPos > start ? wrapPos - start : FindCharCountWithinPixelWidth(font, paragraph, start, effectiveWrap, metrics);
                                break;
                            }
                        case TextControlWrapType.Hard:
                        case TextControlWrapType.AutoHard:
                            {
                                charsToTake = FindCharCountWithinPixelWidth(font, paragraph, start, effectiveWrap, metrics);
                                break;
                            }
                        default:
                            charsToTake = paragraph.Length - start;
                            break;
                    }

                    charsToTake = Math.Clamp(charsToTake, 0, paragraph.Length - start);

                    int visualEnd = start + charsToTake;
                    string candidate = paragraph[start..visualEnd];
                    Size lineSize = metrics.MeasureString(font, candidate);

                    while (lineSize.Width > effectiveWrap && visualEnd > start)
                    {
                        visualEnd--;
                        candidate = paragraph[start..visualEnd];
                        lineSize = metrics.MeasureString(font, candidate);
                    }

                    int advanceAmount = visualEnd - start;

                    if (wrapType == TextControlWrapType.Soft || wrapType == TextControlWrapType.AutoSoft)
                    {
                        int displayEnd = visualEnd;

                        while (displayEnd > start && char.IsWhiteSpace(paragraph[displayEnd - 1]))
                            displayEnd--;

                        string displayLine = paragraph[start..displayEnd];

                        lineSize = metrics.MeasureString(font, displayLine);

                        lines.Add(new TextControlLineData(
                            globalIndex + start,
                            globalIndex + displayEnd,
                            lineSize.Width,
                            lineSize.Height));
                    }
                    else
                    {
                        string finalLine = paragraph[start..visualEnd];
                        lineSize = metrics.MeasureString(font, finalLine);

                        lines.Add(new TextControlLineData(
                            globalIndex + start,
                            globalIndex + visualEnd,
                            lineSize.Width,
                            lineSize.Height));
                    }

                    start += advanceAmount;
                }

                globalIndex += paragraph.Length + (paragraph != paragraphs[^1] ? 1 : 0);
            }

            return lines;
        }

        public static IntPtr CreateFontSurface(IntPtr font, TextProperties props, string content)
        {
            if (font == IntPtr.Zero)
                throw new ArgumentException("Font handle is invalid.", nameof(font));

            TTF.FontStyleFlags originalStyle = TTF.GetFontStyle(font);
            TTF.HintingFlags originalHinting = TTF.GetFontHinting(font);
            bool originalKerning = TTF.GetFontKerning(font);
            int originalOutline = TTF.GetFontOutline(font);

            TTF.SetFontStyle(font, props.FontStyleFlags);
            TTF.SetFontHinting(font, props.FontHintingFlags);
            TTF.SetFontKerning(font, props.FontKerning);
            TTF.SetFontOutline(font, props.TextOutlineSize);

            IntPtr surface;

            switch (props.FontRenderMode)
            {
                case FontRenderMode.Solid:
                    surface = TTF.RenderTextSolid(font, content, 0, props.TextForegroundColor);
                    break;

                case FontRenderMode.Shaded:
                    surface = TTF.RenderTextShaded(font, content, 0, props.TextForegroundColor, props.TextBackgroundColor);
                    break;

                case FontRenderMode.Blended:
                default:
                    surface = TTF.RenderTextBlended(font, content, 0, props.TextForegroundColor);
                    break;
            }

            TTF.SetFontStyle(font, originalStyle);
            TTF.SetFontHinting(font, originalHinting);
            TTF.SetFontKerning(font, originalKerning);
            TTF.SetFontOutline(font, originalOutline);

            if (surface == IntPtr.Zero)
                throw new InvalidOperationException($"TTF render failed: {SDL.GetError()}");

            return surface;
        }
        public static IntPtr CreateFontTexture(Window window, IntPtr font, TextProperties props, string content)
        {
            if (window.Renderer == IntPtr.Zero)
                throw new InvalidOperationException("Renderer is not initialized.");

            IntPtr surface = CreateFontSurface(font, props, content);
            IntPtr texture = CreateTextureFromSurface(window, surface);

            return texture;
        }

        #endregion
    }
}