using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TauriDotNetBridge.Contracts;

namespace TauriDotNetBridge;

internal class Router
{
    private static readonly JsonSerializerSettings myResponseSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private static readonly JsonSerializerSettings myRequestSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new DefaultNamingStrategy()
        }
    };

    private static Router? myInstance;
    private static readonly object myLock = new();

    private readonly ActionInvoker myActionInvoker;

    private Router(bool isDebug)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
            {
                builder.AddConsole();
                if (isDebug)
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    builder.SetMinimumLevel(LogLevel.Warning);
                }
            });

        var loader = new PluginLoader();
        loader.Load(services);

        myActionInvoker = new ActionInvoker(services, myRequestSettings);
    }

    public static Router Instance(bool isDebug)
    {
        if (myInstance != null)
        {
            lock (myLock)
            {
                myInstance ??= new Router(isDebug);
            }
        }
        return myInstance!;
    }

    public string RouteRequest(string? requestText)
    {
        if (requestText is null or "")
        {
            return Serialize(RouteResponse.Error("Request string is empty"));
        }

        RouteRequest? request;
        try
        {
            request = JsonConvert.DeserializeObject<RouteRequest>(requestText, myRequestSettings);
            if (request == null)
            {
                return Serialize(RouteResponse.Error("Failed to parse request JSON"));
            }
        }
        catch (Exception)
        {
            return Serialize(RouteResponse.Error("Failed to parse request JSON"));
        }

        var response = myActionInvoker.InvokeAction(request.Controller, request.Action, request.Data);
        return Serialize(response);
    }

    private static string Serialize(RouteResponse response) =>
        JsonConvert.SerializeObject(response, myResponseSettings);
}
