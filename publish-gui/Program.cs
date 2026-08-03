using PublishStudio.Core;

namespace PublishStudio.Gui;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string? bindir = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--bindir" && i + 1 < args.Length)
                bindir = args[i + 1];

        try
        {
            DllResolver.Apply(bindir);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"DLL directory resolution failed:\n{ex.Message}",
                "Publish Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
