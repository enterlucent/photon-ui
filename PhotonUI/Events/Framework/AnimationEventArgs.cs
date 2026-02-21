using PhotonUI.Animations;

namespace PhotonUI.Events.Framework
{
    public class AnimationEventArgs(AnimationHandle handle, AnimationState state) : FrameworkEventArgs
    {
        public AnimationHandle Handle { get; } = handle;
        public AnimationState State { get; } = state;
    }
}