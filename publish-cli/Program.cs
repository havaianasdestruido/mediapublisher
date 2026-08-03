using PublishStudio.Core;

namespace PublishStudio.Cli;

/// <summary>
/// publish-cli: drives the WLXMediaPublishSubscribe.dll PublishManager C API.
/// Real calls report real HRESULTs; engine stubs report "engine stub: E_NOTIMPL".
/// </summary>
static class Program
{
    private const string Dll = Probes.Dll;

    static int Main(string[] args)
    {
        try
        {
            string dir = DllResolver.Apply(FindBindir(args));
            Console.WriteLine($"DLL dir: {dir}");
            Console.WriteLine($"DLL: {Dll} (32-bit)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }

        string cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
        switch (cmd)
        {
            case "targets": return CmdTargets();
            case "auth": return CmdAuth(Arg(args, 1));
            case "signout": return CmdSignOut(Arg(args, 1));
            case "publish": return CmdPublish(args);
            case "status": return CmdStatus();
            case "check": return CmdCheck();
            case "help":
            case "-h":
            case "--help":
                PrintHelp();
                return 0;
            default:
                Console.Error.WriteLine($"unknown command: {cmd}");
                PrintHelp();
                return 2;
        }
    }

    private static string? FindBindir(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--bindir" && i + 1 < args.Length)
                return args[i + 1];
        return null;
    }

    private static string? Arg(string[] args, int i) =>
        args.Length > i ? args[i] : null;

    // --- targets ---

    private static int CmdTargets()
    {
        using var pm = PublishManager.Create();
        Console.WriteLine("Publish targets:");
        Console.WriteLine("  {0,-3} {1,-12} {2,-12} {3}", "ID", "Name", "Available", "Default");

        var def = pm.GetDefaultTarget();
        var ids = pm.EnumerateTargets();
        foreach (int id in ids.Value)
        {
            var name = PublishManager.GetTargetName(id);
            var svc = PublishManager.GetServiceStatus(id);
            Console.WriteLine("  {0,-3} {1,-12} {2,-12} {3}",
                id, name.Value, svc.Value ? "TRUE" : "FALSE",
                id == def.Value ? "yes" : "");
        }

        Console.WriteLine();
        Console.WriteLine(StubAware.Annotate("EnumerateTargets", ids.Hr));
        Console.WriteLine(StubAware.Annotate("GetDefaultTarget", def.Hr));
        return ids.Succeeded ? 0 : 1;
    }

    // --- auth ---

    private static int CmdAuth(string? targetArg)
    {
        if (!TryParseTarget(targetArg, out int target, out string? err))
            return Fail(err);

        using var pm = PublishManager.Create();
        Console.WriteLine($"Authenticating to {TargetName(target)} (target {target})...");

        var auth = pm.Authenticate(target);
        Console.WriteLine(StubAware.Annotate("Authenticate", auth.Hr));

        var isAuth = pm.IsAuthenticated(target);
        Console.WriteLine(StubAware.Annotate("IsAuthenticated", isAuth.Hr) +
            $" (authenticated={isAuth.Value})");

        var acct = pm.GetAccountInfo(target);
        Console.WriteLine(StubAware.Annotate("GetAccountInfo", acct.Hr) +
            $" (display='{acct.Value}')");

        var refresh = pm.RefreshToken(target);
        Console.WriteLine(StubAware.Annotate("RefreshToken", refresh.Hr));

        int rc = auth.Succeeded ? 0 : 1;
        Console.WriteLine();
        Console.WriteLine(rc == 0 ? "auth: OK (engine session auth only; real credential flow not implemented in DLL)" : "auth: FAILED");
        return rc;
    }

    // --- signout ---

    private static int CmdSignOut(string? targetArg)
    {
        if (!TryParseTarget(targetArg, out int target, out string? err))
            return Fail(err);

        using var pm = PublishManager.Create();
        var so = pm.SignOut(target);
        Console.WriteLine(StubAware.Annotate("SignOut", so.Hr));
        var isAuth = pm.IsAuthenticated(target);
        Console.WriteLine(StubAware.Annotate("IsAuthenticated", isAuth.Hr) +
            $" (authenticated={isAuth.Value})");
        Console.WriteLine(so.Succeeded ? "signout: OK" : "signout: FAILED");
        return so.Succeeded ? 0 : 1;
    }

    // --- publish ---

    private static int CmdPublish(string[] args)
    {
        if (args.Length < 3)
            return Fail("usage: publish-cli publish <file> <target>");

        string file = args[1];
        if (!File.Exists(file))
            return Fail($"file does not exist: {file}");
        if (!TryParseTarget(args[2], out int target, out string? err))
            return Fail(err);

        using var pm = PublishManager.Create();
        Console.WriteLine($"Publishing '{Path.GetFileName(file)}' to {TargetName(target)}...");

        var auth = pm.Authenticate(target);
        Console.WriteLine(StubAware.Annotate("Authenticate", auth.Hr));

        var cfg = PublishConfig.DefaultFor(target);
        cfg.Title = Path.GetFileName(file);
        var start = pm.StartPublish(target, file, cfg);
        Console.WriteLine(StubAware.Annotate("StartPublish", start.Hr) +
            (start.Value != IntPtr.Zero ? $" (session 0x{start.Value.ToInt64():X})" : " (no session returned)"));

        if (start.Value != IntPtr.Zero)
        {
            var st = pm.QueryStatus(start.Value);
            Console.WriteLine(StubAware.Annotate("GetStatus", st.Hr) +
                (st.Hr == Hr.E_NOTIMPL ? "" : $" status={st.Status}%={st.Percent}"));

            var r = pm.GetResult(start.Value);
            Console.WriteLine(StubAware.Annotate("GetResult", r.Hr));

            var cancel = pm.Cancel(start.Value);
            Console.WriteLine(StubAware.Annotate("Cancel", cancel.Hr));
        }

        Console.WriteLine();
        Console.WriteLine("publish: session created via real API; upload/status/result are engine stubs (E_NOTIMPL)");
        Console.WriteLine("hint: real per-plugin Upload() returns E_NOTIMPL in this DLL build -- no bytes are transmitted");
        return 0;
    }

    // --- status ---

    private static int CmdStatus()
    {
        using var pm = PublishManager.Create();
        var def = pm.GetDefaultTarget();
        Console.WriteLine(StubAware.Annotate("GetDefaultTarget", def.Hr) +
            $" (default={TargetName(def.Value)})");

        Console.WriteLine();
        Console.WriteLine("Per-target auth + availability:");
        var ids = pm.EnumerateTargets();
        foreach (int id in ids.Value)
        {
            var name = PublishManager.GetTargetName(id);
            var svc = PublishManager.GetServiceStatus(id);
            var auth = pm.IsAuthenticated(id);
            Console.WriteLine("  {0,-10} available={1,-5} authenticated={2}",
                name.Value, svc.Value ? "TRUE" : "FALSE", auth.Value ? "TRUE" : "FALSE");
        }

        Console.WriteLine();
        Console.WriteLine("Session-status surface (engine stubs in this build):");
        var st = pm.QueryStatus(IntPtr.Zero);
        Console.WriteLine("  " + StubAware.Annotate("GetStatus(NULL)", st.Hr));
        var r = pm.GetResult(IntPtr.Zero);
        Console.WriteLine("  " + StubAware.Annotate("GetResult(NULL)", r.Hr));
        var subSt = pm.QuerySubscribeStatus(IntPtr.Zero);
        Console.WriteLine("  " + StubAware.Annotate("GetSubscribeStatus(NULL)", subSt.Hr));
        return 0;
    }

    // --- check (smoke) ---

    private static int CmdCheck()
    {
        Console.WriteLine($"Smoke check -- {Probes.Dll} ({Probes.Run().Count} probes)");
        Console.WriteLine();
        var results = Probes.Run();
        int pass = 0, fail = 0;
        foreach (var c in results)
        {
            bool ok = c.Pass;
            if (ok) pass++; else fail++;
            Console.WriteLine($"  {(ok ? "PASS" : "FAIL")}  [{c.Kind,-6}] {c.Name,-45} {c.Detail}");
        }

        Console.WriteLine();
        Console.WriteLine($"real/stub map:");
        foreach (var line in StubAware.TableLines())
            Console.WriteLine(line);

        Console.WriteLine();
        Console.WriteLine($"check: {pass} passed, {fail} failed");
        return fail == 0 ? 0 : 1;
    }

    // --- helpers ---

    private static string TargetName(int target) =>
        PublishManager.GetTargetName(target).Value;

    private static bool TryParseTarget(string? arg, out int target, out string? err)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            target = -1;
            err = "missing target (use target name or id 0-4, default 0)";
            return false;
        }
        if (int.TryParse(arg, out target) && target is >= 0 and <= 4)
        {
            err = null;
            return true;
        }
        string? name = Enum.TryParse<PublishTarget>(arg, true, out var t)
            ? ((int)t).ToString()
            : null;
        if (name is not null && int.TryParse(name, out target) && target is >= 0 and <= 4)
        {
            err = null;
            return true;
        }
        target = -1;
        err = $"unknown target '{arg}' (use 0-4 or Facebook/Flickr/YouTube/Vimeo/SkyDrive)";
        return false;
    }

    private static int Fail(string msg)
    {
        Console.Error.WriteLine($"ERROR: {msg}");
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Publish Studio -- publish-cli  (WLXMediaPublishSubscribe.dll driver)

            Usage: publish-cli <command> [args] [--bindir <dll-dir>]

            Commands:
              targets                     List publish targets + availability
              auth [target]               Authenticate to a target (default 0)
              signout [target]            Sign out of a target (default 0)
              publish <file> <target>     Attempt to publish a video file
              status                      Report default target, auth, availability
              check                       Smoke test: real calls + honest stub probes
              help                        Show this help

            Target: 0-4 or Facebook/Flickr/YouTube/Vimeo/SkyDrive

            DLL dir resolution: --bindir arg > WMMR_BIN_DIR env >
            C:\Users\mcmco\Desktop\WMMR\build_clean\bin\Debug

            Honesty note: several PublishManager calls are engine stubs that
            return E_NOTIMPL (GetStatus/Cancel/GetResult/callbacks/subscribe/
            RefreshToken). They are labeled "[engine stub]" and reported
            verbatim instead of being faked.
            """);
    }
}
