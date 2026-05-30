using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Forms;

static class Program
{
    [DllImport("xinput1_4.dll")]
    static extern uint XInputGetState(uint idx, out XInputState state);

    [DllImport("xinput1_4.dll")]
    static extern uint XInputGetBatteryInformation(uint idx, byte dev, out BattInfo info);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr h);

    // XInputGetState struct -- fields unused but required for marshalling
    [StructLayout(LayoutKind.Sequential)]
    struct XInputState
    {
        public uint   PacketNumber;
        public ushort Buttons;
        public byte   LeftTrigger;
        public byte   RightTrigger;
        public short  ThumbLX;
        public short  ThumbLY;
        public short  ThumbRX;
        public short  ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BattInfo { public byte Type; public byte Level; }

    const byte GAMEPAD           = 0x00;
    const byte TYPE_DISCONNECTED = 0x00;
    const byte TYPE_WIRED        = 0x01;
    // Internal pseudo-state: XInputGetState reports connected but battery data not yet available
    const byte TYPE_WAITING      = 0xFE;
    const byte LVL_EMPTY         = 0x00;
    const byte LVL_LOW           = 0x01;
    const byte LVL_MED           = 0x02;
    const byte LVL_FULL          = 0x03;

    static Icon icoDisconnected;
    static Icon icoFull;
    static Icon icoMedium;
    static Icon icoLow;
    static Icon icoWired;

    static NotifyIcon        tray;
    static ToolStripMenuItem startupItem;
    static byte prevType  = 0xFF;
    static byte prevLevel = 0xFF;
    static bool xinputOk  = true;

    const string APP_NAME    = "XboxChargeMon";
    const string STARTUP_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        LoadIcons();

        tray = new NotifyIcon { Visible = true, Text = "Xbox Charge Mon" };
        tray.Icon = icoDisconnected;

        var menu = new ContextMenuStrip();

        var aboutItem = new ToolStripMenuItem("About XboxChargeMon");
        aboutItem.Click += (s, e) =>
        {
            MessageBox.Show(
                "XboxChargeMon\n\n" +
                "A lightweight tray utility for monitoring Xbox controller battery level on Windows.\n\n" +
                "Reads battery data via XInput every 2 seconds. Because the Xbox wireless protocol " +
                "does not expose a charging flag, charging is inferred when the battery level rises " +
                "between polls.\n\n" +
                "Icons load from an icons\\ folder next to the EXE if present, otherwise fall back " +
                "to the versions embedded at build time.",
                "About XboxChargeMon",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        };
        menu.Items.Add(aboutItem);
        menu.Items.Add(new ToolStripSeparator());

        startupItem         = new ToolStripMenuItem("Start with Windows");
        startupItem.Checked = IsStartup();
        startupItem.Click  += (s, e) =>
        {
            bool currentlyEnabled = IsStartup();

            if (!currentlyEnabled)
            {
                DialogResult confirm = MessageBox.Show(
                    "XboxChargeMon will run automatically when Windows starts.\n\n" +
                    "To remove it later, right-click the tray icon and uncheck \"Start with Windows\".",
                    "Add to Startup",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (confirm != DialogResult.Yes) return;
            }

            bool enable         = !currentlyEnabled;
            SetStartup(enable);
            startupItem.Checked = enable;
        };
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (s, e) => { tray.Visible = false; Application.Exit(); });

        tray.ContextMenuStrip = menu;
        tray.DoubleClick     += (s, e) =>
            tray.ShowBalloonTip(3000, "Xbox Charge Mon", tray.Text, ToolTipIcon.None);

        var timer = new Timer { Interval = 2000 };
        timer.Tick += (s, e) => Poll();
        timer.Start();
        Poll();

        Application.Run();
    }

    static void Poll()
    {
        if (!xinputOk) return;

        for (uint i = 0; i < 4; i++)
        {
            XInputState xstate;
            uint stateRes;
            try { stateRes = XInputGetState(i, out xstate); }
            catch (DllNotFoundException)
            {
                xinputOk = false;
                SetTray("Xbox | XInput not found", icoDisconnected);
                return;
            }

            if (stateRes != 0) continue;

            BattInfo bi;
            XInputGetBatteryInformation(i, GAMEPAD, out bi);

            if (bi.Type == TYPE_DISCONNECTED)
            {
                // Controller is connected (XInputGetState confirmed) but battery data
                // has not been sent yet. Keepalive packets will trigger it within seconds.
                if (prevType != TYPE_WAITING)
                {
                    prevType  = TYPE_WAITING;
                    prevLevel = 0xFF;
                    SetTray("Xbox | Connected | Reading...", icoDisconnected);
                }
                return;
            }

            bool typeChanged  = bi.Type  != prevType;
            bool levelChanged = bi.Level != prevLevel;

            if (!typeChanged && !levelChanged) return;

            bool levelRose = levelChanged && prevLevel != 0xFF && bi.Level > prevLevel;
            bool levelFell = levelChanged && prevLevel != 0xFF && bi.Level < prevLevel;

            if (bi.Type == TYPE_WIRED)
            {
                SetTray("Xbox | USB connected", icoWired);
            }
            else
            {
                string label, time, direction;
                Icon   ico;

                switch (bi.Level)
                {
                    case LVL_MED:
                        label = "Medium"; time = "~8-12h";  ico = icoMedium; break;
                    case LVL_FULL:
                        label = "Full";   time = "~15-20h"; ico = icoFull;   break;
                    default:
                        label = "Low";    time = "~2-4h";   ico = icoLow;    break;
                }

                direction = levelRose ? " (rising)" : (levelFell ? " (dropping)" : "");
                SetTray("Xbox | " + label + direction + " | " + time, ico);

                // Notify on first detection at low, or when level drops into low range
                bool justWentLow = (bi.Level == LVL_LOW || bi.Level == LVL_EMPTY)
                                && (levelFell || prevLevel == 0xFF || prevLevel > LVL_LOW);
                if (justWentLow)
                    tray.ShowBalloonTip(7000, "Xbox Controller", "charge your controller.", ToolTipIcon.None);
            }

            prevType  = bi.Type;
            prevLevel = bi.Level;
            return;
        }

        if (prevType != TYPE_DISCONNECTED)
        {
            prevType  = TYPE_DISCONNECTED;
            prevLevel = 0xFF;
            SetTray("Xbox | No controller", icoDisconnected);
        }
    }

    static void SetTray(string text, Icon icon)
    {
        tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        tray.Icon = icon;
    }

    static void LoadIcons()
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");

        icoDisconnected = TryLoad(dir, "disconnected.png") ?? Fallback(Color.DimGray);
        icoFull         = TryLoad(dir, "full.png")         ?? Fallback(Color.LimeGreen);
        icoMedium       = TryLoad(dir, "medium.png")       ?? Fallback(Color.Gold);
        icoLow          = TryLoad(dir, "low.png")          ?? Fallback(Color.OrangeRed);
        icoWired        = TryLoad(dir, "wired.png")        ?? Fallback(Color.DeepSkyBlue);
    }

    static Icon TryLoad(string dir, string file)
    {
        // External icons folder takes priority -- allows icon replacement without rebuilding
        try
        {
            string path = Path.Combine(dir, file);
            if (File.Exists(path))
            {
                using (var bmp = new Bitmap(path))
                {
                    IntPtr h = bmp.GetHicon();
                    try   { return (Icon)Icon.FromHandle(h).Clone(); }
                    finally { DestroyIcon(h); }
                }
            }
        }
        catch { }

        // Fall back to icons embedded in the EXE at build time
        try
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file))
            {
                if (stream != null)
                {
                    using (var bmp = new Bitmap(stream))
                    {
                        IntPtr h = bmp.GetHicon();
                        try   { return (Icon)Icon.FromHandle(h).Clone(); }
                        finally { DestroyIcon(h); }
                    }
                }
            }
        }
        catch { }

        return null;
    }

    // Used when no icon file exists externally or embedded
    static Icon Fallback(Color color)
    {
        using (var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
        using (var g   = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            using (var b = new SolidBrush(color))
                g.FillRectangle(b, 2, 2, 12, 12);
            IntPtr h = bmp.GetHicon();
            try   { return (Icon)Icon.FromHandle(h).Clone(); }
            finally { DestroyIcon(h); }
        }
    }

    static bool IsStartup()
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(STARTUP_KEY))
                return k != null && k.GetValue(APP_NAME) != null;
        }
        catch { return false; }
    }

    static void SetStartup(bool enable)
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(STARTUP_KEY, true))
            {
                if (k == null) return;
                if (enable) k.SetValue(APP_NAME, Application.ExecutablePath);
                else        k.DeleteValue(APP_NAME, false);
            }
        }
        catch { }
    }
}
