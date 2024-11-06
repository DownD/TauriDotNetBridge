using System.Runtime.InteropServices;
using System.Text;

namespace TauriDotNetBridge;

public static class Bridge
{
	private static bool myIsDebug;

	public static void SetDebug(bool isDebug)
	{
		myIsDebug = isDebug;
	}

	[UnmanagedCallersOnly]
	public static unsafe byte* ProcessRequest(IntPtr requestPtr, int requestLength)
	{
		var request = Marshal.PtrToStringUTF8(requestPtr, requestLength);
		if (request == null || request.Length == 0)
		{
			request = null;
		}

		var response = Router.Instance(myIsDebug).RouteRequest(request);

		var responseBytes = Encoding.UTF8.GetBytes(response);
		var responsePtr = Marshal.AllocHGlobal(responseBytes.Length + 1);

		Marshal.Copy(responseBytes, 0, responsePtr, responseBytes.Length);
		Marshal.WriteByte(responsePtr, responseBytes.Length, 0);

		return (byte*)responsePtr;
	}
}
