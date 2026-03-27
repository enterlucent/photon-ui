using PhotonUI.Controls;
using PhotonUI.Events;
using PhotonUI.Events.Platform;
using PhotonUI.Models;
using SDL3;

namespace PhotonUI.Services.Recognizers.Gestures
{
    public class PressGesture : IGestureRecognizer
    {
        public bool NeedsUpdates => true;

        public record PressedCandidate(Control Source, InputDeviceKey DeviceID, SDL.FPoint Position, ulong DownTimestamp);
        public record PressedSource(Control Source, ulong Timestamp, int Count);
        public record PressedPending(PressedCandidate Candidate, SDL.Event Event);

        protected Dictionary<InputDeviceKey, PressedCandidate> Candidates = [];
        protected Dictionary<InputDeviceKey, PressedSource> LastPress = [];
        protected Dictionary<InputDeviceKey, PressedPending> PendingPresses = [];

        protected const ulong ClickThreshold = 350;

        public void Process(Window window, SDL.Event e)
        {
            switch (e.Type)
            {
                case (uint)SDL.EventType.MouseButtonDown:
                case (uint)SDL.EventType.PenButtonDown:
                case (uint)SDL.EventType.FingerDown:
                    this.HandleDown(window, e);
                    break;

                case (uint)SDL.EventType.MouseButtonUp:
                case (uint)SDL.EventType.PenButtonUp:
                case (uint)SDL.EventType.FingerUp:
                    this.HandleUp(window, e);
                    break;
            }
        }
        public void Update(Window window)
        {
            if (this.PendingPresses.Count > 0)
                this.FlushPendingPresses(window, SDL.GetTicks());
        }
        public void Cancel(Window window, PlatformEventArgs e)
        {
            this.FlushPendingPresses(window, SDL.GetTicks());
        }

        private void HandleDown(Window window, SDL.Event e)
        {
            InputDeviceKey deviceKey;
            Control? target;

            float x;
            float y;

            switch (e.Type)
            {
                default:
                case (uint)SDL.EventType.MouseButtonDown:
                    x = e.Button.X;
                    y = e.Button.Y;

                    deviceKey = new(InputDeviceType.Mouse, e.Button.Button);
                    target = Photon.ResolveHitControl(window, x, y);
                    break;
                case (uint)SDL.EventType.PenButtonDown:
                    x = e.PButton.X;
                    y = e.PButton.Y;
                    deviceKey = new(InputDeviceType.Pen, e.PButton.Button);
                    target = Photon.ResolveHitControl(window, x, y);
                    break;
                case (uint)SDL.EventType.FingerDown:
                    deviceKey = new(InputDeviceType.Touch, e.TFinger.FingerID);
                    x = (e.TFinger.X * window.DrawRect.W);
                    y = (e.TFinger.Y * window.DrawRect.H);
                    target = Photon.ResolveHitControl(window, x, y);
                    break;
            }

            if (target != null)
                this.Candidates[deviceKey] =
                    new PressedCandidate(target, deviceKey, new SDL.FPoint() { X = x, Y = y }, SDL.GetTicks());
        }
        private void HandleUp(Window window, SDL.Event e)
        {
            InputDeviceKey deviceKey;
            Control? upTarget;

            switch (e.Type)
            {
                default:
                case (uint)SDL.EventType.MouseButtonUp:
                    deviceKey = new(InputDeviceType.Mouse, e.Button.Button);
                    upTarget = Photon.ResolveHitControl(window, e.Button.X, e.Button.Y);
                    break;
                case (uint)SDL.EventType.PenButtonUp:
                    deviceKey = new(InputDeviceType.Pen, e.PButton.Button);
                    upTarget = Photon.ResolveHitControl(window, e.PButton.X, e.PButton.Y);
                    break;
                case (uint)SDL.EventType.FingerUp:
                    deviceKey = new(InputDeviceType.Touch, e.TFinger.FingerID);
                    float x = (e.TFinger.X * window.DrawRect.W);
                    float y = (e.TFinger.Y * window.DrawRect.H);
                    upTarget = Photon.ResolveHitControl(window, x, y);
                    break;
            }

            if (!this.Candidates.TryGetValue(deviceKey, out PressedCandidate? candidate))
                return;

            ulong now = SDL.GetTicks();
            int clickCount = 1;

            if (this.LastPress.TryGetValue(deviceKey, out var last))
                if (last.Source == candidate.Source && now - last.Timestamp <= ClickThreshold)
                    clickCount = last.Count + 1;

            this.LastPress[deviceKey] = new PressedSource(candidate.Source, now, clickCount);
            this.PendingPresses[deviceKey] = new PressedPending(candidate, e);
            this.Candidates.Remove(deviceKey);
        }

        private void FlushPendingPresses(Window window, ulong now)
        {
            List<InputDeviceKey> toDispatch = [];

            foreach (var kvp in this.PendingPresses)
            {
                var candidate = kvp.Value.Candidate;
                if (now - candidate.DownTimestamp > ClickThreshold)
                    toDispatch.Add(kvp.Key);
            }

            foreach (var key in toDispatch)
            {
                if (this.PendingPresses.TryGetValue(key, out var pending))
                {
                    this.PendingPresses.Remove(key);

                    int clickCount = 1;

                    if (this.LastPress.TryGetValue(key, out PressedSource? last))
                        clickCount = last.Count;

                    PointerPressEventArgs args = new(window, pending.Candidate.Source, clickCount, pending.Candidate.Position, pending.Event);

                    window.DispatchToControl(args, pending.Candidate.Source);

                    this.LastPress[key] = new PressedSource(pending.Candidate.Source, last.Timestamp, 0);
                }
            }
        }
    }
}