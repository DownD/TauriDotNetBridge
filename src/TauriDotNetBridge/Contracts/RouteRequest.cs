namespace TauriDotNetBridge.Contracts;

internal class RouteRequest
{
    public string Controller { get; set; } = "Home";
    public string Action { get; set; } = "Index";
    public object? Data { get; set; }
}
