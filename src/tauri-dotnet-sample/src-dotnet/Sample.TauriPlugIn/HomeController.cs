namespace Sample.TauriPlugIn;

public class HomeController
{
    public string Greet(string name) =>
        $"Hello, {name}! You've been greeted from C#!";
}
