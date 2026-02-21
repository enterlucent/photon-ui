namespace PhotonUI.Services.Interpolators
{
    public sealed class FloatInterpolator : IInterpolator<float>
    {
        public float Lerp(float start, float end, float progress)
            => start + (end - start) * progress;
    }
}