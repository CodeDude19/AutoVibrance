namespace AutoVibrance;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Ensure only one instance runs
        using var mutex = new Mutex(true, "AutoVibranceApp", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Auto Vibrance is already running.", "Auto Vibrance",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayApplication());
    }
}
