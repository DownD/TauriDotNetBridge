using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TauriDotNetBridge.Contracts;

namespace TauriDotNetBridge;

internal class ActionInvoker
{
    private readonly IServiceCollection myServices;
    private readonly IServiceProvider myServiceProvider;
    private readonly JsonSerializer mySerializer;
    private readonly ILogger<ActionInvoker> myLogger;

    public ActionInvoker(IServiceCollection services, JsonSerializerSettings settings)
    {
        myServices = services;

        myServiceProvider = services.BuildServiceProvider();
        myLogger = myServiceProvider.GetRequiredService<ILogger<ActionInvoker>>();
        mySerializer = JsonSerializer.Create(settings);
    }

    public RouteResponse InvokeAction(string controller, string action, object? data)
    {
        var type = myServices.FirstOrDefault(x =>
            (x.ImplementationType?.Name.Equals(controller, StringComparison.OrdinalIgnoreCase) == true ||
             x.ImplementationType?.Name.Equals(controller + "Controller", StringComparison.OrdinalIgnoreCase) == true) &&
            x.ImplementationType?.IsClass == true &&
            x.ImplementationType?.IsAbstract == false)
            ?.ImplementationType;

        if (type == null)
        {
            myLogger.LogWarning($"No controller found for: '{controller}'");
            return RouteResponse.Error($"No controller found for: '{controller}'");
        }

        var method = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(x => x.Name.Equals(action, StringComparison.OrdinalIgnoreCase)
                                 && x.GetParameters().Length <= 1);

        if (method == null)
        {
            myLogger.LogWarning($"No action found for '{action}' in controller '{controller}'");
            return RouteResponse.Error($"No action found for '{action}' in controller '{controller}'");
        }

        var instance = myServiceProvider.GetService(type);
        if (instance == null)
        {
            myLogger.LogError($"Failed to resolve a controller instance for '{type}.{method}'");
            return RouteResponse.Error($"Failed to resolve a controller instance for '{type}.{method}'");
        }

        try
        {
            if (data is null)
            {
                return RouteResponse.Ok(method.Invoke(instance, null));
            }
            else
            {
                var arg = ((JObject)data).ToObject(method.GetParameters().Single().ParameterType, mySerializer);
                return RouteResponse.Ok(method.Invoke(instance, [arg]));
            }
        }
        catch (Exception ex)
        {
            return RouteResponse.Error(ex);
        }
    }
}
