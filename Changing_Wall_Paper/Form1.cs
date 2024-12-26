using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Changing_Wall_Paper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Import the SystemParametersInfo function from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(uint action, uint uParam, string vParam, uint winIni);

        // Constants for the function
        public static readonly uint SPI_SETDESKWALLPAPER = 0x14;
        public static readonly uint SPIF_UPDATEINIFILE = 0x01;
        public static readonly uint SPIF_SENDCHANGE = 0x02;

        public static void SetWallpaper(string path)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string wallpaperPath = openFileDialog1.FileName;
                if (File.Exists(wallpaperPath))
                {
                    SetWallpaper(wallpaperPath);
                    MessageBox.Show("Your wallpaper has been changed successfully.");
                }
                else
                {
                    Console.WriteLine("The file does not exist.");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Import GetSystemMetrics from user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetSystemMetrics(int nIndex);

        // Getting Height and Width of Screen
        public static void GettingHeightWidthOfScreen(Label lblWidth, Label lblHeight)
        {
            // Get the height and width of the screen
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);
            lblHeight.Text = screenHeight + " px";
            lblWidth.Text = screenWidth + " px";
        }

        // Define the SYSTEM_POWER_STATUS structure
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte Reserved1;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        // Import GetSystemPowerStatus from kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);

        static string GetBatteryStatus(byte flag)
        {
            switch (flag)
            {
                case 1:
                    return "High, more than 66% charged";
                case 2:
                    return "Low, less than 33% charged";
                case 4:
                    return "Critical, less than 5% charged";
                case 8:
                    return "Charging";
                case 128:
                    return "No battery";
                case 255:
                    return "Unknown status";
                default:
                    return "Battery status not detected";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GettingHeightWidthOfScreen(lblWidth, lblHieght);

            if (GetSystemPowerStatus(out SYSTEM_POWER_STATUS status))
            {
                lblBattery.Text = "AC Line Status: " + (status.ACLineStatus == 0 ? "Offline" : "Online");
                lblAcLine.Text = "Battery Charge Status: " + GetBatteryStatus(status.BatteryFlag);
                Battery_Charge_Status.Text = "Battery Life Percent: " + (status.BatteryLifePercent == 255 ? "Unknown" : status.BatteryLifePercent + "%");
                Battery_Life_Remaining.Text = "Battery Life Remaining: " + (status.BatteryLifeTime == -1 ? "Unknown" : status.BatteryLifeTime + " seconds");
                Full_Battery_Lifetime.Text = "Full Battery Lifetime: " + (status.BatteryFullLifeTime == -1 ? "Unknown" : status.BatteryFullLifeTime + " seconds");
            }
            else
            {
                lblBattery.Text = "Unable to get battery status.";
                lblAcLine.Text = "";
                Battery_Charge_Status.Text = "";
                Battery_Life_Remaining.Text = "";
                Full_Battery_Lifetime.Text = "";
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        public struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID Luid;
            public int Attributes;
        }

        const int SE_PRIVILEGE_ENABLED = 0x00000002;
        const int TOKEN_QUERY = 0x00000008;
        const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        const uint EWX_LOGOFF = 0x00000000;
        const uint EWX_SHUTDOWN = 0x00000001;
        const uint EWX_REBOOT = 0x00000002;
        const uint EWX_FORCE = 0x00000004;

        static void EnableShutdownPrivilege()
        {
            if (!OpenProcessToken(System.Diagnostics.Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle))
            {
                MessageBox.Show("Failed to open process token.");
                return;
            }

            if (!LookupPrivilegeValue(null, "SeShutdownPrivilege", out LUID luid))
            {
                MessageBox.Show("Failed to look up privilege value.");
                return;
            }

            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            if (!AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
            {
                MessageBox.Show("Failed to adjust token privileges.");
                return;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            EnableShutdownPrivilege();

            // Shutdown the system
            if (!ExitWindowsEx(EWX_SHUTDOWN | EWX_FORCE, 0))
            {
                MessageBox.Show("Sorry, can't close it while doing other things. Try again after closing all apps.");
            }
        }
    }
}
