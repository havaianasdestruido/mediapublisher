using System.Runtime.InteropServices;
using System.Text;

namespace PublishStudio.Core;

/// <summary>
/// P/Invoke layer for WLXMediaPublishSubscribe.dll (32-bit).
/// Signatures proven in tests\mmr-gui\Native.cs; decorated stdcall names
/// (_PublishManager_*@N) are resolved automatically from the plain names.
/// </summary>
internal static class Native
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool SetDllDirectoryW([MarshalAs(UnmanagedType.LPWStr)] string lpPathName);

    // --- COM quartet (real; DllCanUnloadNow returns S_OK) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int DllCanUnloadNow();

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int DllGetClassObject(ref Guid rclsid, ref Guid riid, out IntPtr ppv);

    // --- Manager lifecycle (real) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr PublishManager_Create();

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern void PublishManager_Destroy(IntPtr hManager);

    // --- Target enumeration (real) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_EnumerateTargets(IntPtr hManager, [Out] int[] targets, ref uint count);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    internal static extern int PublishManager_GetTargetName(int target, StringBuilder name, uint cchName);

    // --- Authentication (real) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_Authenticate(IntPtr hManager, int target, IntPtr hParentWnd);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_IsAuthenticated(IntPtr hManager, int target, out int pAuth);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_SignOut(IntPtr hManager, int target);

    // --- Publishing (StartPublish real/partial; session ops are engine stubs) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    internal static extern IntPtr PublishManager_StartPublish(IntPtr hManager, int target, [MarshalAs(UnmanagedType.LPWStr)] string? pszFilePath, ref PublishConfig config);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_GetStatus(IntPtr hPublish, out uint pStatus, out uint pPercent);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_Cancel(IntPtr hPublish);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_GetResult(IntPtr hPublish, IntPtr pResult);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_SetProgressCallback(IntPtr hPublish, IntPtr pfnProgress, IntPtr pUserData);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_SetCompleteCallback(IntPtr hPublish, IntPtr pfnComplete, IntPtr pUserData);

    // --- Subscription (engine stubs) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    internal static extern IntPtr PublishManager_StartSubscribe(IntPtr hManager, int target, [MarshalAs(UnmanagedType.LPWStr)] string? pszItemId);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_GetSubscribeStatus(IntPtr hSubscribe, out uint pStatus, out uint pPercent);

    // --- Account (GetAccountInfo real; RefreshToken engine stub) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    internal static extern int PublishManager_GetAccountInfo(IntPtr hManager, int target, StringBuilder displayName, uint cchDisplayName);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_RefreshToken(IntPtr hManager, int target);

    // --- Settings (real-ish: SetDefaultTarget always S_OK, GetDefaultTarget returns Facebook) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_SetDefaultTarget(IntPtr hManager, int target);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_GetDefaultTarget(IntPtr hManager, out int pTarget);

    // --- Utility (real) ---
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_GetServiceStatus(int target, out int pAvailable);

    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_Cleanup();
}
