// See https://aka.ms/new-console-template for more information

namespace DuplicateFinder.Console;

public abstract class Program
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("Find and delete duplicate files");
        System.Console.WriteLine("====================================");
        System.Console.WriteLine("Please enter the path to the folder you want to analyze:");
        
        var duplicateSearchFilePath = System.Console.ReadLine();
        
        System.Console.WriteLine("====================================");
        System.Console.WriteLine(
            "Start finding Duplicates ..., please Wait it can take a long time depending on the number of files");

        var result = (await SearchFilesWithSameNameAndSize(duplicateSearchFilePath ?? throw new InvalidOperationException("Path cannot be null"))).ToList();

        if (Directory.Exists(duplicateSearchFilePath)==false) throw new InvalidOperationException("Path does not exist");
        
        // Write Result to a text file
        var logFilePath = Path.Combine(duplicateSearchFilePath, "Dupletten.txt");
        await using (var sw = new StreamWriter(logFilePath))
        {
            await sw.WriteLineAsync("Find Duplicates in Folder: " + duplicateSearchFilePath + " at " + DateTime.Now);
            await sw.WriteLineAsync("====================================");
            foreach (var item in result)
            {
                foreach (var file in item)
                {
                    await sw.WriteLineAsync(file);
                }

                await sw.WriteLineAsync();
            }
            await sw.WriteLineAsync("End of List at " + DateTime.Now);
        }
        
        System.Console.WriteLine();
        System.Console.WriteLine("====================================");
        System.Console.WriteLine("Found " + result.Count + " duplicates");
        System.Console.WriteLine("You can investigate the result in the file: " + logFilePath);
        
        // Ask the user if he wants to delete the duplicates or move them to a specific folder
        System.Console.WriteLine("====================================");
        System.Console.WriteLine("Do you want to delete the duplicates or move them to a specific folder? (delete/move/abort)");
        var answer = System.Console.ReadLine();
        
        if(answer==null) return;
        
        if (answer.ToLower() == "delete")
        {
            // Delete one of the duplicate files
            var deleteResults = DeleteDuplicates(result);

            // Write Result to a text file
            logFilePath = Path.Combine(duplicateSearchFilePath, "Duplicates deleted.txt");
            await using (var sw =
                         new StreamWriter(logFilePath))
            {
                foreach (var fileName in deleteResults)
                {
                    await sw.WriteLineAsync(fileName);
                }
            }
            
            System.Console.WriteLine("====================================");
            System.Console.WriteLine("Deleted " + deleteResults.Count + " duplicates");
            System.Console.WriteLine("You can investigate the result in the file: " + logFilePath);
        }

        if (answer.ToLower() == "move")
        {
            System.Console.WriteLine("Enter the folder where you want to move the duplicates:");
            var newFolder = System.Console.ReadLine();
            if(Directory.Exists(newFolder) == false)
            {
                System.Console.WriteLine("====================================");
                System.Console.WriteLine("The folder " + newFolder + " does not exist");
                return;
            }

            void ProgressCallback(int current, int total)
            {
                ClearCurrentConsoleLine();
                System.Console.Write("Moved " + current + " of " + total + " duplicates");
            }

            var moveResult = MoveDuplicates(result, newFolder, ProgressCallback);
            
            // Write Result to a text file
            logFilePath = Path.Combine(duplicateSearchFilePath, "Duplicates moved.txt");
            await using (var sw =
                         new StreamWriter(logFilePath))
            {
                foreach (var tuple in moveResult)
                {
                    await sw.WriteLineAsync(tuple.Item1 + " -> " + tuple.Item2);
                }
            }
            
            System.Console.WriteLine("====================================");
            System.Console.WriteLine("Moved " + moveResult.Count + " duplicates");
            System.Console.WriteLine("You can investigate the result in the file: " + logFilePath);
        }
    }


    static Task<IEnumerable<List<string>>> SearchFilesWithSameNameAndSize(string rootFolderPath)
    {
        var filesByNameAndSize = new Dictionary<string, List<string>>();

        // Get all files in the root folder and its subfolders
        var allFiles = Directory.GetFiles(rootFolderPath, "*", SearchOption.AllDirectories).ToList();

        // Show progress in Console

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

            // Show progress in Console
            if (System.Console.CursorLeft < 5)
            {
                System.Console.Write(".");
            }
            else
            {
                ClearCurrentConsoleLine();
            }
            
        }

        // Filter out the entries where only one file with a specific name and size exists
        var duplicateFiles = filesByNameAndSize.Values.Where(list => list.Count > 1);
        
        return Task.FromResult(duplicateFiles);
    }

    static void ClearCurrentConsoleLine()
    {
        int currentLineCursor = System.Console.CursorTop;
        System.Console.SetCursorPosition(0, System.Console.CursorTop);
        System.Console.Write(new string(' ', System.Console.WindowWidth)); 
        System.Console.SetCursorPosition(0, currentLineCursor);
    }

    static List<Tuple<string, string>> MoveDuplicates(List<List<string>> duplicateFiles, string newFolder, Action<int, int> progressCallback)
    {
        var filesMoved = new List<Tuple<string, string>>();

        // Delete all duplicates except one
        foreach (var fileList in duplicateFiles)
        {
            // Keep the first file and delete the rest
            for (var i = 1; i < fileList.Count; i++)
            {
                var sourceFileName = fileList[i];
                var destFileName = Path.Combine(newFolder, Path.GetFileName(sourceFileName));

                try
                {
                    File.Move(sourceFileName, destFileName);
                    filesMoved.Add(new Tuple<string, string>(sourceFileName, destFileName));

                    // Callback
                    progressCallback?.Invoke(filesMoved.Count, duplicateFiles.Count);
                }
                catch (Exception e)
                {
                    filesMoved.Add(new Tuple<string, string>(sourceFileName,e.Message));
                }
            }
        }

        return filesMoved;
    }

    static List<string> DeleteDuplicates(ICollection<List<string>> duplicateFiles)
    {
        var filesDeleted = new List<string>();

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

                    // Show progress in console
                }
                catch (Exception e)
                {
                    filesDeleted.Add(e.Message);
                }
            }
        }

        return filesDeleted;
    }
}