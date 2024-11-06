using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TauriDotNetBridge.Contracts;

namespace TauriDotNetBridge;

internal class PluginLoader
{
    public static string DotNetHome = Path.GetDirectoryName(typeof(PluginLoader).Assembly.Location)!;

    public void Load(ServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<PluginLoader>>();

        AppDomain.CurrentDomain.AssemblyResolve += AssemblyDependency.AssemblyResolve;

        var assemblies = Directory.GetFiles(DotNetHome, "*.TauriPlugIn.dll");

        foreach (var dllPath in assemblies)
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.Load(LoadFile(dllPath));
                var plugInName = assembly.GetName().Name;

                logger.LogDebug($"Loading '{Path.GetFileNameWithoutExtension(dllPath)}' ... ");

                foreach (var type in assembly.GetTypes().Where(x => typeof(IPlugIn).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract))
                {
                    var instance = (IPlugIn)Activator.CreateInstance(type)!;

                    logger.LogDebug($"  Initializing '{type}' ... ");

                    instance.Initialize(services);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load {Path.GetFileName(dllPath)}: {ex}");
            }
        }

        AppDomain.CurrentDomain.AssemblyResolve -= AssemblyDependency.AssemblyResolve;
    }

    private static byte[] LoadFile(string filename)
    {
        using var fs = new FileStream(filename, FileMode.Open);

        byte[] buffer = new byte[(int)fs.Length];
        fs.Read(buffer, 0, buffer.Length);
        fs.Close();

        return buffer;
    }
}