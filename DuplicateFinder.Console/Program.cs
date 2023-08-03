// See https://aka.ms/new-console-template for more information

using System.Data.SqlTypes;

public class Program
{
    static string FolderToAnalyze { get; set; } = @"\\SynologyNas002\homes\veronika.rainer\Photos";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Find and delete duplicate files");
        Console.WriteLine("====================================");
        Console.WriteLine("Please enter the path to the folder you want to analyze:");
        var path = Console.ReadLine();
        Console.WriteLine("====================================");
        Console.WriteLine(
            "Start finding Duplicates ..., please Wait it can take a long time depending on the number of files");

        var result = (await SearchFilesWithSameNameAndSize(path)).ToList();

        // Write Result to a text file
        var logFilePath = Path.Combine(path, "Dupletten.txt");
        await using (var sw = new StreamWriter(logFilePath))
        {
            await sw.WriteLineAsync("Find Duplicates in Folder: " + path + " at " + DateTime.Now);
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
        
        Console.WriteLine();
        Console.WriteLine("====================================");
        Console.WriteLine("Found " + result.Count + " duplicates");
        Console.WriteLine("You can investigate the result in the file: " + logFilePath);
        
        // Ask the user if he wants to delete the duplicates
        Console.WriteLine("Do you want to delete the duplicates? (y/n)");
        var answer = Console.ReadLine();
        if (answer == null || answer.ToLower() != "y")
        {
            Console.WriteLine("Duplicates not deleted");
        }
        else
        {
            // Delete one of the duplicate files
            var deleteResults = DeleteDuplicates(result);

            // Write Result to a text file
            logFilePath = Path.Combine(FolderToAnalyze, "Dupletten geloescht.txt");
            using (var sw =
                   new StreamWriter(logFilePath))
            {
                foreach (var fileName in deleteResults)
                {
                    sw.WriteLine(fileName);
                }
            }
            
            Console.WriteLine("====================================");
            Console.WriteLine("Deleted " + deleteResults.Count + " duplicates");
            Console.WriteLine("You can investigate the result in the file: " + logFilePath);
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
            if (Console.CursorLeft < 5)
            {
                Console.Write(".");
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
    
    public static void ClearCurrentConsoleLine()
    {
        int currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth)); 
        Console.SetCursorPosition(0, currentLineCursor);
    }

    static List<string> DeleteDuplicates(ICollection<List<string>> duplicateFiles)
    {
        var filesDeleted = new List<string>();

        // Show progress in console

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