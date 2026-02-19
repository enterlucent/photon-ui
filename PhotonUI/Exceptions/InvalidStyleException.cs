namespace PhotonUI.Exceptions
{
    public class InvalidStyleException : Exception
    {
        public Type? StyleType { get; }

        public InvalidStyleException() { }
        public InvalidStyleException(string message)
            : base(message) { }
        public InvalidStyleException(string message, Exception inner)
            : base(message, inner) { }

        public InvalidStyleException(string message, Type styleType)
            : base(message) => this.StyleType = styleType;

        public InvalidStyleException(string message, Type styleType, Exception inner)
            : base(message, inner) => this.StyleType = styleType;
    }
}