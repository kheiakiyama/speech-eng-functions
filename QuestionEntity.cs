using System;
using Aspire.Hosting;
using Azure;
using Azure.Data.Tables;
using Aspire.Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Extensions.Logging;
using SpeechEng.Functions;

public class QuestionEntity : Azure.Data.Tables.ITableEntity
{
    public QuestionEntity(ulong id)
    {
        this.PartitionKey = "speech-eng";
        this.RowKey = id.ToString();
        ResultCount = 0;
        CorrectCount = 0;
    }

    public QuestionEntity() { }

    public string Sentence { get; set; }
    public int ResultCount { get; set; }
    public int CorrectCount { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public static QuestionEntity GetEntity(string id)
    {
        var table = GetTable();
        var entity = table.GetEntityIfExists<QuestionEntity>("speech-eng", id);
        return entity.Value;
    }

    public static QuestionEntity GetEntity(DateTime time, ILogger<Questions> log)
    {
        log.LogInformation($"time:{time.ToString()}");
        var table = GetTable();
        var res = table.Query<QuestionEntity>()
            .Where(e => e.PartitionKey == "speech-eng" && e.Timestamp < time)
            .Take(20)
            .ToArray();
        var entity = RandomChoise(res);
        if (entity != null)
        {
            log.LogInformation($"EntityTime:{entity.Timestamp.ToString()}");
            return entity;
        }
        res = table.Query<QuestionEntity>()
            .Where(e => e.PartitionKey == "speech-eng")
            .Take(20)
            .ToArray();
        entity = RandomChoise(res);
        if (entity != null)
        {
            log.LogInformation($"Entity2Time:{entity.Timestamp.ToString()}");
            return entity;
        }
        return null;
    }

    private static QuestionEntity RandomChoise(QuestionEntity[] entities)
    {
        var random = new Random();
        var index = random.Next(entities.Length);
        return index < entities.Length ? entities[index] : null;
    }

    private static TableClient tmpTable = null;

    private static TableClient GetTable()
    {
        if (tmpTable != null)
            return tmpTable;

        var serviceClient = new TableServiceClient(Environment.GetEnvironmentVariable("speechengfunction_STORAGE"));
        tmpTable = serviceClient.GetTableClient("sentences");
        return tmpTable;
    }

    public void Replace()
    {
        var table = GetTable();
        table.UpdateEntity(this, ETag.All, TableUpdateMode.Replace);
    }

    // public void Insert()
    // {
    //     CloudTable table = GetTable();
    //     TableOperation insertOperation = TableOperation.Insert(this);
    //     table.Execute(insertOperation);
    // }
}