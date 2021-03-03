using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NuGet;
using NuGet.Frameworks;
using NugetDownloaderApp.NugetWorker;
using NugetDownloaderApp.NugetWorker.Utility;
using NugetDownloaderApp.Reflction;

namespace NugetDownloaderApp
{
    public partial class Form2 : Form
    {
        #region constructors

        public Form2()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
        }

        #endregion

        #region methods

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            toolStripStatusLabel1.Text = "Downloading...";

            var logger = NugetHelper.Instance.logger;
            var pack = textBox1.Text.Trim();

            var packageName = pack.Length == 0
                                  ? "Newtonsoft.Json"
                                  : pack;

            var version = "";

            try
            {
                logger.LogData.Clear();
                logger.LogDebug("");
                logger.LogDebug("!!!!!!!!!Begin!!!!!!!!!");
                var nugetEngine = new NugetEngine();

                nugetEngine.GetPackage(packageName, version)
                           .Wait();

                foreach (var dll in nugetEngine.dllInfos)
                {
                    logger.LogDebug($"Relavant dll : {dll.rootPackage} | {dll.framework} | {dll.path} | ");
                }
            }

            catch (Exception ex)
            {
                logger.LogDebug($"Exception Occured : {ex.Message} | {ex.StackTrace}");
            }
            finally
            {
                logger.LogDebug("!!!!!!!!!!End!!!!!!!!!!");
                logger.LogDebug("");
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
                toolStripStatusLabel1.Text = e.Error.Message;
            }
            else if (e.Cancelled)
            {
                toolStripStatusLabel1.Text = "Finished!";
                richTextBox1.Text = NugetHelper.Instance.logger.LogData.ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox2.Text.Trim()
                            .Length
                    > 0)
                {
                    var nugetFramwork = NuGetFramework.ParseFrameworkName(NugetHelper.Instance.GetTargetFramwork(), new DefaultFrameworkNameProvider());

                    var nupkgPath = textBox2.Text.Trim();
                    var package = new ZipPackage(nupkgPath);

                    //var content = package.GetContentFiles();
                    var files = package.GetLibFiles()
                                       .ToList();

                    var targetDlls = files.FindAll(x => x.EffectivePath.ToLower()
                                                         .Contains(".dll")
                                                        && x.TargetFramework.Identifier.ToLower() == nugetFramwork.Framework.ToLower())
                                          .Take(1);

                    foreach (var item in targetDlls)
                    {
                        if (item.EffectivePath.ToLower()
                                .Contains(".dll"))
                        {
                            var rawAssembly = item.GetStream()
                                                  .ReadAllBytes();

                            var ad = AppDomain.CreateDomain("newDomian");
                            // Loader lives in another AppDomain
                            var loader = (Loader)ad.CreateInstanceAndUnwrap(typeof(Loader).Assembly.FullName, typeof(Loader).FullName);
                            loader.LoadAssembly(rawAssembly);
                            var dd = loader.ExecuteStaticMethod("RestSharp.Validation.Ensure", "NotEmpty", "admin", "admin");
                            var dd1 = loader.ExecuteStaticMethod("RestSharp.SimpleJson", "EscapeToJavascriptString", "admin\\teset\\asdf\\sdaf");
                            richTextBox1.Text = $"RestSharp.SimpleJson.EscapeToJavascriptString(\"admin\\\\teset\\\\asdf\\\\sdaf\") -- {dd1}";
                            ad.DomainUnload += OnAppDomainUnload;
                            AppDomain.Unload(ad);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
        }

        private void OnAppDomainUnload(object sender, EventArgs e) { }

        #endregion

    }
}
