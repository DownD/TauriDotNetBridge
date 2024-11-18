namespace Sample.TauriPlugIn;

using TauriDotNetBridge.Contracts;

public class NewsFeed(IEventPublisher publisher) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"News feed started");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            Console.WriteLine($"{DateTime.Now}|Publishing news");
            publisher.Publish("news-feed", $"News from C# at {DateTime.Now}");
        }
    }
}
