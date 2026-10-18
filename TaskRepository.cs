using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Task_Manager;

public class TaskRepository : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteConnection _connection;

    public TaskRepository(string? databasePath = null)
    {
        _databasePath = databasePath ?? GetDefaultDatabasePath();
        _connection = new SqliteConnection($"Data Source={_databasePath}");
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public static string GetDefaultDatabasePath()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Task_Manager");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "tasks.db");
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Tasks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                DueDate TEXT NOT NULL,
                Priority TEXT NOT NULL,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                CompletedAt TEXT NULL
            );";

        command.ExecuteNonQuery();
    }

    public TaskItem AddTask(TaskItem task)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Tasks (Title, Description, DueDate, Priority, IsCompleted, CompletedAt)
            VALUES (@title, @description, @dueDate, @priority, @isCompleted, @completedAt);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@title", task.Title);
        command.Parameters.AddWithValue("@description", task.Description);
        command.Parameters.AddWithValue("@dueDate", task.DueDate.ToString("O"));
        command.Parameters.AddWithValue("@priority", task.Priority);
        command.Parameters.AddWithValue("@isCompleted", task.IsCompleted ? 1 : 0);
        command.Parameters.AddWithValue("@completedAt", task.CompletedAt.HasValue ? task.CompletedAt.Value.ToString("O") : DBNull.Value);

        var result = command.ExecuteScalar();
        task.Id = Convert.ToInt32(result);
        return task;
    }

    public void MarkTaskComplete(int taskId)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Tasks
            SET IsCompleted = 1,
                CompletedAt = @completedAt
            WHERE Id = @id;";

        command.Parameters.AddWithValue("@id", taskId);
        command.Parameters.AddWithValue("@completedAt", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
    }

    public List<TaskItem> GetActiveTasks()
    {
        return GetTasks(false);
    }

    public List<TaskItem> GetHistoryTasks()
    {
        return GetTasks(true);
    }

    private List<TaskItem> GetTasks(bool completedOnly)
    {
        var results = new List<TaskItem>();

        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Title, Description, DueDate, Priority, IsCompleted, CompletedAt
            FROM Tasks
            WHERE IsCompleted = @isCompleted
            ORDER BY DueDate ASC, Id ASC;";

        command.Parameters.AddWithValue("@isCompleted", completedOnly ? 1 : 0);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new TaskItem
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                DueDate = DateTime.Parse(reader.GetString(3)),
                Priority = reader.GetString(4),
                IsCompleted = reader.GetInt32(5) == 1,
                CompletedAt = reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6))
            });
        }

        return results;
    }
}
