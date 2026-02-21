namespace PhotonUI.Animations.AnimationNodes
{
    public partial class CallbackAnimation(Action callback) : AnimationBase
    {
        private readonly Action callback = callback;
        private bool fired;

        public override void Start()
        {
            if (!this.fired)
            {
                this.fired = true;
                this.callback();
            }
        }

        public override void Update() { }
        public override bool IsComplete => this.fired;
    }
}