namespace PhotonUI.Models
{
    public enum InputDeviceType : byte
    {
        Mouse = 0,
        Pen = 1,
        Touch = 2
    }

    public readonly struct InputDeviceKey(InputDeviceType type, ulong id) : IEquatable<InputDeviceKey>
    {
        public InputDeviceType Type { get; } = type;
        public ulong ID { get; } = id;

        public override bool Equals(object? obj) => obj is InputDeviceKey other && this.Equals(other);
        public bool Equals(InputDeviceKey other) => this.Type == other.Type && this.ID == other.ID;
        public override int GetHashCode() => HashCode.Combine(this.Type, this.ID);

        public static bool operator ==(InputDeviceKey left, InputDeviceKey right) => left.Equals(right);
        public static bool operator !=(InputDeviceKey left, InputDeviceKey right) => !left.Equals(right);
    }
}