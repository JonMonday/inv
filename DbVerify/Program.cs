using Microsoft.Data.Sqlite;
using System;

string connectionString = @"Data Source=c:\Users\john.monday\Documents\JonMondayGit\inv\InvServer.Api\inventory.db";

using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();
    var command = connection.CreateCommand();

    command.CommandText = @"
        SELECT ir.RequestNo, wi.WorkflowTemplateId, t.Name as TemplateName, t.Code as TemplateCode
        FROM INVENTORY_REQUEST ir
        JOIN WORKFLOW_INSTANCE wi ON ir.WorkflowInstanceId = wi.WorkflowInstanceId
        JOIN WORKFLOW_TEMPLATE t ON wi.WorkflowTemplateId = t.WorkflowTemplateId
        WHERE ir.RequestNo = 'REQ-8';";
    
    using (var reader = command.ExecuteReader()) {
        if (reader.Read()) {
            Console.WriteLine($"Req: {reader["RequestNo"]} | Template: {reader["TemplateName"]} ({reader["TemplateCode"]}) | Id: {reader["WorkflowTemplateId"]}");
            
            var templateId = reader["WorkflowTemplateId"];
            var stepCommand = connection.CreateCommand();
            stepCommand.CommandText = $"SELECT WorkflowStepId, StepKey, Name, SequenceNo FROM WORKFLOW_STEP WHERE WorkflowTemplateId = {templateId} ORDER BY SequenceNo;";
            using (var stepReader = stepCommand.ExecuteReader()) {
                Console.WriteLine("\n--- Steps for this Template ---");
                Console.WriteLine($"{"Id",-5} | {"Key",-15} | {"Name",-20} | {"Seq"}");
                while (stepReader.Read()) {
                    Console.WriteLine($"{stepReader["WorkflowStepId"],-5} | {stepReader["StepKey"],-15} | {stepReader["Name"],-20} | {stepReader["SequenceNo"]}");
                }
            }
        }
    }
}
