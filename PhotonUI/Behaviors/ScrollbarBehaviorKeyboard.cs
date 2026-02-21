using PhotonUI.Controls;
using PhotonUI.Models.Properties;
using SDL3;

namespace PhotonUI.Behaviors
{
    public class ScrollbarBehaviorKeyboard<T>(T control)
        : ScrollbarBehavior<T>(control) where T : Control, IScrollHandler, IScrollProperties
    {
        #region ScrollBarBehavior: Input Handlers

        protected virtual void HandleKeyDown(SDL.Event e)
        {
            float newHOffset = this.HorizontalOffset;
            float newVOffset = this.VerticalOffset;

            switch (e.Key.Key)
            {
                case SDL.Keycode.Up:
                    if (this.Control.ScrollDirection == ScrollDirection.Vertical || this.Control.ScrollDirection == ScrollDirection.Both)
                        newVOffset = Math.Clamp(this.VerticalOffset - this.Control.KeyboardScrollStepY, 0f, this.ExtentHeight);
                    break;

                case SDL.Keycode.Down:
                    if (this.Control.ScrollDirection == ScrollDirection.Vertical || this.Control.ScrollDirection == ScrollDirection.Both)
                        newVOffset = Math.Clamp(this.VerticalOffset + this.Control.KeyboardScrollStepY, 0f, this.ExtentHeight);
                    break;

                case SDL.Keycode.Left:
                    if (this.Control.ScrollDirection == ScrollDirection.Horizontal || this.Control.ScrollDirection == ScrollDirection.Both)
                        newHOffset = Math.Clamp(this.HorizontalOffset - this.Control.KeyboardScrollStepX, 0f, this.ExtentWidth);
                    break;

                case SDL.Keycode.Right:
                    if (this.Control.ScrollDirection == ScrollDirection.Horizontal || this.Control.ScrollDirection == ScrollDirection.Both)
                        newHOffset = Math.Clamp(this.HorizontalOffset + this.Control.KeyboardScrollStepX, 0f, this.ExtentWidth);
                    break;
            }

            this.HorizontalOffset = newHOffset;
            this.VerticalOffset = newVOffset;

            this.Control.OnScrollbarUpdated(new(newHOffset, this.ExtentWidth, newVOffset, this.ExtentHeight));
        }

        #endregion

        #region ScrollBarBehavior: Hook

        public override void OnEvent(Window window, SDL.Event e)
        {
            base.OnEvent(window, e);

            switch (e.Type)
            {
                case (uint)SDL.EventType.KeyDown:
                    this.HandleKeyDown(e);
                    break;
            }
        }

        #endregion
    }
}