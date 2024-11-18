namespace TauriDotNetBridge.Contracts;

public interface IHostedService
{
    Task StartAsync(CancellationToken cancellationToken);
}