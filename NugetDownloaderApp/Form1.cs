using System;
using System.ComponentModel;
using System.Windows.Forms;
using NugetWorker;

namespace NugetDownloaderApp
{
    public partial class Form1 : Form
    {
        #region constructors

        public Form1()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
        }

        #endregion

        #region methods

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var logger = NugetHelper.Instance.logger;
            var pack = textBox1.Text.Trim();

            var packageName = pack.Length == 0
                                  ? "Newtonsoft.Json"
                                  : pack;

            var version = "";

            try
            {
                logger.LogInformation("!!!!!!!!!Begin!!!!!!!!!");
                var nugetEngine = new NugetEngine();

                nugetEngine.GetPackage(packageName, version)
                           .Wait();

                foreach (var dll in nugetEngine.dllInfos)
                {
                    logger.LogInformation($"Relavant dll : {dll.rootPackage} | {dll.framework} | {dll.path} | ");
                }

            }

            catch (Exception ex)
            {
                logger.LogInformation($"Exception Occured : {ex.Message} | {ex.StackTrace}");
            }
            finally
            {
                logger.LogInformation("!!!!!!!!!!End!!!!!!!!!!");
                e.Cancel = true;
            }
        }

        // This event handler updates the progress bar.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e) { }

        // This event handler deals with the results of the
        // background operation.
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                MessageBox.Show("Ok!");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
        }

        #endregion
    }
}
