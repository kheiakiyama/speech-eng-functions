using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public class IssueEntity : TableEntity
{
    public IssueEntity(int id)
    {
        this.PartitionKey = "speech-eng";
        this.RowKey = id.ToString();
        ResultCount = 0;
        CorrectCount = 0;
    }

    public IssueEntity() { }

    public string Sentence { get; set; }
    public int ResultCount { get; set; }
    public int CorrectCount { get; set; }
}