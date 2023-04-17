using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GoogleImageShell
{
    public static class Imports
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AllocConsole();
    }
    
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
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
