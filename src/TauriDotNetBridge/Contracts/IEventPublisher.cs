namespace TauriDotNetBridge.Contracts;

public interface IEventPublisher
{
    void Publish(string name, object payload);
}
