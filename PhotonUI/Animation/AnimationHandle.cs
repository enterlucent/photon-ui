using PhotonUI.Events.Framework;

namespace PhotonUI.Animations
{
    public enum AnimationState
    {
        Ready,
        Running,
        Paused,
        Stopped,
        Completed,
        Canceled,
        Invalid
    }

    public sealed class AnimationHandle(AnimationBase inner)
    {
        public event EventHandler<AnimationEventArgs>? StateChanged;

        private readonly AnimationBase inner = inner;
        private AnimationState state = AnimationState.Ready;
        private bool isValid = true;

        public AnimationState State => this.state;
        public bool IsValid => this.isValid;
        public bool IsComplete
        {
            get
            {
                this.EnsureValid();

                return this.state == AnimationState.Completed;
            }
        }

        public void Invalidate()
            => this.isValid = false;
        public void EnsureValid()
        {
            if (!this.isValid)
                throw new InvalidOperationException("Animation handle is no longer valid.");
        }

        public void Start()
        {
            this.EnsureValid();

            if (this.state == AnimationState.Ready || this.state == AnimationState.Paused)
            {
                this.inner.Start();
                this.state = AnimationState.Running;

                this.RaiseStateChanged();
            }
        }
        public void Update()
        {
            this.EnsureValid();

            if (this.state == AnimationState.Running)
            {
                this.inner.Update();

                if (this.inner.IsComplete)
                {
                    this.state = AnimationState.Completed;

                    this.RaiseStateChanged();
                }
            }
        }

        public void Pause()
        {
            this.EnsureValid();

            if (this.state == AnimationState.Running)
            {
                this.state = AnimationState.Paused;

                this.RaiseStateChanged();
            }
        }
        public void Resume()
        {
            this.EnsureValid();

            if (this.state == AnimationState.Paused)
            {
                this.state = AnimationState.Running;

                this.RaiseStateChanged();
            }
        }
        public void Stop()
        {
            this.EnsureValid();

            if (this.state == AnimationState.Running || this.state == AnimationState.Paused)
            {
                this.state = AnimationState.Stopped;

                this.RaiseStateChanged();
            }
        }

        public void Cancel()
        {
            this.EnsureValid();
            this.state = AnimationState.Canceled;
            this.Invalidate();
            this.RaiseStateChanged();
        }

        private void RaiseStateChanged()
            => StateChanged?.Invoke(this, new AnimationEventArgs(this, this.state));
    }
}