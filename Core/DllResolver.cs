using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PublishStudio.Core;

/// <summary>
/// Resolves the 32-bit DLL directory before any native call.
/// Priority: explicit --bindir arg > WMMR_BIN_DIR env > build_clean default.
/// Uses SetDllDirectoryW (proven in tests\mmr-gui\TestModels.cs).
/// </summary>
public static class DllResolver
{
    public const string DefaultBinDir = @"C:\Users\mcmco\Desktop\WMMR\build_clean\bin\Debug";

    public static string? Current { get; private set; }

    public static string Apply(string? explicitDir)
    {
        string dir = Resolve(explicitDir);
        if (!Native.SetDllDirectoryW(dir))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"SetDllDirectoryW failed for '{dir}'");
        Current = dir;
        return dir;
    }

    public static string Resolve(string? explicitDir)
    {
        string? dir = explicitDir
            ?? Environment.GetEnvironmentVariable("WMMR_BIN_DIR");
        if (string.IsNullOrWhiteSpace(dir))
            dir = DefaultBinDir;
        string full = Path.GetFullPath(dir);
        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException($"DLL directory does not exist: {full}");
        return full;
    }
}
