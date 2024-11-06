namespace TauriDotNetBridge;

internal interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}

internal class ConsoleLogger(bool isDebug) : ILogger
{
    private bool myIsDebug = isDebug;

    public void Debug(string message)
    {
        if (!myIsDebug) return;

        Console.WriteLine($"DEBUG|{message}");
    }

    public void Info(string message)
    {
        Console.WriteLine($"INFO|{message}");
    }

    public void Warn(string message)
    {
        Console.WriteLine($"WARN|{message}");
    }

    public void Error(string message)
    {
        Console.WriteLine($"ERROR|{message}");
    }
}