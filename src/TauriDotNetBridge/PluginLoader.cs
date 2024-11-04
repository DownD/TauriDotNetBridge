using Microsoft.Extensions.DependencyInjection;
using TauriDotNetBridge.Contracts;

namespace TauriDotNetBridge;

internal class PluginLoader
{
    public static string DotNetHome = Path.GetDirectoryName(typeof(PluginLoader).Assembly.Location);

    public void Load(ServiceCollection services)
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyDependency.AssemblyResolve;

        if (!Directory.Exists(DotNetHome))
        {
            Console.WriteLine($"DotNet home '{DotNetHome}' doesn't exist");
            return;
        }

        var assemblies = Directory.GetFiles(DotNetHome, "*.TauriPlugIn.dll");

        foreach (var dllPath in assemblies)
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.Load(LoadFile(dllPath));
                var plugInName = assembly.GetName().Name;

                Console.WriteLine($"Loading '{Path.GetFileNameWithoutExtension(dllPath)}' ... ");

                foreach (var type in assembly.GetTypes().Where(x => typeof(IPlugIn).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract))
                {
                    var instance = (IPlugIn)Activator.CreateInstance(type)!;

                    Console.WriteLine($"  Initializing '{type}' ... ");

                    instance.Initialize(services);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {Path.GetFileName(dllPath)}: {ex}");
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