using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Events;
using PhotonUI.Events.Platform;
using PhotonUI.Extensions;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;

namespace PhotonUI.Controls.Content
{
    public record TextControlCaretData(int Line, int Column, int TextIndex, float PixelX, float PixelY);

    public partial class TextBlockSelectable(IServiceProvider serviceProvider, IBindingService bindingService, IFontService fontService)
        : TextBlock(serviceProvider, bindingService, fontService), ITextSelectionProperties
    {
        protected Size HotspotOffset = new(0, 2);

        protected bool IsInsideScrollbarViewport = false;

        protected TextControlCaretData CaretCache = new(0, 0, 0, 0, 0);

        protected bool IsMouseDown = false;
        protected ulong MouseDownTick = 0;
        protected readonly ulong ClickThresholdMs = 150;

        protected bool IsShiftDown = false;
        protected bool IsSelecting = false;
        protected int SelectionIndex = -1;

        public bool HasSelection => this.SelectionIndex > -1;

        #region TextSelectionBlock: Style Properties

        [ObservableProperty] private SDL.Color selectionColor = TextSelectionProperties.Default.SelectionColor;

        #endregion

        #region TextSelectionBlock: Framework

        public override void FrameworkRender(Window window, SDL.Rect? clipRect = null)
        {
            if (this.IsVisible)
            {
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                if (effectiveClipRect.HasValue)
                {
                    if (this.IsTextDirty)
                    {
                        this.RebuildTextureCache(window, this.FromControl<TextProperties>());
                        this.IsTextDirty = false;
                    }

                    Photon.ApplyControlClipRect(window, effectiveClipRect);
                    Photon.DrawControlBackground(this);
                    Photon.ApplyControlClipRect(window, this.ScrollbarViewport.ToRect());

                    this.DrawSelection(window);
                    this.DrawText(window, this.ScrollbarViewport.ToRect());

                    this.ScrollbarBehavior.OnRender(window, this.DrawRect.ToRect());

                    Photon.ApplyControlClipRect(window, clipRect);
                }
            }
        }

        #endregion

        #region TextSelectionBlock: Hooks

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            if (this.Window == null)
                throw new InvalidOperationException($"Control '{this.Name}' is not associated to a RootWindow.");

            base.OnPropertyChanged(e);

            bool invalidateMeasure = false;
            bool invalidateLayout = false;
            bool invalidateRender = false;

            switch (e.PropertyName)
            {
                case nameof(this.SelectionColor):
                    invalidateRender = true;
                    break;
            }

            if (invalidateMeasure)
                this.Parent?.RequestMeasure();

            if (invalidateLayout)
                this.Parent?.RequestArrange();

            if (invalidateRender)
                this.RequestRenderWithFlags(false, false, true);
        }

        public override void OnEvent(Window window, FrameworkEventArgs e)
        {
            if (e.Handled == true || e.Preview == true) return;

            base.OnEvent(window, e);

            if (e is not PlatformEventArgs platformEvent) return;

            switch (platformEvent.NativeEvent.Type)
            {
                case (uint)SDL.EventType.MouseMotion:
                    this.HandleCursor(platformEvent);
                    if (this.IsMouseDown)
                        this.HandleMouseMove(platformEvent.NativeEvent.Motion);
                    platformEvent.Handled = true;
                    break;

                case (uint)SDL.EventType.MouseButtonDown:
                    this.HandleMouseDown(platformEvent.NativeEvent.Button);
                    platformEvent.Handled = true;
                    break;

                case (uint)SDL.EventType.MouseButtonUp:
                    this.HandleMouseUp(platformEvent.NativeEvent.Button);
                    platformEvent.Handled = true;
                    break;

                case (uint)SDL.EventType.KeyDown:
                    this.OnKeyDown(platformEvent.NativeEvent.Key);
                    platformEvent.Handled = true;
                    break;

                case (uint)SDL.EventType.KeyUp:
                    this.OnKeyUp(platformEvent.NativeEvent.Key);
                    platformEvent.Handled = true;
                    break;
            }
        }

        #endregion

        #region TextSelectionBlock: Input Handlers

        protected virtual void ApplyKeyDrivenViewportScroll()
        {
            int caretIndex = this.CaretCache.TextIndex;
            int lineIdx = this.LineDataCache.FindIndex(l => caretIndex >= l.StartIndex && caretIndex <= l.EndIndex);

            if (lineIdx < 0 || this.ScrollbarViewport.W <= 0) return;

            TextControlLineData line = this.LineDataCache[lineIdx];

            Size stringSize = this.FontMetrics.MeasureString(this.Font, this.Text[line.StartIndex..caretIndex]);

            float caretX = stringSize.Width;
            float caretViewportX = caretX - this.ViewportCache.HorizontalOffset;
            float rightEdge = this.ScrollbarViewport.W;

            float newHorizontalOffset = this.ViewportCache.HorizontalOffset;
            int newVerticalOffset = this.ViewportCache.StartLine;

            if (caretViewportX < 0)
            {
                newHorizontalOffset = (int)Math.Max(0, caretX - (this.ScrollbarViewport.W - 1));
            }
            else if (caretViewportX >= rightEdge)
            {
                newHorizontalOffset = (int)Math.Min(line.PixelWidth - this.ScrollbarViewport.W, caretX - this.ScrollbarViewport.W + 1);
            }

            int viewportStart = this.ViewportCache.StartLine;
            int viewportEnd = viewportStart + this.ViewportCache.VisibleLines - 1;

            if (lineIdx < viewportStart)
            {
                newVerticalOffset = lineIdx;
            }
            else if (lineIdx > viewportEnd)
            {
                newVerticalOffset = Math.Min(
                    lineIdx - (this.ViewportCache.VisibleLines - 1),
                    Math.Max(0, this.LineDataCache.Count - this.ViewportCache.VisibleLines)
                );
            }

            this.ViewportCache = this.ViewportCache with
            {
                HorizontalOffset = newHorizontalOffset,
                StartLine = newVerticalOffset
            };

            if (this.ScrollbarBehavior != null)
            {
                this.ScrollbarBehavior.HorizontalOffset = newHorizontalOffset;
                this.ScrollbarBehavior.VerticalOffset = newVerticalOffset;
            }

            this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true);
        }

        protected virtual void ApplyMouseDrivenViewportScroll(float mouseX, float mouseY, float edgeThresholdX = 0, float edgeThresholdY = 0)
        {
            if (this.LineDataCache == null || this.LineDataCache.Count == 0)
                return;

            float newHorizontalOffset = this.ViewportCache.HorizontalOffset;
            int newVerticalOffset = this.ViewportCache.StartLine;

            if (this.ScrollbarViewport.W > 0)
            {
                float localX = mouseX - (this.ScrollbarViewport.X - this.DrawRect.X);
                float contentWidth = this.LineDataCache.Max(l => l.PixelWidth);
                float viewportWidth = this.ScrollbarViewport.W;

                newHorizontalOffset = Photon.GetEdgeScrollHorizontal(
                    currentOffset: newHorizontalOffset,
                    mouseLocalX: localX,
                    viewportWidth: viewportWidth,
                    contentWidth: contentWidth,
                    scrollStep: this.ScrollStepX,
                    scrollMultiplier: this.ScrollStepMultiplierX,
                    edgeThreshold: edgeThresholdX
                );
            }

            if (this.ScrollbarViewport.H > 0 && this.ViewportCache.VisibleLines > 0)
            {
                float localY = mouseY - (this.ScrollbarViewport.Y - this.DrawRect.Y);
                float contentHeight = (this.LineDataCache.Count + 1) * this.ScrollStepY;
                float viewportHeight = this.ScrollbarViewport.H;

                float newVerticalOffsetFloat = Photon.GetEdgeScrollVertical(
                    currentOffset: newVerticalOffset * this.ScrollStepY,
                    mouseLocalY: localY,
                    viewportHeight: viewportHeight,
                    contentHeight: contentHeight,
                    scrollStep: this.ScrollStepY,
                    scrollMultiplier: this.ScrollStepMultiplierY,
                    edgeThreshold: edgeThresholdY
                );

                newVerticalOffset = (int)(newVerticalOffsetFloat / this.ScrollStepY);
            }

            this.ViewportCache = this.ViewportCache with
            {
                HorizontalOffset = newHorizontalOffset,
                StartLine = newVerticalOffset
            };

            if (this.ScrollbarBehavior != null)
            {
                this.ScrollbarBehavior.HorizontalOffset = newHorizontalOffset;
                this.ScrollbarBehavior.VerticalOffset = newVerticalOffset * this.ScrollStepY;
            }

            this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true);
        }
        protected virtual void ApplyMouseDrivenSelection(float localX, float localY)
        {
            if (!this.IsSelecting)
                return;

            int viewportStart = this.ViewportCache.StartLine;

            List<TextControlLineData> visibleLines = [];

            int start = viewportStart;
            int end = Math.Min(this.LineDataCache.Count, viewportStart + this.ViewportCache.VisibleLines);

            for (int i = start; i < end; i++)
                visibleLines.Add(this.LineDataCache[i]);

            int lineIndexWithinViewport = Photon.GetIndexFromPixelHeight(visibleLines, localY);
            int actualLineIndex = viewportStart + lineIndexWithinViewport;

            if (actualLineIndex < 0 || actualLineIndex >= this.LineDataCache.Count)
                return;

            TextControlLineData line = this.LineDataCache[actualLineIndex];

            int columnIndex = Photon.GetColumnFromPixelWidth(
                this.Font,
                this.Text[line.StartIndex..line.EndIndex],
                this.ViewportCache.HorizontalOffset + localX,
                this.FontMetrics);

            this.CaretCache = this.CaretCache with
            {
                Line = actualLineIndex,
                Column = columnIndex,
                TextIndex = line.StartIndex + columnIndex,
                PixelX = localX,
                PixelY = Photon.GetControlLinePixelHeight(visibleLines, 0, lineIndexWithinViewport),
            };

            this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true);
        }

        protected virtual void HandleMouseDown(SDL.MouseButtonEvent e)
        {
            if (!this.IsInsideScrollbarViewport) return;

            SDL.FPoint adjusted = Photon.ApplyHotspotOffset(
                new SDL.FPoint { X = e.X, Y = e.Y },
                this.HotspotOffset
            );

            float mouseX = adjusted.X - this.ScrollbarViewport.X;
            float mouseY = adjusted.Y - this.ScrollbarViewport.Y;

            if (mouseX < 0 || mouseY < 0)
                return;

            float adjustedY = mouseY;

            int viewportStart = this.ViewportCache.StartLine;

            List<TextControlLineData> visibleLines = [.. this.LineDataCache
                .Skip(viewportStart)
                .Take(this.ViewportCache.VisibleLines)];

            int lineIndexWithinViewport = Photon.GetIndexFromPixelHeight(visibleLines, adjustedY);

            int actualLineIndex = viewportStart + lineIndexWithinViewport;
            TextControlLineData line = this.LineDataCache[actualLineIndex];
            float textRelativeX = mouseX - this.ViewportCache.HorizontalOffset;

            int columnIndex = Photon.GetColumnFromPixelWidth(
                this.Font,
                this.Text[line.StartIndex..line.EndIndex],
                textRelativeX,
                this.FontMetrics);

            int caretPos = line.StartIndex + columnIndex;

            float pixelY = Photon.GetControlLinePixelHeight(this.LineDataCache, viewportStart, actualLineIndex);

            this.CaretCache = new TextControlCaretData(
                Line: actualLineIndex,
                Column: columnIndex,
                TextIndex: caretPos,
                PixelX: mouseX,
                PixelY: pixelY
            );

            this.IsMouseDown = true;
            this.MouseDownTick = SDL.GetTicks();

            if (!this.IsShiftDown)
            {
                this.SelectionIndex = caretPos;
                this.IsSelecting = true;
            }
            else
            {
                this.IsSelecting = true;
            }

            this.Window?.CaptureMouse(this);
        }
        protected virtual void HandleMouseMove(SDL.MouseMotionEvent e)
        {
            if (!this.IsMouseDown) return;

            SDL.FPoint adjusted = Photon.ApplyHotspotOffset(
                new SDL.FPoint { X = e.X, Y = e.Y },
                this.HotspotOffset
            );

            float mouseX = adjusted.X - this.ScrollbarViewport.X;
            float mouseY = adjusted.Y - this.ScrollbarViewport.Y;

            this.ApplyMouseDrivenViewportScroll(mouseX, mouseY);
            this.ApplyMouseDrivenSelection(mouseX, mouseY);
        }
        protected virtual void HandleMouseUp(SDL.MouseButtonEvent e)
        {
            if (!this.IsMouseDown) return;

            ulong elapsed = SDL.GetTicks() - this.MouseDownTick;

            if (!this.IsShiftDown && elapsed < this.ClickThresholdMs)
            {
                this.IsSelecting = false;
                this.SelectionIndex = -1;
            }

            this.IsMouseDown = false;

            this.Window?.ReleaseMouse();

            this.RequestRenderWithFlags(textDirty: true);
        }

        protected virtual void HandleArrowUp()
        {
            if (this.HasSelection && !this.IsSelecting)
            {
                int caretIndex = Math.Min(this.CaretCache.TextIndex, this.SelectionIndex);
                this.SelectionIndex = -1;
                this.CaretCache = this.CaretCache with { TextIndex = caretIndex };
            }
            else if (this.CaretCache.Line > 0)
            {
                int targetLine = this.CaretCache.Line - 1;

                TextControlLineData prevLine = this.LineDataCache[targetLine];

                int targetColumn = Math.Min(this.CaretCache.Column, prevLine.EndIndex - prevLine.StartIndex);

                this.CaretCache = this.CaretCache with
                {
                    Line = targetLine,
                    Column = targetColumn,
                    TextIndex = prevLine.StartIndex + targetColumn
                };
            }

            this.ApplyKeyDrivenViewportScroll();
        }
        protected virtual void HandleArrowDown()
        {
            if (this.HasSelection && !this.IsSelecting)
            {
                int caretIndex = Math.Max(this.CaretCache.TextIndex, this.SelectionIndex);
                this.SelectionIndex = -1;
                this.CaretCache = this.CaretCache with { TextIndex = caretIndex };
            }
            else if (this.CaretCache.Line < this.LineDataCache.Count - 1)
            {
                int targetLine = this.CaretCache.Line + 1;
                TextControlLineData nextLine = this.LineDataCache[targetLine];

                int lineLength = nextLine.EndIndex - nextLine.StartIndex;
                int targetColumn = Math.Min(this.CaretCache.Column, Math.Max(0, lineLength - 1));

                this.CaretCache = this.CaretCache with
                {
                    Line = targetLine,
                    Column = targetColumn,
                    TextIndex = nextLine.StartIndex + targetColumn
                };
            }

            this.ApplyKeyDrivenViewportScroll();
        }
        protected virtual void HandleArrowLeft()
        {
            if (this.HasSelection && !this.IsSelecting)
            {
                int caretIndex = Math.Min(this.CaretCache.TextIndex, this.SelectionIndex);
                this.SelectionIndex = -1;
                this.CaretCache = this.CaretCache with { TextIndex = caretIndex };
            }
            else if (this.CaretCache.TextIndex > 0)
            {
                int newIndex = this.CaretCache.TextIndex - 1;
                int lineIdx = this.LineDataCache.FindIndex(l => newIndex >= l.StartIndex && newIndex <= l.EndIndex);
                int column = newIndex - this.LineDataCache[lineIdx].StartIndex;

                this.CaretCache = this.CaretCache with
                {
                    TextIndex = newIndex,
                    Line = lineIdx,
                    Column = column
                };
            }

            this.ApplyKeyDrivenViewportScroll();
        }
        protected virtual void HandleArrowRight()
        {
            if (this.HasSelection && !this.IsSelecting)
            {
                int caretIndex = Math.Max(this.CaretCache.TextIndex, this.SelectionIndex);
                this.SelectionIndex = -1;
                this.CaretCache = this.CaretCache with { TextIndex = caretIndex };
            }
            else if (this.CaretCache.TextIndex < this.Text.Length)
            {
                int newIndex = this.CaretCache.TextIndex + 1;
                int lineIdx = this.LineDataCache.FindIndex(l => newIndex >= l.StartIndex && newIndex <= l.EndIndex);
                int column = newIndex - this.LineDataCache[lineIdx].StartIndex;

                this.CaretCache = this.CaretCache with
                {
                    TextIndex = newIndex,
                    Line = lineIdx,
                    Column = column
                };
            }

            this.ApplyKeyDrivenViewportScroll();
        }

        protected virtual void HandleHome()
        {
            this.CaretCache = this.CaretCache with
            {
                TextIndex = 0,
                Line = 0,
                Column = 0,
                PixelX = 0f,
                PixelY = 0f
            };

            this.ViewportCache = this.ViewportCache with
            {
                StartLine = 0
            };

            this.ApplyKeyDrivenViewportScroll();
        }
        protected virtual void HandleEnd()
        {
            int lastLine = Math.Max(0, this.LineDataCache.Count - 1);

            TextControlLineData lineInfo = this.LineDataCache[lastLine];

            this.CaretCache = this.CaretCache with
            {
                TextIndex = this.Text.Length,
                Line = lastLine,
                Column = Math.Max(0, lineInfo.EndIndex - lineInfo.StartIndex),
                PixelX = lineInfo.PixelWidth,
                PixelY = Photon.GetControlLinePixelHeight(this.LineDataCache)
            };

            this.ViewportCache = this.ViewportCache with
            {
                StartLine = Math.Max(0, this.LineDataCache.Count - this.ViewportCache.VisibleLines)
            };

            this.ApplyKeyDrivenViewportScroll();
        }
        protected virtual void HandleEscape()
        {
            this.SelectionIndex = -1;
            this.IsSelecting = false;

            this.ApplyKeyDrivenViewportScroll();
        }

        protected virtual void HandleSelectAll()
        {
            if (this.Text.Length > 0)
            {
                this.SelectionIndex = this.Text.Length;
                this.CaretCache = this.CaretCache with
                {
                    TextIndex = 0,
                    Line = 0,
                    Column = 0,
                    PixelX = 0f,
                    PixelY = 0f
                };
                this.ViewportCache = this.ViewportCache with
                {
                    StartLine = 0
                };
            }
        }
        protected virtual void HandleCopy()
        {
            if (this.HasSelection)
            {
                int start = Math.Min(this.CaretCache.TextIndex, this.SelectionIndex);
                int end = Math.Max(this.CaretCache.TextIndex, this.SelectionIndex);

                string selected = this.Text[start..end];

                SDL.SetClipboardText(selected);
            }
        }

        protected virtual void HandleCursor(PlatformEventArgs e)
        {
            bool inside = Photon.IHitTest(this.ScrollbarViewport.ToRect(), (int)e.NativeEvent.Motion.X, (int)e.NativeEvent.Motion.Y);

            if (inside && !this.IsInsideScrollbarViewport)
            {
                SDL.SetCursor(SDL.CreateSystemCursor(SDL.SystemCursor.Text));

                this.IsInsideScrollbarViewport = true;
            }
            else if (!inside && this.IsInsideScrollbarViewport)
            {
                SDL.SetCursor(SDL.CreateSystemCursor(this.Cursor));

                this.IsInsideScrollbarViewport = false;
            }
        }

        protected virtual void OnKeyDown(SDL.KeyboardEvent key)
        {
            bool ctrl = (key.Mod & SDL.Keymod.Ctrl) != 0;

            switch (key.Key)
            {
                case SDL.Keycode.LShift:
                case SDL.Keycode.RShift:
                    this.SelectionIndex = this.SelectionIndex >= 0 ? this.SelectionIndex : this.CaretCache.TextIndex;
                    this.IsSelecting = true;
                    this.IsShiftDown = true;
                    break;

                case SDL.Keycode.A:
                    if (ctrl) this.HandleSelectAll();
                    break;

                case SDL.Keycode.C: if (ctrl) this.HandleCopy(); break;
                case SDL.Keycode.End: this.HandleEnd(); break;
                case SDL.Keycode.Home: this.HandleHome(); break;
                case SDL.Keycode.Escape: this.HandleEscape(); break;
                case SDL.Keycode.Left: this.HandleArrowLeft(); break;
                case SDL.Keycode.Right: this.HandleArrowRight(); break;
                case SDL.Keycode.Up: this.HandleArrowUp(); break;
                case SDL.Keycode.Down: this.HandleArrowDown(); break;
            }
        }
        protected virtual void OnKeyUp(SDL.KeyboardEvent key)
        {
            if (key.Mod == SDL.Keymod.Ctrl) { }

            switch (key.Key)
            {
                case SDL.Keycode.LShift:
                case SDL.Keycode.RShift:
                    this.IsSelecting = false;
                    this.IsShiftDown = false;
                    break;
            }
        }

        #endregion

        #region TextSelectionBlock: Helpers

        protected virtual void DrawSelection(Window window)
        {
            if (!this.HasSelection) return;

            int startIndex = Math.Min(this.SelectionIndex, this.CaretCache.TextIndex);
            int endIndex = Math.Max(this.SelectionIndex, this.CaretCache.TextIndex);

            if (startIndex == endIndex) return;

            int startLine = this.ViewportCache.StartLine;
            int endLine = Math.Min(this.LineDataCache.Count, startLine + this.ViewportCache.VisibleLines);

            float[] cumHeights = new float[endLine - startLine + 1];

            for (int i = 0; i < endLine - startLine; i++)
                cumHeights[i + 1] = cumHeights[i] + this.LineDataCache[startLine + i].PixelHeight;

            for (int lineIdx = startLine; lineIdx < endLine; lineIdx++)
            {
                TextControlLineData lineInfo = this.LineDataCache[lineIdx];

                if (endIndex <= lineInfo.StartIndex || startIndex >= lineInfo.EndIndex) continue;

                int selStart = Math.Max(startIndex, lineInfo.StartIndex);
                int selEnd = Math.Min(endIndex, lineInfo.EndIndex);

                if (selStart >= selEnd) continue;

                int prefixLength = selStart - lineInfo.StartIndex;
                int selectLength = selEnd - selStart;

                Size prefixSize = this.FontMetrics.MeasureString(this.Font, this.Text.Substring(lineInfo.StartIndex, prefixLength));
                Size selectSize = this.FontMetrics.MeasureString(this.Font, this.Text.Substring(lineInfo.StartIndex + prefixLength, selectLength));

                int idxInViewport = lineIdx - startLine;
                float yWithinViewport = cumHeights[idxInViewport];

                SDL.FRect drawRect = new()
                {
                    X = prefixSize.Width - this.ViewportCache.HorizontalOffset + this.ScrollbarViewport.X,
                    Y = this.ScrollbarViewport.Y + yWithinViewport,
                    W = selectSize.Width,
                    H = lineInfo.PixelHeight
                };

                SDL.Color adjustedSelection = new()
                {
                    R = this.SelectionColor.R,
                    G = this.SelectionColor.G,
                    B = this.SelectionColor.B,
                    A = (byte)(this.SelectionColor.A * this.Opacity)
                };

                Photon.DrawRectangle(window, drawRect, adjustedSelection, this.Window?.BackTexture ?? default);
            }
        }

        #endregion
    }
}