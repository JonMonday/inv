using Microsoft.Data.Sqlite;
using System;

string connectionString = @"Data Source=c:\Users\john.monday\Documents\JonMondayGit\inv\InvServer.Api\inventory.db";

using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();
    var command = connection.CreateCommand();

    command.CommandText = "SELECT Notes FROM WORKFLOW_TASK_ACTION WHERE WorkflowTaskId = 1;";
    using (var reader = command.ExecuteReader()) {
        if (reader.Read()) Console.WriteLine($"Notes for Task 1: {reader["Notes"]}");
    }
}
