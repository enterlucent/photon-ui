using PhotonUI.Interfaces.Clips;
using PhotonUI.Models.Clips;
using PhotonUI.Services;
using SDL3;

namespace PhotonUI.Components
{
    public class ClipPlayer(ClipSequence clip)
    {
        protected readonly ClipSequence Clip = clip
            ?? throw new ArgumentNullException(nameof(clip));

        protected long StartTicks = -1;
        protected int FrameIndex = 0;

        public virtual float PlaybackSpeed { get; set; } = 1.0f;
        public virtual bool Looping { get; set; } = true;
        public virtual PlaybackDirection Direction { get; set; } = PlaybackDirection.Forward;
        public virtual bool IsPlaying { get; protected set; } = false;
        public virtual int CurrentFrameIndex => this.FrameIndex;

        public virtual int TotalDuration
        {
            get
            {
                int total = 0;

                for (int i = 0; i < this.Clip.FrameCount; i++)
                    total += this.Clip.GetFrame(i).IntervalMs;

                return total;
            }
        }
        public virtual ulong ElapsedTime
        {
            get
            {
                if (this.StartTicks == -1) return 0;

                return (ulong)((SDL.GetTicks() - (ulong)this.StartTicks) * this.PlaybackSpeed);
            }
        }

        public virtual void Play()
        {
            this.IsPlaying = true;
            if (this.StartTicks == -1)
                this.StartTicks = (long)SDL.GetTicks();
        }
        public virtual void Seek(int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= this.Clip.FrameCount)
                throw new ArgumentOutOfRangeException(nameof(frameIndex));

            this.FrameIndex = frameIndex;
            this.StartTicks = (long)SDL.GetTicks();
        }
        public virtual void SeekToTime(TimeSpan time)
        {
            ulong targetMs = (ulong)(time.TotalMilliseconds * this.PlaybackSpeed);

            int accumulated = 0;
            for (int i = 0; i < this.Clip.FrameCount; i++)
            {
                accumulated += this.Clip.GetFrame(i).IntervalMs;
                if (targetMs < (ulong)accumulated)
                {
                    this.FrameIndex = i;
                    break;
                }
            }

            this.StartTicks = (long)SDL.GetTicks() - (long)targetMs;
        }
        public virtual void Stop() => this.IsPlaying = false;
        public virtual void Reset()
        {
            this.StartTicks = -1;
        }

        public virtual ClipFrame CurrentFrame
        {
            get
            {
                if (this.Clip.FrameCount == 0)
                    throw new InvalidOperationException("No frames in clip.");

                if (!this.IsPlaying)
                {
                    this.FrameIndex = 0;
                    return this.Clip.GetFrame(0);
                }

                if (this.StartTicks == -1)
                    this.StartTicks = (long)SDL.GetTicks();

                ulong elapsed = this.ElapsedTime;

                int frameIndex = 0;
                int accumulated = 0;

                for (int i = 0; i < this.Clip.FrameCount; i++)
                {
                    accumulated += this.Clip.GetFrame(i).IntervalMs;
                    if (elapsed < (ulong)accumulated)
                    {
                        frameIndex = i;
                        break;
                    }
                }

                if (elapsed >= (ulong)this.TotalDuration)
                {
                    if (this.Looping)
                    {
                        ulong wrapped = elapsed % (ulong)this.TotalDuration;
                        accumulated = 0;
                        for (int i = 0; i < this.Clip.FrameCount; i++)
                        {
                            accumulated += this.Clip.GetFrame(i).IntervalMs;
                            if (wrapped < (ulong)accumulated)
                            {
                                frameIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        frameIndex = this.Clip.FrameCount - 1;
                    }
                }

                if (this.Direction == PlaybackDirection.Reverse)
                    frameIndex = this.Clip.FrameCount - 1 - frameIndex;

                this.FrameIndex = frameIndex;
                return this.Clip.GetFrame(frameIndex);
            }
        }
    }
}