using System;
using System.Windows.Forms;

namespace NugetDownloaderApp
{
    static class Program
    {
        #region methods

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form2());
        }

        #endregion
    }
}
