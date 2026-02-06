using Microsoft.Data.Sqlite;
using System;

string connectionString = @"Data Source=c:\Users\john.monday\Documents\JonMondayGit\inv\InvServer.Api\inventory.db";

using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();
    var command = connection.CreateCommand();

    Console.WriteLine("--- REQ-10 Full Task History ---");
    command.CommandText = @"
        SELECT wt.WorkflowTaskId, s.Name as StepName, ts.Code as TaskStatus, wt.CreatedAt, wt.CompletedAt
        FROM WORKFLOW_TASK wt
        JOIN INVENTORY_REQUEST ir ON wt.WorkflowInstanceId = ir.WorkflowInstanceId
        JOIN WORKFLOW_STEP s ON wt.WorkflowStepId = s.WorkflowStepId
        JOIN WORKFLOW_TASK_STATUS ts ON wt.WorkflowTaskStatusId = ts.WorkflowTaskStatusId
        WHERE ir.RequestNo = 'REQ-10' OR ir.RequestId = 10
        ORDER BY wt.WorkflowTaskId ASC;";
    
    using (var reader = command.ExecuteReader()) {
        while (reader.Read()) {
            Console.WriteLine($"TID {reader["WorkflowTaskId"]} | Step: {reader["StepName"]} | Status: {reader["TaskStatus"]} | Created: {reader["CreatedAt"]}");
        }
    }

    Console.WriteLine("\n--- Assignees for REQ-10 ---");
    command.CommandText = @"
        SELECT wt.WorkflowTaskId, u.DisplayName, asg.Code as AssigneeStatus
        FROM WORKFLOW_TASK wt
        JOIN INVENTORY_REQUEST ir ON wt.WorkflowInstanceId = ir.WorkflowInstanceId
        JOIN WORKFLOW_TASK_ASSIGNEE wta ON wt.WorkflowTaskId = wta.WorkflowTaskId
        JOIN ""USER"" u ON wta.UserId = u.UserId
        JOIN WORKFLOW_TASK_ASSIGNEE_STATUS asg ON wta.AssigneeStatusId = asg.AssigneeStatusId
        WHERE ir.RequestNo = 'REQ-10' OR ir.RequestId = 10;";
    using (var reader = command.ExecuteReader()) {
        while (reader.Read()) Console.WriteLine($"Task {reader["WorkflowTaskId"]}: {reader["DisplayName"]} ({reader["AssigneeStatus"]})");
    }
}
