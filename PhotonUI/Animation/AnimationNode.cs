using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Animations.AnimationNodes;
using PhotonUI.Controls;

namespace PhotonUI.Animations
{
    public abstract partial class AnimationBase : ObservableObject
    {
        public abstract void Start();
        public abstract void Update();
        public abstract bool IsComplete { get; }

        public AnimationBase Then(AnimationBase next)
            => new SequenceAnimation(this, next);
        public AnimationBase Group(AnimationBase other)
            => new GroupAnimation(this, other);
        public AnimationBase Wait(TimeSpan delay)
            => new SequenceAnimation(this, new WaitAnimation(delay));
        public AnimationBase OnComplete(Action callback)
            => new SequenceAnimation(this, new CallbackAnimation(callback));

        public AnimationHandle Enqueue(Window window)
        {
            AnimationHandle handle = window.AnimationEnqueue(this);

            if (handle.State == AnimationState.Ready)
                handle.Start();

            return handle;
        }
    }
}