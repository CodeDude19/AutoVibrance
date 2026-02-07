using System.Runtime.InteropServices;

namespace AutoVibrance;

public class OverlayForm : Form
{
    private readonly Label _label;

    // Window styles for click-through transparency
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOPMOST = 0x8;
    private const int WS_EX_TOOLWINDOW = 0x80;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE = -20;

    public OverlayForm()
    {
        // Form setup
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(20, 20);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        Opacity = 0.85;
        Padding = new Padding(8);

        // Label for displaying info
        _label = new Label
        {
            AutoSize = true,
            Location = new Point(8, 8),
            ForeColor = Color.Lime,
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 11f, FontStyle.Bold),
            Text = "Visibility: --\nLuminance: --"
        };
        Controls.Add(_label);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Make window click-through
        int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
        SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    public void UpdateValues(float gamma, float luminance)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateValues(gamma, luminance));
            return;
        }

        _label.Text = $"Visibility: {gamma:F2}\nLuminance: {luminance:F0}";
    }

    public void SetEnabled(bool enabled)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetEnabled(enabled));
            return;
        }

        if (enabled)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
}
