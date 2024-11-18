using System.Runtime.InteropServices;
using System.Text;

namespace TauriDotNetBridge;

public static class Bridge
{
	private static bool myIsDebug;
	private static Router? myInstance;
	private static readonly object myLock = new();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public unsafe delegate void EmitCallback(byte* eventName, byte* payload);

	private static EmitCallback? myEmitCallback;

	private static Router Instance(bool isDebug)
	{
		if (myInstance == null)
		{
			lock (myLock)
			{
				var composer = new Composer(isDebug);
				composer.LoadPlugIns();

				myInstance ??= new Router(composer);

				composer.StartHostedServices();
			}
		}
		return myInstance!;
	}

	[UnmanagedCallersOnly]
	public static void SetDebug(int isDebug)
	{
		myIsDebug = isDebug != 0;
	}

	[UnmanagedCallersOnly]
	public static unsafe byte* ProcessRequest(IntPtr requestPtr, int requestLength)
	{
		var request = Marshal.PtrToStringUTF8(requestPtr, requestLength);
		if (request == null || request.Length == 0)
		{
			request = null;
		}

		var response = Instance(myIsDebug).RouteRequest(request);

		return TransferOwnershipToRust(response);
	}

	private static unsafe byte* TransferOwnershipToRust(string str)
	{
		var bytes = Encoding.UTF8.GetBytes(str);
		var pointer = Marshal.AllocHGlobal(bytes.Length + 1);

		Marshal.Copy(bytes, 0, pointer, bytes.Length);
		Marshal.WriteByte(pointer, bytes.Length, 0);

		return (byte*)pointer;
	}

	[UnmanagedCallersOnly]
	public static void RegisterEmitCallback(IntPtr callbackPtr)
	{
		// ensure hosted services are started
		Instance(myIsDebug);

		myEmitCallback = Marshal.GetDelegateForFunctionPointer<EmitCallback>(callbackPtr);
	}

	public static unsafe void Emit(string eventName, string payload)
	{
		var eventNamePtr = TransferOwnershipToRust(eventName);
		var payloadPtr = TransferOwnershipToRust(payload);

		myEmitCallback?.Invoke(eventNamePtr, payloadPtr);
	}
}
