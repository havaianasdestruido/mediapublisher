using System.Runtime.InteropServices;

namespace PublishStudio.Core;

/// <summary>Publish target service identifiers (WLXMediaPublishSubscribe.h PublishTarget).</summary>
public enum PublishTarget : int
{
    Facebook = 0,
    Flickr = 1,
    YouTube = 2,
    Vimeo = 3,
    SkyDrive = 4,
}

/// <summary>Publish session status values (WLXMediaPublishSubscribe.h PublishStatus).</summary>
public enum PublishStatus : uint
{
    Idle = 0,
    Authenticating = 1,
    Uploading = 2,
    Processing = 3,
    Complete = 4,
    Error = 5,
    Cancelled = 6,
}

/// <summary>Native layout of MediaPublish::PublishConfig (WCHAR arrays + BOOLs).</summary>
[StructLayout(LayoutKind.Sequential)]
public struct PublishConfig
{
    public int Target;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
    public string Title;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
    public string Description;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string Tags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Category;

    public int Private;
    public int AllowEmbed;
    public uint PrivacyLevel;

    public static PublishConfig DefaultFor(int target) => new()
    {
        Target = target,
        Title = string.Empty,
        Description = string.Empty,
        Tags = string.Empty,
        Category = string.Empty,
        Private = 0,
        AllowEmbed = 1,
        PrivacyLevel = 0,
    };
}

/// <summary>Native layout of MediaPublish::PublishResult (GetResult output).</summary>
[StructLayout(LayoutKind.Sequential)]
public struct PublishResult
{
    public int HrResult;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
    public string PublishUrl;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string Error;

    public ulong ItemId;
}

/// <summary>Typed HRESULT + value from one native call.</summary>
public readonly record struct ApiResult<T>(int Hr, T Value)
{
    public bool Succeeded => Hr >= 0;
    public string HrText => HrRes.Name(Hr);
    public string HrFull => HrRes.Fmt(Hr);
}
