namespace PhotonUI.Services.Interpolators
{
    public sealed class IntInterpolator : IInterpolator<int>
    {
        public int Lerp(int start, int end, float progress)
            => (int)(start + (end - start) * progress);
    }
}