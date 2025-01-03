namespace LogTime;

static class Program
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            SquirrelAwareApp.HandleEvents(onInitialInstall: OnAppInstall, onAppUninstall: OnAppUninstall, onEveryRun: OnAppRun);

            using var mutex = new Mutex(false, @"Global\" + Constants.MutexName);

            if (!mutex.WaitOne(0, false))
            {
                BringExistingInstanceToFront();
                Environment.Exit(0);
            }

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unhandled exception: " + ex.ToString());
        }
    }

    private static void BringExistingInstanceToFront()
    {
        Process currentProcess = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

        foreach (Process process in processes)
        {
            if (process.Id != currentProcess.Id &&
                process.MainModule?.FileName == currentProcess.MainModule?.FileName &&
                process.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(process.MainWindowHandle, Constants.SW_RESTORE);
                SetForegroundWindow(process.MainWindowHandle);
                break;
            }
        }
    }

    private static void OnAppInstall(SemanticVersion version, IAppTools tools)
    {
        tools.CreateShortcutForThisExe(ShortcutLocation.StartMenu | ShortcutLocation.Desktop);
    }

    private static void OnAppUninstall(SemanticVersion version, IAppTools tools)
    {
        tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenu | ShortcutLocation.Desktop);
    }

    private static void OnAppRun(SemanticVersion version, IAppTools tools, bool firstRun)
    {
        tools.SetProcessAppUserModelId();
        // show a welcome message when the app is first installed
        if (firstRun) MessageBox.Show("Thanks for installing LogTime!");
    }
}
