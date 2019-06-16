using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string outputDirectoryPath = "";

        public MainWindow()
        {
            InitializeComponent();

            ((INotifyCollectionChanged)FileList.Items).CollectionChanged += FileList_CollectionChanged;

            OutputDirLabel.Content = "";
            ShowProcessing(false);
            EnableListButtons(false);
        }

        void FileList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EnableListButtons(FileList.Items.Count > 0);
        }

        void EnableListButtons(bool enable)
        {
            DeleteSelectedButton.IsEnabled = enable;
            ClearAllButton.IsEnabled = enable;

            EnableStartButton(enable);
        }

        void EnableStartButton(bool enable)
        {
            StartButton.IsEnabled = enable && !string.IsNullOrEmpty(outputDirectoryPath);
        }

        void ShowProcessing(bool show)
        {
            ProcessingLabel.IsEnabled = show;
            ProcessingLabel.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            ProcessingBar.IsEnabled = show;
            ProcessingBar.Visibility = show ? Visibility.Visible : Visibility.Hidden;
        }

        void AddFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();

            if (result == false)
                return;

            var fi = new FileInfo(dialog.FileName);

            FileList.Items.Add(new ListEntry(fi));
        }

        void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            if (string.IsNullOrWhiteSpace(dialog.SelectedPath))
                return;

            var di = new DirectoryInfo(dialog.SelectedPath);

            foreach (var file in di.GetFiles())
            {
                FileList.Items.Add(new ListEntry(file));
            }
        }

        async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            SetAllButtons(false);
            ShowProcessing(true);

            var formatString = FormatSelection.SelectionBoxItem.ToString().ToLowerInvariant();
            var entries = FileList.Items.Cast<ListEntry>().ToArray();

            ProcessingBar.Value = 0;
            ProcessingBar.Minimum = 0;
            ProcessingBar.Maximum = entries.Length;

            await Task.Run(async () =>
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];

                    ExecuteProcess($"ffmpeg -i \"{entry.Path}\" -vcodec copy -acodec copy \"{Path.Combine(outputDirectoryPath, entry.Name)}.{formatString}\"");

                    await Dispatcher.InvokeAsync(() =>
                    {
                        ProcessingLabel.Content = $"Processing file {i + 1} of {entries.Length}.";
                        ProcessingBar.Value = i + 1;
                    });
                }
            }).ContinueWith(async _ =>
            {
                MessageBox.Show($"Converted {entries.Length} file(s).", "Success");

                await Dispatcher.InvokeAsync(() =>
                {
                    SetAllButtons(true);
                    ShowProcessing(false);
                });
            });
        }

        void SetAllButtons(bool enable)
        {
            AddFilesButton.IsEnabled = enable;
            AddFolderButton.IsEnabled = enable;

            EnableListButtons(enable);

            FormatSelection.IsEnabled = enable;
            BrowseButton.IsEnabled = enable;
        }

        static void ExecuteProcess(string command)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            process.Start();

            process.StandardInput.WriteLine(command);
            process.StandardInput.Flush();
            process.StandardInput.Close();
            process.WaitForExit();
        }

        void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            if (string.IsNullOrWhiteSpace(dialog.SelectedPath))
                return;

            outputDirectoryPath = dialog.SelectedPath;
            OutputDirLabel.Content = new DirectoryInfo(outputDirectoryPath).Name;

            FileList_CollectionChanged(this, null);
        }

        void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.Items.IsEmpty)
                return;

            if (FileList.SelectedItem is null)
                return;

            FileList.Items.Remove(FileList.SelectedItem);
        }

        void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.Items.IsEmpty)
                return;

            FileList.Items.Clear();
        }
    }

    public class ListEntry
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public string Path { get; set; }

        public ListEntry(string path)
        {
            var fi = new FileInfo(path);

            Name = System.IO.Path.GetFileNameWithoutExtension(path);
            Extension = fi.Extension;
            Path = fi.FullName;
        }

        public ListEntry(FileInfo fi)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
            Extension = fi.Extension;
            Path = fi.FullName;
        }

        public override string ToString() => Name + Extension;
    }
}
