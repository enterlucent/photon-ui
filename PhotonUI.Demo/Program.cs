partial class Program
{
    static void Main(string[] args)
    {
        MainArgsHandler(args);      
    }

    #region PhotonUI.Demo: Argumentation

    private static void MainArgsHandler(string[] args)
    {
        foreach (string arg in args)
            if (string.IsNullOrEmpty(arg)) { }
    }

    #endregion
}