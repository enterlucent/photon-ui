namespace PhotonUI.Animations.AnimationNodes
{
    public partial class SequenceAnimation(params AnimationBase[] animations) : AnimationBase
    {
        private readonly Queue<AnimationBase> queue = new(animations);
        private AnimationBase? current;

        public override void Start() => this.Advance();
        public override void Update()
        {
            this.current?.Update();
            if (this.current?.IsComplete == true) this.Advance();
        }
        public override bool IsComplete => this.current == null;

        private void Advance()
        {
            this.current = this.queue.Count > 0 ? this.queue.Dequeue() : null;
            this.current?.Start();
        }
    }
}