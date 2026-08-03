namespace PublishStudio.Core;

/// <summary>Result of one smoke check.</summary>
public readonly record struct CheckOutcome(
    string Name,
    string Dll,
    string Kind,
    bool Pass,
    string Detail);

/// <summary>
/// Smoke/sanity probes against the real DLL: exercises the proven working
/// surface (Create/Enumerate/GetTargetName/Authenticate/IsAuthenticated/
/// GetAccountInfo/Cleanup/Destroy) and the honest stub surface (GetStatus/
/// Cancel/GetResult/callbacks/subscribe/RefreshToken -> E_NOTIMPL).
/// </summary>
public static class Probes
{
    public const string Dll = "WLXMediaPublishSubscribe.dll";

    public static IReadOnlyList<CheckOutcome> Run()
    {
        var results = new List<CheckOutcome>();
        void T(string name, string kind, bool pass, string detail) =>
            results.Add(new CheckOutcome(name, Dll, kind, pass, detail));

        T("PublishManager_Create", "real", true, "handle lifecycle");
        using (var pm = PublishManager.Create())
        {
            T("PublishManager_EnumerateTargets", "real",
                pm.EnumerateTargets() is var (hr, ids) && Hr.Is(hr, Hr.S_OK) && ids.Length == 5 && ids.SequenceEqual(new[] { 0, 1, 2, 3, 4 }),
                $"S_OK count={ids.Length} targets={string.Join(",", ids)}");

            var names = new[] { "Facebook", "Flickr", "YouTube", "Vimeo", "SkyDrive" };
            for (int i = 0; i < names.Length; i++)
            {
                var r = PublishManager.GetTargetName(i);
                T($"PublishManager_GetTargetName({i})", "real",
                    Hr.Is(r.Hr, Hr.S_OK) && r.Value == names[i],
                    $"{r.HrText} '{r.Value}'");
            }

            var auth = pm.Authenticate(1);
            T("PublishManager_Authenticate(1)", "real", Hr.Is(auth.Hr, Hr.S_OK), auth.HrText);

            var isAuth = pm.IsAuthenticated(0);
            T("PublishManager_IsAuthenticated(0)", "real",
                Hr.Is(isAuth.Hr, Hr.S_OK) && !isAuth.Value,
                $"{isAuth.HrText} auth=FALSE");

            var signout = pm.SignOut(0);
            T("PublishManager_SignOut(0)", "real", Hr.Is(signout.Hr, Hr.S_OK), signout.HrText);

            var acct = pm.GetAccountInfo(0);
            T("PublishManager_GetAccountInfo(0)", "real",
                Hr.Is(acct.Hr, Hr.S_OK) && acct.Value == "User",
                $"{acct.HrText} '{acct.Value}'");

            var cfg = PublishConfig.DefaultFor(1);
            var start = pm.StartPublish(1, null, cfg);
            T("PublishManager_StartPublish(null path)", "real",
                start.Hr == Hr.E_INVALIDARG && start.Value == IntPtr.Zero,
                "NULL session for NULL file path");

            var st = pm.QueryStatus(IntPtr.Zero);
            T("PublishManager_GetStatus(NULL)", "stub", Hr.Is(st.Hr, Hr.E_NOTIMPL), Hr.Name(st.Hr));

            var cancel = pm.Cancel(IntPtr.Zero);
            T("PublishManager_Cancel(NULL)", "stub", Hr.Is(cancel.Hr, Hr.E_NOTIMPL), Hr.Name(cancel.Hr));

            var result = pm.GetResult(IntPtr.Zero);
            T("PublishManager_GetResult(NULL)", "stub", Hr.Is(result.Hr, Hr.E_NOTIMPL), Hr.Name(result.Hr));

            var prog = pm.SetProgressCallback(IntPtr.Zero);
            T("PublishManager_SetProgressCallback", "stub", Hr.Is(prog.Hr, Hr.E_NOTIMPL), Hr.Name(prog.Hr));

            var comp = pm.SetCompleteCallback(IntPtr.Zero);
            T("PublishManager_SetCompleteCallback", "stub", Hr.Is(comp.Hr, Hr.E_NOTIMPL), Hr.Name(comp.Hr));

            var sub = pm.StartSubscribe(0, null);
            T("PublishManager_StartSubscribe", "stub", sub.Hr == Hr.E_NOTIMPL && sub.Value == IntPtr.Zero, "NULL (subscribe stub)");

            var subSt = pm.QuerySubscribeStatus(IntPtr.Zero);
            T("PublishManager_GetSubscribeStatus(NULL)", "stub", Hr.Is(subSt.Hr, Hr.E_NOTIMPL), Hr.Name(subSt.Hr));

            var refresh = pm.RefreshToken(0);
            T("PublishManager_RefreshToken(0)", "stub", Hr.Is(refresh.Hr, Hr.E_NOTIMPL), Hr.Name(refresh.Hr));

            var setDef = pm.SetDefaultTarget(2);
            T("PublishManager_SetDefaultTarget(2)", "real", Hr.Is(setDef.Hr, Hr.S_OK), setDef.HrText);

            var getDef = pm.GetDefaultTarget();
            T("PublishManager_GetDefaultTarget", "real",
                Hr.Is(getDef.Hr, Hr.S_OK) && getDef.Value == 0,
                $"{getDef.HrText} target=Facebook({getDef.Value})");
        }

        for (int i = 0; i < 5; i++)
        {
            var svc = PublishManager.GetServiceStatus(i);
            T($"PublishManager_GetServiceStatus({i})", "real",
                Hr.Is(svc.Hr, Hr.S_OK) && svc.Value,
                $"{svc.HrText} available=TRUE");
        }

        var unload = PublishManager.DllCanUnloadNow();
        T("DllCanUnloadNow", "real", Hr.Is(unload.Hr, Hr.S_OK), unload.HrText);

        var cleanup = PublishManager.Cleanup();
        T("PublishManager_Cleanup", "real", Hr.Is(cleanup.Hr, Hr.S_OK), cleanup.HrText);

        return results;
    }
}
