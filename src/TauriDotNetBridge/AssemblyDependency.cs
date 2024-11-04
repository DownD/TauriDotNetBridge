using System.Reflection;

namespace TauriDotNetBridge;

public class AssemblyDependency
{
	public static Assembly? AssemblyResolve(object sender, ResolveEventArgs args)
	{
		if (args.Name.StartsWith("System")) return null;

		var name = new AssemblyName(args.Name).Name + ".dll";

		try
		{
			var compatible = AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.FullName == args.Name).First();
			//Console.WriteLine($"Requested loaded ASM, returning {compatible.GetName()}");
			return compatible;
		}
		catch (Exception)
		{
		}

		string dependencyPath = Path.Combine(PluginLoader.DotNetHome, name);
		return Assembly.LoadFile(dependencyPath);
	}
}
