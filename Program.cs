using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

class Program
{
    private static readonly List<string> Tasks = new()
    {
        "Task1",
        "Task2",
        "Task3",
        "Task4",
        "Task5"
    };

    private const string StatusFile = "taskstatus.json";
    private const string TempFile = "taskstatus.json.tmp";

    static void Main(string[] args)
    {
        try
        {
            var completedTasks = LoadCompletedTasks();

            Console.WriteLine("Starting task processor...");
            Console.WriteLine($"Completed tasks found: {string.Join(", ", completedTasks)}");

            foreach (var task in Tasks)
            {
                if (completedTasks.Contains(task))
                {
                    Console.WriteLine($"Skipping {task} (already completed)");
                    continue;
                }

                ProcessTask(task);

                completedTasks.Add(task);
                SaveCompletedTasksAtomically(completedTasks);

                Console.WriteLine($"{task} completed and saved.");
            }

            Console.WriteLine("All tasks processed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application crashed: {ex.Message}");
            Console.WriteLine("State has been persisted. Restart the app to resume.");
        }
    }

    private static void ProcessTask(string task)
    {
        Console.WriteLine($"Processing {task}...");
        Thread.Sleep(1000);

        // Simulated crash
        if (task == "Task3")
        {
            throw new Exception("Unexpected crash!");
        }
    }

    private static HashSet<string> LoadCompletedTasks()
    {
        if (!File.Exists(StatusFile))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(StatusFile);
            var tasks = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return new HashSet<string>(tasks, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // If file is corrupted for any reason, fail gracefully
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void SaveCompletedTasksAtomically(HashSet<string> completedTasks)
    {
        var json = JsonSerializer.Serialize(
            completedTasks.OrderBy(x => x).ToList(),
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        using (var stream = new FileStream(
            TempFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.WriteThrough))
            
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(json);
            writer.Flush();
            stream.Flush(true);
        }

        if (File.Exists(StatusFile))
        {
            File.Replace(TempFile, StatusFile, null);
        }
        else
        {
            File.Move(TempFile, StatusFile);
        }
    }

}
