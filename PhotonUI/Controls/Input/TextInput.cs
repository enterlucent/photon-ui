using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Controls.Content;
using PhotonUI.Events;
using PhotonUI.Events.Framework;
using PhotonUI.Extensions;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace PhotonUI.Controls.Input
{
    public partial class TextInput(IServiceProvider serviceProvider, IBindingService bindingService, IKeyBindingService keyBindingService, IFontService fontService)
        : TextBlockSelectable(serviceProvider, bindingService, keyBindingService, fontService), IBorderProperties, ITextCaretProperties
    {
        protected static readonly Random Random = new();
        protected DateTime LastInputTime;

        protected bool UndoBatchOpen = false;
        protected int UndoBatchTimeoutMs = 1000;
        protected readonly Stack<(string text, int caret)> UndoStack = new();
        protected readonly Stack<(string text, int caret)> RedoStack = new();

        protected bool IsCaretDirty = false;
        protected bool IsCaretVisible = true;
        protected ulong CaretLastTick = 0;
        protected SDL.FRect CaretRect = new();
        protected SDL.Color? CaretDrawColor = null;

        public override Thickness PaddingExtent
            => this.Padding + this.BorderThickness;

        #region TextInput: Style Properties

        [ObservableProperty] private BorderColors borderColors = BorderProperties.Default.BorderColors;
        [ObservableProperty] private Thickness borderThickness = BorderProperties.Default.BorderThickness;

        [ObservableProperty] private bool caretVisibility = TextCaretProperties.Default.CaretVisibility;
        [ObservableProperty] private int caretBlinkRate = TextCaretProperties.Default.CaretBlinkRate;
        [ObservableProperty] private int caretWidth = TextCaretProperties.Default.CaretWidth;
        [ObservableProperty] private SDL.Color caretColor = TextCaretProperties.Default.CaretColor;
        [ObservableProperty] private Thickness caretOutline = TextCaretProperties.Default.CaretOutline;
        [ObservableProperty] private BorderColors caretOutlineColor = TextCaretProperties.Default.CaretOutlineColor;

        [ObservableProperty] private bool growsWithInput = false;

        #endregion

        #region TextInput: Actions

        public Action<string>? SubmitAction { get; set; }

        public ICommand? OnSubmit { get; set; }

        #endregion

        #region TextInput: Framework

        public override void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            base.ApplyStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case IBorderProperties borderProps:
                        this.ApplyProperties(borderProps);
                        break;
                    case ITextCaretProperties textCaretProps:
                        this.ApplyProperties(textCaretProps);
                        break;
                }
            }
        }

        public override void RequestRender(bool invalidate = true)
        {
            this.IsRenderDirty = true;
            this.IsCaretDirty = true;
            this.IsTextDirty = true;
            this.ScrollbarBehavior.RequestRender();

            if (invalidate)
                Photon.InvalidateRenderChain(this);
        }
        public virtual void RequestRenderWithFlags(bool textDirty = false, bool scrollbarDirty = false, bool caretDirty = false, bool invalidate = true)
        {
            this.IsRenderDirty = true;
            this.IsTextDirty = textDirty;
            this.IsCaretDirty = caretDirty;

            if (scrollbarDirty == true)
                this.ScrollbarBehavior.RequestRender();

            if (invalidate)
                Photon.InvalidateRenderChain(this);
        }

        public override void FrameworkTick(Window window)
        {
            if (this.Window?.Focused != this) return;

            ulong ticks = SDL.GetTicks();

            if (ticks - this.CaretLastTick > (ulong)this.CaretBlinkRate)
            {
                this.CaretLastTick = ticks;
                this.IsCaretVisible = !this.IsCaretVisible;

                this.RequestRenderWithFlags(textDirty: true, caretDirty: true);
            }

            this.CheckUndoBatchTimer();
        }
        public override void FrameworkIntrinsic(Window window, Size content)
        {
            if (this.GrowsWithInput)
            {
                Size intrinsic = this.IntrinsicSize;

                float height = Photon.GetControlLinePixelHeight(this.LineDataCache);
                float clampedHeight = Math.Clamp(height, this.MinHeight, this.MaxHeight);

                this.IntrinsicSize.Height = clampedHeight;
                this.DrawRect.H = clampedHeight;
            }
        }
        public override void FrameworkRender(Window window, SDL.Rect? clipRect = null)
        {
            if (this.IsVisible)
            {
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                if (effectiveClipRect.HasValue)
                {
                    TextProperties tProps = this.FromControl<TextProperties>();
                    TextCaretProperties tcProps = this.FromControl<TextCaretProperties>();

                    Photon.ApplyControlClipRect(window, effectiveClipRect);
                    Photon.DrawControlBackground(this);
                    Photon.DrawControlBorder(this);
                    Photon.ApplyControlClipRect(window, this.ScrollbarViewport.ToRect());

                    if (this.IsTextDirty)
                    {
                        this.RebuildTextureCache(window, tProps);
                        this.IsTextDirty = false;
                    }

                    this.DrawSelection(window);
                    this.DrawText(window, this.ScrollbarViewport.ToRect());

                    if (this.IsCaretDirty)
                    {
                        this.RebuildCaretRect(this.ScrollbarViewport.ToRect(), tProps);
                        this.DrawCaret(window, tcProps);
                        this.IsCaretDirty = false;
                    }

                    this.ScrollbarBehavior.OnRender(window, this.DrawRect.Deflate(this.BorderThickness).ToRect());

                    Photon.ApplyControlClipRect(window, clipRect);
                }
            }
        }

        #endregion

        #region TextInput: Hooks

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
            bool invalidateCaret = false;

            switch (e.PropertyName)
            {
                case nameof(this.GrowsWithInput):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    break;

                case nameof(this.BorderThickness):
                    invalidateLayout = true;
                    invalidateRender = true;
                    break;

                case nameof(this.BorderColors):
                    invalidateRender = true;
                    break;

                case nameof(this.CaretVisibility):
                case nameof(this.CaretBlinkRate):
                case nameof(this.CaretWidth):
                case nameof(this.CaretColor):
                case nameof(this.CaretOutline):
                case nameof(this.CaretOutlineColor):
                    invalidateRender = true;
                    invalidateCaret = true;
                    break;
            }

            if (invalidateMeasure)
                this.Parent?.RequestMeasure();

            if (invalidateLayout)
                this.Parent?.RequestArrange();

            if (invalidateRender)
                this.RequestRenderWithFlags(caretDirty: invalidateCaret);
        }

        public override void OnEvent(Window window, FrameworkEventArgs e)
        {
            if (e.Handled || e.Preview) return;

            base.OnEvent(window, e);

            switch (e)
            {
                case FocusGainedEventArgs:
                    SDL.StartTextInput(window.Handle);
                    this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true, caretDirty: true);
                    break;

                case FocusLostEventArgs:
                    if (this.Window?.Focused != this)
                    {
                        SDL.StopTextInput(window.Handle);
                        SDL.SetCursor(SDL.CreateSystemCursor(this.Cursor));

                        this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true, caretDirty: true);
                    }
                    break;
            }

            if (e is not PlatformEventArgs platformEvent) return;

            switch (platformEvent.NativeEvent.Type)
            {
                case (uint)SDL.EventType.MouseButtonDown:
                    this.Window?.SetFocus(this);
                    platformEvent.Handled = true;
                    break;
                case (uint)SDL.EventType.TextInput:
                    this.OnTextInput(platformEvent.NativeEvent);
                    platformEvent.Handled = true;
                    break;
            }
        }

        #endregion

        #region TextInput: Helpers

        protected virtual void RebuildCaretRect(SDL.Rect viewport, TextProperties props)
        {
            if (this.LineDataCache == null || this.LineDataCache.Count < 1)
                return;

            (int line, int column) =
                Photon.GetLineAndColumnFromControlLine(
                    this.LineDataCache,
                    this.Text,
                    this.CaretCache.TextIndex);

            TextControlLineData lineInfo = this.LineDataCache[line];

            if (column < 0) column = 0;
            if (column > (lineInfo.EndIndex - lineInfo.StartIndex))
                column = lineInfo.EndIndex - lineInfo.StartIndex;

            this.CaretDrawColor ??= this.CaretColor;

            int caretTextIndex = lineInfo.StartIndex + column;

            Size textSize = default;

            if (caretTextIndex > lineInfo.StartIndex && caretTextIndex <= this.Text.Length)
                textSize = this.FontMetrics.MeasureString(this.Font, this.Text[lineInfo.StartIndex..caretTextIndex]);

            this.CaretRect = new SDL.FRect
            {
                X = viewport.X + textSize.Width,
                Y = viewport.Y + ((line - this.ViewportCache.StartLine) * this.FontMetrics.SkipLine),
                W = this.CaretWidth,
                H = lineInfo.PixelHeight - 2
            };
        }
        protected virtual void DrawCaret(Window window, TextCaretProperties props)
        {
            if (!this.IsCaretVisible ||
                this.CaretCache.Line < this.ViewportCache.StartLine ||
                this.CaretCache.Line >= this.ViewportCache.StartLine + this.ViewportCache.VisibleLines)
                return;

            IntPtr backTexture = this.Window?.BackTexture ?? IntPtr.Zero;

            BorderColors outlineColors = props.CaretOutlineColor.WithOpacity(this.Opacity);

            Photon.DrawBorder(window, this.CaretRect, props.CaretOutline, outlineColors, backTexture);

            SDL.Color caretColor = new()
            {
                R = this.CaretColor.R,
                G = this.CaretColor.G,
                B = this.CaretColor.B,
                A = (byte)(this.CaretColor.A * this.Opacity)
            };

            Photon.DrawRectangle(window, this.CaretRect.Deflate(props.CaretOutline), caretColor, backTexture);
        }

        #endregion

        #region TextInput: Input Handlers

        protected override void HandleArrowDown()
        {
            base.HandleArrowDown();

            this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true, caretDirty: true);
        }
        protected override void HandleArrowLeft()
        {
            base.HandleArrowLeft();

            this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true, caretDirty: true);
        }
        protected override void HandleArrowRight()
        {
            base.HandleArrowRight();
        }
        protected override void HandleArrowUp()
        {
            base.HandleArrowUp();

            this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true, caretDirty: true);
        }

        protected virtual void HandleCut()
        {
            if (this.HasSelection)
            {
                int start = Math.Min(this.CaretCache.TextIndex, this.SelectionIndex);
                int end = Math.Max(this.CaretCache.TextIndex, this.SelectionIndex);

                string selected = this.Text[start..end];

                SDL.SetClipboardText(selected);

                this.PushUndoBatch();

                this.Text = this.Text.Remove(start, end - start);
                this.CaretCache = this.CaretCache with { TextIndex = start };
                this.SelectionIndex = -1;
            }
        }
        protected virtual void HandlePaste()
        {
            string clip = SDL.GetClipboardText();

            if (!string.IsNullOrEmpty(clip))
            {
                this.PushUndoBatch();

                if (this.HasSelection)
                {
                    int start = Math.Min(this.CaretCache.TextIndex, this.SelectionIndex);
                    int end = Math.Max(this.CaretCache.TextIndex, this.SelectionIndex);

                    this.Text = this.Text.Remove(start, end - start);
                    this.CaretCache = this.CaretCache with { TextIndex = start };
                    this.SelectionIndex = -1;
                }

                this.Text = this.Text.Insert(this.CaretCache.TextIndex, clip);
                this.CaretCache = this.CaretCache with { TextIndex = clip.Length };
            }
        }

        protected virtual void HandleBackspace()
        {
            if (this.HasSelection)
            {
                this.PushUndoBatch();

                int start = Math.Min(this.CaretCache.TextIndex, this.SelectionIndex);
                int end = Math.Max(this.CaretCache.TextIndex, this.SelectionIndex);

                this.Text = this.Text.Remove(start, end - start);
                this.CaretCache = this.CaretCache with { TextIndex = start };
                this.SelectionIndex = -1;
            }
            else if (this.CaretCache.TextIndex > 0)
            {
                this.PushUndoBatch();

                this.Text = this.Text.Remove(this.CaretCache.TextIndex - 1, 1);
                this.CaretCache = this.CaretCache with { TextIndex = this.CaretCache.TextIndex - 1 };
            }
        }
        protected virtual void HandleDelete()
        {
            if (this.HasSelection)
            {
                this.PushUndoBatch();

                int start = Math.Min(this.CaretCache.TextIndex, this.SelectionIndex);
                int end = Math.Max(this.CaretCache.TextIndex, this.SelectionIndex);

                this.Text = this.Text.Remove(start, end - start);
                this.CaretCache = this.CaretCache with { TextIndex = start };
                this.SelectionIndex = -1;
            }
            else if (this.CaretCache.TextIndex < this.Text.Length)
            {
                this.PushUndoBatch();

                this.Text = this.Text.Remove(this.CaretCache.TextIndex, 1);
            }
        }

        protected virtual void HandleEnter()
        {
            this.UndoBatchOpen = false;

            this.SubmitAction?.Invoke(this.Text);
            this.OnSubmit?.Execute(this.Text);
        }
        protected virtual void HandleUndoRedoZ(SDL.KeyboardEvent key)
        {
            this.SelectionIndex = -1;
            this.IsSelecting = false;
            this.UndoBatchOpen = false;

            bool ctrl = (key.Mod & SDL.Keymod.Ctrl) != 0;
            bool shift = (key.Mod & SDL.Keymod.Shift) != 0;

            if (!ctrl) return;

            if (shift)
                this.Redo();
            else
                this.Undo();
        }
        protected virtual void HandleRedoKey(SDL.KeyboardEvent key)
        {
            this.SelectionIndex = -1;
            this.IsSelecting = false;
            this.UndoBatchOpen = false;

            if ((key.Mod & SDL.Keymod.Ctrl) != 0)
                this.Redo();
        }

        protected virtual void OnTextInput(SDL.Event e)
        {
            string? text = Marshal.PtrToStringUTF8(e.Text.Text);

            if (string.IsNullOrEmpty(text)) return;

            this.PushUndoBatch();

            if (this.HasSelection)
            {
                int start = Math.Min(this.CaretCache.TextIndex, this.SelectionIndex);
                int end = Math.Max(this.CaretCache.TextIndex, this.SelectionIndex);

                this.Text = this.Text.Remove(start, end - start);
                this.CaretCache = this.CaretCache with { TextIndex = start };
                this.SelectionIndex = -1;
            }

            this.Text = this.Text.Insert(this.CaretCache.TextIndex, text);

            int newIndex = this.CaretCache.TextIndex + text.Length;

            this.BuildLineDataCache(this.Text, this.Font, this.TextWrapType, this.ScrollbarViewport.W);

            int lineIdx = this.LineDataCache.FindIndex(l => newIndex >= l.StartIndex && newIndex <= l.EndIndex);
            int column = newIndex - this.LineDataCache[lineIdx].StartIndex;

            this.CaretCache = this.CaretCache with
            {
                TextIndex = newIndex,
                Line = lineIdx,
                Column = column
            };

            this.ApplyKeyDrivenViewportScroll();

            this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true, caretDirty: true);
        }
        protected override void OnKeyDown(SDL.KeyboardEvent key)
        {
            bool ctrl = (key.Mod & SDL.Keymod.Ctrl) != 0;

            base.OnKeyDown(key);

            switch (key.Key)
            {
                case SDL.Keycode.X: if (ctrl) this.HandleCut(); break;
                case SDL.Keycode.V: if (ctrl) this.HandlePaste(); break;
                case SDL.Keycode.Z: this.HandleUndoRedoZ(key); break;
                case SDL.Keycode.Y: this.HandleRedoKey(key); break;
                case SDL.Keycode.Delete: this.HandleDelete(); break;
                case SDL.Keycode.Backspace: this.HandleBackspace(); break;
                case SDL.Keycode.Return: this.HandleEnter(); break;
            }
        }

        #endregion

        #region TextInput: Undo Handlers

        public virtual void Undo()
        {
            if (this.UndoStack.Count > 0)
            {
                this.RedoStack.Push((this.Text, this.CaretCache.TextIndex));

                (string text, int caret) = this.UndoStack.Pop();

                this.Text = text;
                this.CaretCache = this.CaretCache with { TextIndex = caret };
            }
        }
        public virtual void Redo()
        {
            if (this.RedoStack.Count > 0)
            {
                this.UndoStack.Push((this.Text, this.CaretCache.TextIndex));

                (string text, int caret) = this.RedoStack.Pop();

                this.Text = text;
                this.CaretCache = this.CaretCache with { TextIndex = caret };
            }
        }

        protected virtual void CheckUndoBatchTimer()
        {
            if (this.UndoBatchOpen && (DateTime.Now - this.LastInputTime).TotalMilliseconds > this.UndoBatchTimeoutMs)
                this.UndoBatchOpen = false;
        }
        protected virtual void PushUndoBatch()
        {
            if (!this.UndoBatchOpen)
            {
                this.UndoStack.Push((this.Text, this.CaretCache.TextIndex));
                this.RedoStack.Clear();
                this.UndoBatchOpen = true;
            }

            this.LastInputTime = DateTime.Now;
        }

        #endregion
    }
}