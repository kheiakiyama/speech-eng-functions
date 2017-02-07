using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public class QuestionEntity : TableEntity
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

    public static QuestionEntity GetEntity(string id)
    {
        CloudTable table = GetTable();
        TableOperation retrieveOperation = TableOperation.Retrieve<QuestionEntity>("speech-eng", id);
        TableResult retrievedResult = table.Execute(retrieveOperation);
        return (QuestionEntity)retrievedResult.Result;
    }

    public static QuestionEntity GetEntity(DateTime time, TraceWriter log)
    {
        log.Info($"time:{time.ToString()}");
        CloudTable table = GetTable();
        TableQuery<QuestionEntity> query = new TableQuery<QuestionEntity>().Where(
            TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "speech-eng"),
                TableOperators.And,
                TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThan, time)));
        query.TakeCount = 20;
        var entity = RandomChoise(table.ExecuteQuery(query).ToArray());
        if (entity != null)
        {
            log.Info($"EntityTime:{entity.Timestamp.ToString()}");
            return (QuestionEntity)entity;
        }

        query = new TableQuery<QuestionEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "speech-eng"));
        query.TakeCount = 20;
        entity = RandomChoise(table.ExecuteQuery(query).ToArray());
        if (entity != null)
        {
            log.Info($"Entity2Time:{entity.Timestamp.ToString()}");
            return (QuestionEntity)entity;
        }

        return null;
    }

    private static QuestionEntity RandomChoise(QuestionEntity[] entities)
    {
        var random = new Random();
        var index = random.Next(entities.Length);
        return entities[index];
    }

    private static CloudTable tmpTable = null;

    private static CloudTable GetTable()
    {
        if (tmpTable != null)
            return tmpTable;
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["speechengfunction_STORAGE"]);
        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        return tmpTable = tableClient.GetTableReference("sentences");
    }

    public void Replace()
    {
        CloudTable table = GetTable();
        TableOperation updateOperation = TableOperation.Replace(this);
        table.Execute(updateOperation);
    }

    public void Insert()
    {
        CloudTable table = GetTable();
        TableOperation insertOperation = TableOperation.Insert(this);
        table.Execute(insertOperation);
    }
}