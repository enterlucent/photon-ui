namespace PhotonUI.Animations.AnimationNodes
{
    public partial class GroupAnimation(params AnimationBase[] animations) : AnimationBase
    {
        private readonly List<AnimationBase> animations = [.. animations];

        public override void Start() { foreach (AnimationBase a in this.animations) a.Start(); }
        public override void Update() { foreach (AnimationBase a in this.animations) a.Update(); }
        public override bool IsComplete => this.animations.All(a => a.IsComplete);
    }
}