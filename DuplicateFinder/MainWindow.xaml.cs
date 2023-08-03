using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DuplicateFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string FolderToAnalyze { get; set; } = @"\\SynologyNas002\homes\veronika.rainer\Photos";

        public MainWindow()
        {
            InitializeComponent();
        }

        async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            FolderToAnalyze = PathTextBox.Text;
            
            var result = (await SearchFilesWithSameNameAndSize(FolderToAnalyze)).ToList();

            // Write Result to a text file
            var logFilePath = Path.Combine(FolderToAnalyze, "Dupletten.txt");
            await using (var sw = new StreamWriter(logFilePath))
            {
                foreach (var item in result)
                {
                    foreach (var file in item)
                    {
                        await sw.WriteLineAsync(file);
                    }

                    await sw.WriteLineAsync();
                }
            }

            // Show the results in the FilesListView
            FilesListView.ItemsSource = result.FirstOrDefault();

            // Show the number of duplicates in the Title
            Title = $"Duplicate Finder - {result.Count()} duplicates found";

            // Delete one of the duplicate files
            DeleteDuplicates(result);
        }

        Task<IEnumerable<List<string>>> SearchFilesWithSameNameAndSize(string rootFolderPath)
        {
            var filesByNameAndSize = new Dictionary<string, List<string>>();

            // Get all files in the root folder and its subfolders
            var allFiles = Directory.GetFiles(rootFolderPath, "*", SearchOption.AllDirectories).ToList();
            
            ProgressBar1.Maximum = allFiles.Count;
            ProgressBar1.Value = 0;

            foreach (var filePath in allFiles)
            {
                var fileInfo = new FileInfo(filePath);

                // Create a unique key combining file name and file size
                var key = $"{fileInfo.Name}-{fileInfo.Length}";

                // If the key exists in the dictionary, add the file path to the list
                if (filesByNameAndSize.TryGetValue(key, out var value))
                {
                    value.Add(filePath);
                }
                else
                {
                    // If the key doesn't exist, create a new list and add the file path
                    filesByNameAndSize[key] = new List<string> { filePath };
                }
                
                // Adjust progressbar value
                if (ProgressBar1.Value< ProgressBar1.Maximum)
                {
                    ProgressBar1.Value++;
                }
            }

            // Filter out the entries where only one file with a specific name and size exists
            var duplicateFiles = filesByNameAndSize.Values.Where(list => list.Count > 1);

            return Task.FromResult(duplicateFiles);
        }

        void DeleteDuplicates(ICollection<List<string>> duplicateFiles)
        {
            var filesDeleted = new List<string>();

            ProgressBar2.Maximum = duplicateFiles.Count;
            ProgressBar2.Value = 0;
            
            // Delete all duplicates except one
            foreach (var fileList in duplicateFiles)
            {
                // Keep the first file and delete the rest
                for (var i = 1; i < fileList.Count; i++)
                {
                    try
                    {
                        File.Delete(fileList[i]);
                        filesDeleted.Add(fileList[i]);
                        
                        // Adjust progressbar value
                        if (ProgressBar2.Value< ProgressBar2.Maximum)
                        {
                            ProgressBar2.Value++;
                        }
                    }
                    catch (Exception e)
                    {
                        filesDeleted.Add(e.Message);
                    }
                }
            }

            // Write Result to a text file
            var logFilePath = Path.Combine(FolderToAnalyze, "Dupletten geloescht.txt");
            using (var sw =
                   new StreamWriter(logFilePath))
            {
                foreach (var fileName in filesDeleted)
                {
                    sw.WriteLine(fileName);
                }
            }
        }
    }
}