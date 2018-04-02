using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;

namespace PDF_Compliance_Converter
{
    /// <summary>
    /// The view model for the main window.
    /// </summary>
    public class Presenter : ObservableObject
    {
        #region Private Member Variables
        private string destination = String.Empty;
        private int errorCount = 0;
        private bool isEnabled = true;
        private List<string> folders = new List<string>();        
        private bool mirrorFolders = false;
        private int processedCount = 0;
        private int progress = 0;
        private string statusMessage = "Ready";
        private BackgroundWorker worker = new BackgroundWorker() {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true            
        };
        #endregion

        #region Properties
        /// <summary>
        /// Returns false when the UI should be disabled otherwise true;
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }

            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    base.OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        /// <summary>
        /// The folders that contain the source data.
        /// </summary>
        public List<string> Folders
        {
            get
            {
                return folders;
            }

            set
            {
                folders = value;
                base.OnPropertyChanged(nameof(Folders));
            }
        }

        /// <summary>
        /// Indicates if the folders in each source folder should be created in the destination folder.
        /// </summary>
        public bool MirrorFolders
        {
            get
            {
                return mirrorFolders;
            }

            set
            {
                if (mirrorFolders != value)
                {
                    mirrorFolders = value;
                    base.OnPropertyChanged(nameof(MirrorFolders));
                }
            }
        }

        /// <summary>
        /// The progress percentage of the work being done.
        /// </summary>
        public int Progress
        {
            get
            {
                return progress;
            }

            set
            {
                progress = value;
                base.OnPropertyChanged(nameof(Progress));
            }
        }

        /// <summary>
        /// A status message of the work being done.
        /// </summary>
        public string StatusMessage
        {
            get
            {
                return statusMessage;
            }

            set
            {
                if (statusMessage != value)
                {
                    statusMessage = value;
                    base.OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        /// <summary>
        /// The add folder command.
        /// </summary>
        public ICommand AddFolderCommand
        {
            get
            {
                var command = new DelegateCommand(() => AddFolder());
                return command;
            }
        }

        /// <summary>
        /// The cancel command for the background worker.
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                var command = new DelegateCommand(() => Cancel());
                return command;
            }
        }

        /// <summary>
        /// The execute command for the background worker.
        /// </summary>
        public ICommand ExecuteCommand
        {
            get
            {
                var command = new DelegateCommand(() => Execute());
                return command;
            }
        }

        /// <summary>
        ///  The remove folder command.
        /// </summary>
        public ICommand RemoveFolderCommand
        {
            get
            {
                var command = new DelegateCommand((param) => RemoveFolder(param));
                return command;
            }
        }
        #endregion

        #region Methods
        private void AddFolder()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                string folder = dialog.SelectedPath;
                Folders.Add(folder);
            }
        }

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as WorkArgs;
            CancellationTokenSource token = new CancellationTokenSource();
            Converter converter = new Converter(token);
            converter.Error += (s, ex) => worker.ReportProgress(0, ex);
            converter.PdfConverted += (s, fi) => worker.ReportProgress(0, fi);
            converter.ReportFolderProgress += (s, p) => worker.ReportProgress(p);

            if (worker.CancellationPending)
                token.Cancel();

            try
            {
                converter.ConvertPDFsInFolders(
                    args.Folders.ToArray(), 
                    args.Destination, 
                    args.MirrorFolders);
                e.Result = null;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void BackgroundWorkProgress(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState != null && e.UserState.GetType().Equals(typeof(FileInfo)))
            {
                var file = e.UserState as FileInfo;
                this.processedCount++;
                StatusMessage = $"Processing: {file.Name}";
            }
            else if (e.UserState != null && e.UserState.GetType().Equals(typeof(Exception)))
            {
                var ex = e.UserState as Exception;
                this.errorCount++;
                StatusMessage = $"Error: {ex.Message}";
            }
            else
            {
                Progress = e.ProgressPercentage;
            }
        }

        private void BackgroundWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null && e.Result.GetType().Equals(typeof(Exception)))
            {
                var ex = e.Result as Exception;
                MessageBox.Show($"There was a problem processing the files. {ex.Message}");
            }
            else
            {
                Progress = 100;
                string msg = $"The process has completed with {processedCount} files processed and {errorCount} errors.";
                MessageBox.Show(msg);
            }
        }

        private void Cancel()
        {
            worker.CancelAsync();
        }

        private void Execute()
        {
            IsEnabled = false;
            Progress = 0;
            WorkArgs args = new WorkArgs();
            args.Destination = new DirectoryInfo(destination);
            args.Folders = folders;
            args.MirrorFolders = mirrorFolders;
            worker.DoWork += BackgroundWork;
            worker.ProgressChanged += BackgroundWorkProgress;
            worker.RunWorkerCompleted += BackgroundWorkComplete;
            worker.RunWorkerAsync(args);
        }

        private void RemoveFolder(object parameter)
        {
            string folder = parameter as string;
            Folders.Remove(folder);
        }
        #endregion

        #region Classes
        private class WorkArgs
        {
            public DirectoryInfo Destination { get; set; }
            public IEnumerable<string> Folders { get; set; }
            public bool MirrorFolders { get; set; }
        }
        #endregion
    }
}
