namespace PublishStudio.Core;

/// <summary>
/// Classifies each PublishManager export as real / engine-stub / partial,
/// based on the WLXMediaPublishSubscribe.cpp implementation and the proven
/// mmr-gui suite results. Lets the CLI/GUI label results honestly instead of
/// pretending stubbed calls do real work.
/// </summary>
public enum ApiKind
{
    Real,
    Stub,
    Partial,
}

public static class StubAware
{
    public static readonly IReadOnlyDictionary<string, ApiKind> ApiTable =
        new Dictionary<string, ApiKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["DllCanUnloadNow"] = ApiKind.Real,
            ["DllGetClassObject"] = ApiKind.Real,
            ["DllRegisterServer"] = ApiKind.Real,
            ["DllUnregisterServer"] = ApiKind.Real,

            ["Create"] = ApiKind.Real,
            ["Destroy"] = ApiKind.Real,
            ["EnumerateTargets"] = ApiKind.Real,
            ["GetTargetName"] = ApiKind.Real,
            ["Authenticate"] = ApiKind.Real,
            ["IsAuthenticated"] = ApiKind.Real,
            ["SignOut"] = ApiKind.Real,
            ["GetAccountInfo"] = ApiKind.Real,
            ["GetServiceStatus"] = ApiKind.Real,
            ["Cleanup"] = ApiKind.Real,
            ["GetDefaultTarget"] = ApiKind.Real,
            ["SetDefaultTarget"] = ApiKind.Real,

            ["StartPublish"] = ApiKind.Partial,
            ["StartSubscribe"] = ApiKind.Stub,

            ["GetStatus"] = ApiKind.Stub,
            ["Cancel"] = ApiKind.Stub,
            ["GetResult"] = ApiKind.Stub,
            ["SetProgressCallback"] = ApiKind.Stub,
            ["SetCompleteCallback"] = ApiKind.Stub,
            ["GetSubscribeStatus"] = ApiKind.Stub,
            ["RefreshToken"] = ApiKind.Stub,
        };

    public static string Badge(string api) =>
        ApiTable.TryGetValue(api, out var kind) ? kind switch
        {
            ApiKind.Real => "[real]",
            ApiKind.Stub => "[engine stub]",
            ApiKind.Partial => "[partial]",
            _ => "[?]",
        } : "[?]";

    /// <summary>Single-line honest result label, e.g. "[engine stub] GetStatus -> engine stub: E_NOTIMPL".</summary>
    public static string Annotate(string api, int hr)
    {
        string verdict = hr == Hr.E_NOTIMPL
            ? "engine stub: E_NOTIMPL"
            : Hr.Fmt(hr);
        return $"{Badge(api)} {api} -> {verdict}";
    }

    /// <summary>Prints the full real-vs-stub table (used by CLI 'check' and the GUI legend).</summary>
    public static IEnumerable<string> TableLines() =>
        ApiTable.OrderBy(kv => kv.Key).Select(kv =>
            $"  {kv.Key,-24} {Badge(kv.Key)}");
}
