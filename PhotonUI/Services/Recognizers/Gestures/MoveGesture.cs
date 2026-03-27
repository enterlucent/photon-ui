using PhotonUI.Controls;
using PhotonUI.Events;
using PhotonUI.Events.Platform;
using PhotonUI.Models;
using SDL3;
using System.Numerics;

namespace PhotonUI.Services.Recognizers.Gestures
{
    public record MoveCandidate(Control Source, InputDeviceKey DeviceID, SDL.FPoint Start, SDL.FPoint Last, Control? CurrentOver, bool Started);


    public class MoveGesture : IGestureRecognizer
    {
        public bool NeedsUpdates => false;

        protected MoveCandidate? ActiveMove = null;

        protected const float MoveStartThreshold = 4f;

        public void Process(Window window, SDL.Event e)
        {
            InputDeviceKey deviceKey;

            float x;
            float y;

            switch (e.Type)
            {
                case (uint)SDL.EventType.MouseButtonDown:
                    deviceKey = new(InputDeviceType.Mouse, e.Button.Button);
                    x = e.Button.X;
                    y = e.Button.Y;

                    // Only track the first button/finger
                    if (this.ActiveMove == null)
                        this.HandleDown(window, deviceKey, x, y, e);
                    break;
                case (uint)SDL.EventType.PenButtonDown:
                    deviceKey = new(InputDeviceType.Pen, e.PButton.Button);
                    x = e.PButton.X;
                    y = e.PButton.Y;

                    if (this.ActiveMove == null)
                        this.HandleDown(window, deviceKey, x, y, e);
                    break;
                case (uint)SDL.EventType.FingerDown:
                    deviceKey = new(InputDeviceType.Touch, e.TFinger.FingerID);
                    x = (int)(e.TFinger.X * window.DrawRect.W);
                    y = (int)(e.TFinger.Y * window.DrawRect.H);

                    if (this.ActiveMove == null)
                        this.HandleDown(window, deviceKey, x, y, e);
                    break;

                case (uint)SDL.EventType.MouseMotion:
                    deviceKey = new(InputDeviceType.Mouse, e.Button.Button);
                    x = e.Motion.X;
                    y = e.Motion.Y;

                    if (this.ActiveMove != null && this.ActiveMove.DeviceID == deviceKey)
                        this.HandleMove(window, deviceKey, x, y, e);
                    break;
                case (uint)SDL.EventType.PenMotion:
                    deviceKey = new(InputDeviceType.Pen, e.PButton.Button);
                    x = e.PMotion.X;
                    y = e.PMotion.Y;

                    if (this.ActiveMove != null && this.ActiveMove.DeviceID == deviceKey)
                        this.HandleMove(window, deviceKey, x, y, e);
                    break;
                case (uint)SDL.EventType.FingerMotion:
                    deviceKey = new(InputDeviceType.Touch, e.TFinger.FingerID);
                    x = (e.TFinger.X * window.DrawRect.W);
                    y = (e.TFinger.Y * window.DrawRect.H);

                    if (this.ActiveMove != null && this.ActiveMove.DeviceID == deviceKey)
                        this.HandleMove(window, deviceKey, x, y, e);
                    break;

                case (uint)SDL.EventType.MouseButtonUp:
                case (uint)SDL.EventType.PenButtonUp:
                case (uint)SDL.EventType.FingerUp:
                    this.RemoveCandidate(window, e);
                    break;
            }
        }
        public void Update(Window window) { }
        public void Cancel(Window window, PlatformEventArgs e)
            => this.RemoveCandidate(window, e.NativeEvent);

        private void HandleDown(Window window, InputDeviceKey deviceKey, float x, float y, SDL.Event e)
        {
            // Only start tracking if no active pointer
            if (this.ActiveMove != null) return;

            Control? target = Photon.ResolveHitControl(window, x, y);
            if (target == null) return;

            SDL.FPoint position = new() { X = x, Y = y };

            // Initial enter event
            PointerEnterEventArgs enterArgs = new(window, deviceKey, target, target, position, e);
            window.DispatchToControl(enterArgs, target);

            this.ActiveMove = new MoveCandidate(target, deviceKey, position, position, target, false);
        }
        private void HandleMove(Window window, InputDeviceKey deviceKey, float x, float y, SDL.Event e)
        {
            if (this.ActiveMove == null || this.ActiveMove.DeviceID != deviceKey) return;

            SDL.FPoint position = new() { X = x, Y = y };
            Vector2 delta = new(position.X - this.ActiveMove.Last.X, position.Y - this.ActiveMove.Last.Y);

            Control? newOver = Photon.ResolveHitControl(window, x, y);

            // Enter/Exit events
            if (this.ActiveMove.CurrentOver != newOver)
            {
                if (this.ActiveMove.CurrentOver != null)
                {
                    PointerExitEventArgs exitArgs =
                        new(window, deviceKey, this.ActiveMove.Source, this.ActiveMove.CurrentOver, position, e);
                    window.DispatchToControl(exitArgs, this.ActiveMove.CurrentOver);
                }

                if (newOver != null)
                {
                    PointerEnterEventArgs enterArgs =
                        new(window, deviceKey, this.ActiveMove.Source, newOver, position, e);
                    window.DispatchToControl(enterArgs, newOver);
                }
            }

            // Start move if threshold exceeded
            if (!this.ActiveMove.Started)
            {
                Vector2 fromStart = new(position.X - this.ActiveMove.Start.X, position.Y - this.ActiveMove.Start.Y);
                if (fromStart.LengthSquared() > MoveStartThreshold * MoveStartThreshold)
                {
                    PointerMoveStartEventArgs startArgs =
                        new(window, deviceKey, this.ActiveMove.Source, newOver!, Vector2.Zero, position, e);
                    window.DispatchToControl(startArgs, this.ActiveMove.Source);

                    this.ActiveMove = this.ActiveMove with { Started = true };
                }
            }
            else
            {
                // Dispatch move
                if (this.ActiveMove.Source != null && newOver != null)
                {
                    PointerMoveEventArgs moveArgs =
                        new(window, deviceKey, this.ActiveMove.Source, newOver, delta, position, e);
                    window.DispatchToControl(moveArgs, this.ActiveMove.Source);
                }
            }

            // Update candidate
            this.ActiveMove = this.ActiveMove with { Last = position, CurrentOver = newOver };
        }
        private void RemoveCandidate(Window window, SDL.Event e)
        {
            if (this.ActiveMove == null) return;

            Control? current = this.ActiveMove.CurrentOver;
            SDL.FPoint position = this.ActiveMove.Last;
            Vector2 delta = this.ActiveMove.Started
                ? new Vector2(position.X - this.ActiveMove.Last.X, position.Y - this.ActiveMove.Last.Y)
                : Vector2.Zero;

            if (this.ActiveMove.Source != null)
            {
                // MoveEnd only if move session started
                if (this.ActiveMove.Started)
                {
                    PointerMoveEndEventArgs endArgs =
                        new(window, this.ActiveMove.DeviceID, this.ActiveMove.Source, current!, delta, position, e);
                    window.DispatchToControl(endArgs, this.ActiveMove.Source);
                }

                // Always dispatch exit if still over a control
                if (current != null)
                {
                    PointerExitEventArgs exitArgs =
                        new(window, this.ActiveMove.DeviceID, this.ActiveMove.Source, current, position, e);
                    window.DispatchToControl(exitArgs, current);
                }
            }

            this.ActiveMove = null;
        }
    }
}