using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TauriDotNetBridge.Contracts;

namespace TauriDotNetBridge;

internal class Composer
{
    private static readonly string DotNetHome = Path.GetDirectoryName(typeof(Composer).Assembly.Location)!;

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

        var assemblies = Directory.GetFiles(DotNetHome, "*.TauriPlugIn.dll");

        // we need the AssemblyResolve event handler registered forever because
        // dependencies are loaded by CLR on-demand and we need to resolve them
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        foreach (var path in assemblies)
        {
            Load(logger, path);
        }

        ServiceProvider = Services.BuildServiceProvider();

        Assembly? OnAssemblyResolve(object? _, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("System")) return null;

            logger.Debug($"    Resolving assembly: '{args.Name}'");

            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.FullName == args.Name);
            if (asm != null)
            {
                return asm;
            }

            var name = new AssemblyName(args.Name).Name + ".dll";
            var dependencyPath = Path.Combine(DotNetHome, name);
            logger.Debug($"    Loading: '{dependencyPath}'");
            return Assembly.LoadFile(dependencyPath);
        }
    }

    private void Load(ILogger logger, string dllPath)
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

    private static byte[] LoadFile(string filename)
    {
        using var fs = new FileStream(filename, FileMode.Open);

        byte[] buffer = new byte[(int)fs.Length];
        fs.Read(buffer, 0, buffer.Length);
        fs.Close();

        return buffer;
    }
}
