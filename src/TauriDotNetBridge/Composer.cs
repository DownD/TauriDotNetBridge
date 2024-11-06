using Microsoft.Extensions.DependencyInjection;
using TauriDotNetBridge.Contracts;

namespace TauriDotNetBridge;

internal class Composer
{
    public static string DotNetHome = Path.GetDirectoryName(typeof(Composer).Assembly.Location)!;

    public Composer(bool isDebug)
    {
        Services = new ServiceCollection();

        Services.AddSingleton<ILogger>(new ConsoleLogger(isDebug));
    }

    public IServiceCollection Services { get; private set; }
    public IServiceProvider? ServiceProvider { get; private set; }

    public void Compose()
    {
        ServiceProvider = Services.BuildServiceProvider();
        var logger = ServiceProvider.GetRequiredService<ILogger>();

        AppDomain.CurrentDomain.AssemblyResolve += AssemblyDependency.AssemblyResolve;

        var assemblies = Directory.GetFiles(DotNetHome, "*.TauriPlugIn.dll");

        foreach (var dllPath in assemblies)
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.Load(LoadFile(dllPath));
                var plugInName = assembly.GetName().Name;

                logger.Debug($"Loading '{Path.GetFileNameWithoutExtension(dllPath)}' ... ");

                foreach (var type in assembly.GetTypes().Where(x => typeof(IPlugIn).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract))
                {
                    var instance = (IPlugIn)Activator.CreateInstance(type)!;

                    logger.Debug($"  Initializing '{type}' ... ");

                    instance.Initialize(Services);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load {Path.GetFileName(dllPath)}: {ex}");
            }
        }

        AppDomain.CurrentDomain.AssemblyResolve -= AssemblyDependency.AssemblyResolve;

        ServiceProvider = Services.BuildServiceProvider();
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
