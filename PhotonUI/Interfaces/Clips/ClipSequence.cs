using PhotonUI.Models.Clips;

namespace PhotonUI.Interfaces.Clips
{
    public class ClipSequence
    {
        private readonly List<ClipFrame> frames = [];

        public int FrameCount => this.frames.Count;

        public void AddFrame(ClipFrame frame)
            => this.frames.Add(frame);

        public ClipFrame GetFrame(int frameIndex)
        {
            if (this.frames.Count == 0)
                throw new InvalidOperationException("No frames in clip.");

            int index = frameIndex % this.frames.Count;

            return this.frames[index];
        }
    }
}