using System;
using System.IO;
using System.Threading;
using PublishStudio.Core;

namespace PublishStudio;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0) { PrintHelp(); return; }
        var cmd = args[0].ToLowerInvariant();
        using var sess = new PublishSession();
        switch (cmd)
        {
            case "targets":
                var targets = sess.EnumerateTargets();
                foreach (var id in targets) Console.WriteLine($"{id}: {sess.GetTargetName(id)}");
                break;
            case "auth":
                if (args.Length < 2) { Console.WriteLine("usage: auth <target>"); return; }
                int t = int.Parse(args[1]);
                var hr = sess.Authenticate(t);
                Console.WriteLine($"Authenticate returned: {Hr.Name(hr)} ({Hr.Hex(hr)})");
                break;
            case "publish":
                if (args.Length < 3) { Console.WriteLine("usage: publish <file> <target>"); return; }
                string file = args[1]; int target = int.Parse(args[2]);
                var pub = sess.StartPublish(target, file);
                if (pub == IntPtr.Zero) { Console.WriteLine("Publish not started."); break; }
                while (true)
                {
                    var (status, percent) = sess.GetStatus(pub);
                    Console.WriteLine($"Status {status}, {percent}%");
                    if (status != 0) break; // simplistic: non-zero means done/error
                    Thread.Sleep(500);
                }
                break;
            case "signout":
                if (args.Length < 2) { Console.WriteLine("usage: signout <target>"); return; }
                int sid = int.Parse(args[1]);
                sess.SignOut(sid);
                break;
            default:
                PrintHelp();
                break;
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("Commands: targets | auth <id> | publish <file> <id> | signout <id>");
    }
}
