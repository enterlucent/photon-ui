namespace PhotonUI.Animations.AnimationNodes
{
    public partial class WaitAnimation(TimeSpan duration) : AnimationBase
    {
        private readonly TimeSpan duration = duration;
        private DateTime start;

        public override void Start() => this.start = DateTime.UtcNow;
        public override void Update() { }
        public override bool IsComplete => (DateTime.UtcNow - this.start) >= this.duration;
    }
}