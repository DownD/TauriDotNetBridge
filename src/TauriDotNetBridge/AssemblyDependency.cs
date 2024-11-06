using System.Reflection;

namespace TauriDotNetBridge;

internal class AssemblyDependency
{
	public static Assembly? AssemblyResolve(object sender, ResolveEventArgs args)
	{
		if (args.Name.StartsWith("System")) return null;

		var name = new AssemblyName(args.Name).Name + ".dll";

		try
		{
			return AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.FullName == args.Name).First();
		}
		catch (Exception)
		{
		}

		string dependencyPath = Path.Combine(PluginLoader.DotNetHome, name);
		return Assembly.LoadFile(dependencyPath);
	}
}
