namespace PublishStudio.Core;

/// <summary>HRESULT constants and formatting helpers.</summary>
public static class Hr
{
    public const int S_OK = 0;
    public const int S_FALSE = 1;
    public const int E_NOTIMPL = unchecked((int)0x80004001);
    public const int E_INVALIDARG = unchecked((int)0x80070057);
    public const int E_ACCESSDENIED = unchecked((int)0x80070005);
    public const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);

    public static bool Is(int actual, int expected) =>
        unchecked((uint)actual) == unchecked((uint)expected);

    public static bool Failed(int hr) => hr < 0;

    public static string Hex(int hr) => $"0x{unchecked((uint)hr) & 0xFFFFFFFF:X8}";

    public static string Name(int hr) => hr switch
    {
        S_OK => "S_OK",
        S_FALSE => "S_FALSE",
        E_NOTIMPL => "E_NOTIMPL",
        E_INVALIDARG => "E_INVALIDARG",
        E_ACCESSDENIED => "E_ACCESSDENIED",
        CLASS_E_CLASSNOTAVAILABLE => "CLASS_E_CLASSNOTAVAILABLE",
        _ => Hex(hr),
    };

    public static string Fmt(int hr) => $"{Name(hr)} ({Hex(hr)})";
}
