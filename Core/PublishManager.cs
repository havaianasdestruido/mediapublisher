using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PublishStudio.Core;

internal static class Hr
{
    public const int S_OK = 0;
    public const int E_NOTIMPL = unchecked((int)0x80004001);
    public const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
    public static bool Is(int actual, int expected) => unchecked((uint)actual) == unchecked((uint)expected);
    public static string Hex(int hr) => $"0x{unchecked((uint)hr) & 0xFFFFFFFF:X8}";
    public static string Name(int hr) => hr switch
    {
        S_OK => "S_OK",
        E_NOTIMPL => "E_NOTIMPL",
        CLASS_E_CLASSNOTAVAILABLE => "CLASS_E_CLASSNOTAVAILABLE",
        _ => Hex(hr),
    };
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct PublishConfig
{
    public int Target;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)] public string Title;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)] public string Description;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)] public string Tags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Category;
    public int Private;
    public int AllowEmbed;
    public uint PrivacyLevel;
}

internal static class Native
{
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr PublishManager_Create();
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern void PublishManager_Destroy(IntPtr hManager);
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_EnumerateTargets(IntPtr hManager, [Out] int[] targets, ref uint count);
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    internal static extern int PublishManager_GetTargetName(int target, StringBuilder name, uint cchName);
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_Authenticate(IntPtr hManager, int target, IntPtr hParentWnd);
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_IsAuthenticated(IntPtr hManager, int target, out int pAuth);
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    internal static extern IntPtr PublishManager_StartPublish(IntPtr hManager, int target, [MarshalAs(UnmanagedType.LPWStr)] string? pszFilePath, ref PublishConfig config);
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_GetStatus(IntPtr hPublish, out uint pStatus, out uint pPercent);
    [DllImport("WLXMediaPublishSubscribe.dll", CallingConvention = CallingConvention.StdCall)]
    internal static extern int PublishManager_SignOut(IntPtr hManager, int target);
}

public sealed class PublishSession : IDisposable
{
    private readonly IntPtr _handle;
    public PublishSession()
    {
        _handle = Native.PublishManager_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create PublishManager");
    }
    public int[] EnumerateTargets()
    {
        uint count = 16; var arr = new int[count];
        var hr = Native.PublishManager_EnumerateTargets(_handle, arr, ref count);
        if (Hr.Is(hr, Hr.E_NOTIMPL) || Hr.Is(hr, Hr.CLASS_E_CLASSNOTAVAILABLE)) { Console.WriteLine($"engine stub: {Hr.Hex(hr)}"); return Array.Empty<int>(); }
        if (!Hr.Is(hr, Hr.S_OK)) throw new InvalidOperationException($"Enumerate failed: {Hr.Name(hr)}");
        Array.Resize(ref arr, (int)count); return arr;
    }
    public string GetTargetName(int id)
    {
        var sb = new StringBuilder(256);
        var hr = Native.PublishManager_GetTargetName(id, sb, (uint)sb.Capacity);
        if (Hr.Is(hr, Hr.E_NOTIMPL) || Hr.Is(hr, Hr.CLASS_E_CLASSNOTAVAILABLE)) { Console.WriteLine($"engine stub: {Hr.Hex(hr)}"); return $"Target{id}"; }
        if (!Hr.Is(hr, Hr.S_OK)) throw new InvalidOperationException($"GetName failed: {Hr.Name(hr)}");
        return sb.ToString();
    }
    public int Authenticate(int target)
    {
        var hr = Native.PublishManager_Authenticate(_handle, target, IntPtr.Zero);
        if (Hr.Is(hr, Hr.E_NOTIMPL) || Hr.Is(hr, Hr.CLASS_E_CLASSNOTAVAILABLE)) { Console.WriteLine($"engine stub: {Hr.Hex(hr)}"); }
        return hr;
    }
    public bool IsAuthenticated(int target)
    {
        var hr = Native.PublishManager_IsAuthenticated(_handle, target, out int auth);
        if (Hr.Is(hr, Hr.E_NOTIMPL) || Hr.Is(hr, Hr.CLASS_E_CLASSNOTAVAILABLE)) { Console.WriteLine($"engine stub: {Hr.Hex(hr)}"); return false; }
        return auth != 0;
    }
    public IntPtr StartPublish(int target, string path)
    {
        var cfg = new PublishConfig { Target = target };
        var pub = Native.PublishManager_StartPublish(_handle, target, path, ref cfg);
        if (pub == IntPtr.Zero)
        {
            int hr = Marshal.GetLastWin32Error();
            if (Hr.Is(hr, Hr.E_NOTIMPL) || Hr.Is(hr, Hr.CLASS_E_CLASSNOTAVAILABLE)) Console.WriteLine($"engine stub: {Hr.Hex(hr)}");
            else Console.WriteLine($"StartPublish error: {Hr.Hex(hr)}");
        }
        return pub;
    }
    public (uint status, uint percent) GetStatus(IntPtr publishHandle)
    {
        var hr = Native.PublishManager_GetStatus(publishHandle, out uint status, out uint percent);
        if (Hr.Is(hr, Hr.E_NOTIMPL) || Hr.Is(hr, Hr.CLASS_E_CLASSNOTAVAILABLE)) { Console.WriteLine($"engine stub: {Hr.Hex(hr)}"); return (0,0); }
        if (!Hr.Is(hr, Hr.S_OK)) throw new InvalidOperationException($"GetStatus failed: {Hr.Name(hr)}");
        return (status, percent);
    }
    public void SignOut(int target)
    {
        var hr = Native.PublishManager_SignOut(_handle, target);
        if (Hr.Is(hr, Hr.E_NOTIMPL) || Hr.Is(hr, Hr.CLASS_E_CLASSNOTAVAILABLE)) Console.WriteLine($"engine stub: {Hr.Hex(hr)}");
    }
    public void Dispose() => Native.PublishManager_Destroy(_handle);
}
