using System;
#if DEBUG
using System.Runtime.InteropServices;
#endif

using System.Windows.Forms;

namespace GoogleImageShell
{
#if DEBUG
    public static class Imports
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AllocConsole();
    }
#endif
    
    public static class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();


        [STAThread]
        public static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length >= 1 && args[0].ToLower() == "search")
            {
#if DEBUG
                Imports.AllocConsole();
#endif
                Application.Run(new UploadForm(args));
            }
            else
            {
#if DEBUG
                Imports.AllocConsole();
#endif
                Application.Run(new ConfigForm());
            }
        }
    }
}
