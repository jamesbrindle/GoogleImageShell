using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GoogleImageShell
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool AllocConsole();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length >= 1 && args[0].ToLower() == "search")
            {
                // AllocConsole();
                Application.Run(new UploadForm(args));
            }
            else
            {
                // AllocConsole();
                Application.Run(new ConfigForm());
            }
        }
    }
}
