using PhotonUI.Interfaces;

namespace PhotonUI.Models.Properties
{
    public enum StretchMode
    {
        None,
        Fill,
        Uniform,
        UniformToFill
    }

    public enum StretchDirection
    {
        UpOnly,
        DownOnly,
        Both
    }

    public interface IStretchProperties : IStyleProperties
    {
        StretchMode StretchMode { get; }
        StretchDirection StretchDirection { get; }
    }

    public readonly record struct StretchProperties(
        StretchMode StretchMode,
        StretchDirection StretchDirection) : IStretchProperties
    {
        public static StretchProperties Default => new(
            StretchMode.None,
            StretchDirection.Both
        );
    }
}