using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Controls;
using PhotonUI.Interfaces.Services;
using PhotonUI.Services;

namespace PhotonUI.Animations
{
    public partial class PropertyAnimation<TTarget, TProp>(IInterpolatorService lerpService) : AnimationBase
        where TTarget : Control
    {
        private readonly IInterpolatorService lerpService = lerpService;
        private readonly IInterpolator<TProp> interpolator = lerpService.Get<TProp>();

        private TProp? start;
        private TProp? end;
        private TimeSpan duration;
        private Func<float, float> easing = t => t;
        private DateTime startTime;

        private bool loop;
        private Func<TProp, TProp>? nextTargetFactory;
        private TimeSpan epsilon = TimeSpan.FromMilliseconds(1);

        [ObservableProperty]
        private TProp? value;

        public override bool IsComplete => !this.loop && (DateTime.UtcNow - this.startTime) >= this.duration;

        public PropertyAnimation<TTarget, TProp> From(TProp start) { this.start = start; return this; }
        public PropertyAnimation<TTarget, TProp> To(TProp end) { this.end = end; return this; }
        public PropertyAnimation<TTarget, TProp> Over(TimeSpan duration) { this.duration = duration; return this; }
        public PropertyAnimation<TTarget, TProp> WithEasing(Func<float, float> easing) { this.easing = easing; return this; }
        public PropertyAnimation<TTarget, TProp> Loop(Func<TProp, TProp> nextFactory) { this.loop = true; this.nextTargetFactory = nextFactory; return this; }
        public PropertyAnimation<TTarget, TProp> WithLoopOverlap(TimeSpan overlap) { this.epsilon = overlap; return this; }

        public override void Start() => this.startTime = DateTime.UtcNow;
        public override void Update()
        {
            if (this.duration == TimeSpan.Zero || this.start is null || this.end is null) return;

            float elapsedMs = (float)(DateTime.UtcNow - this.startTime).TotalMilliseconds;
            float durationMs = (float)this.duration.TotalMilliseconds;

            if (this.loop && elapsedMs >= durationMs - (float)this.epsilon.TotalMilliseconds)
            {
                this.Value = this.end;
                TProp current = this.end!;
                TProp? next = this.nextTargetFactory!(current);

                this.start = current;
                this.end = next;
                this.startTime = DateTime.UtcNow;

                elapsedMs = 0f;
            }

            float progress = Math.Clamp(elapsedMs / durationMs, 0f, 1f);
            float eased = this.easing(progress);

            this.Value = this.interpolator.Lerp(this.start!, this.end!, eased);
        }
    }
}