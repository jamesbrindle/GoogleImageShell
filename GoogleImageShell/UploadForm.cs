using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoogleImageShell
{
    public partial class UploadForm : Form
    {
        private readonly string _imagePath;
        private readonly bool _includeFileName;
        private readonly bool _resizeOnUpload;

        private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();

        public UploadForm(string[] args)
        {
            InitializeComponent();
            for (var i = 1; i < args.Length; ++i)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "-n":
                        _includeFileName = true;
                        break;
                    case "-r":
                        _resizeOnUpload = true;
                        break;
                    default:
                        _imagePath = arg;
                        break;
                }
            }
        }

        private void UploadForm_Load(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            sb.Append($"Uploading image: {_imagePath}\n");
            sb.Append($"Include file name: {_includeFileName}\n");
            sb.Append($"Resize on upload: {_resizeOnUpload}\n");
#if DEBUG
            Console.WriteLine(sb.ToString());
#endif
            Log(sb.ToString());

            var task = GoogleImages.Search(_imagePath, _includeFileName, _resizeOnUpload, _cancelTokenSource.Token);
            task.ContinueWith(OnUploadComplete, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void UploadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cancelTokenSource.Cancel();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OnUploadComplete(Task<string> task)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
#if DEBUG
                    Console.WriteLine("Failed to upload image: " + task.Exception.InnerException);
#endif
                    Log("Failed to upload image: " + task.Exception.InnerException);
                    break;
                case TaskStatus.Canceled:
#if DEBUG
                    Console.WriteLine("Upload canceled by user");
#endif
                    Log("Upload canceled by user");
                    break;
                case TaskStatus.RanToCompletion:
                {
#if DEBUG
                    Console.WriteLine("Image uploaded successfully, opening results page");
#endif

                    Log("Image uploaded successfully, opening results page");
                    if (TryOpenBrowser(task))
                    {
#if !DEBUG
                        Close();
#endif
#if DEBUG
                        Console.WriteLine("Debug compilation enabled form will remain open");
                        Log("Debug compilation enabled form will remain open");
#endif
                        return;
                    }

                    break;
                }
                case TaskStatus.Created:
                    break;
                case TaskStatus.WaitingForActivation:
                    break;
                case TaskStatus.WaitingToRun:
                    break;
                case TaskStatus.Running:
                    break;
                case TaskStatus.WaitingForChildrenToComplete:
                    break;
                default:
#if DEBUG
                    Console.WriteLine("Unexpected task result status: " + task.Status);
#endif
                    Log("Unexpected task result status: " + task.Status);
                    break;
            }
            cancelButton.Text = "Close";
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
        }

        private bool TryOpenBrowser(Task<string> task)
        {
            try
            {
                Process.Start(task.Result);
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine("Failed to open browser: " + ex);
#endif
                Log("Failed to open browser: " + ex);
                return false;
            }
        }

        private void Log(string text)
        {
            logTextBox.AppendText(text);
            logTextBox.AppendText(Environment.NewLine);
        }
    }
}
