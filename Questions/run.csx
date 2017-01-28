#r "System.Configuration"
#load "../IssueEntity.csx"

using System.Net;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table; 

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("ResultCount function processed a request.");

    // parse query parameter
    string id = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "id", true) == 0)
        .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set id to query string or body data
    id = id ?? data?.id;

    if (id == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a id on the query string or in the request body");
    
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["speechengfunction_STORAGE"]);
    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
    CloudTable table = tableClient.GetTableReference("sentences");
    TableOperation retrieveOperation = TableOperation.Retrieve<IssueEntity>("speech-eng", id);

    // Execute the retrieve operation.
    TableResult retrievedResult = table.Execute(retrieveOperation);

    // Print the phone number of the result.
    if (retrievedResult.Result == null)
        return req.CreateResponse(HttpStatusCode.InternalServerError, "This is a bug, maybe..");
    

    var issue = ((IssueEntity)retrievedResult.Result);
    log.Info(issue.RowKey);
    return req.CreateResponse(HttpStatusCode.OK, new { 
        sentence = issue.Sentence,
        total = issue.ResultCount,
        correct = issue.CorrectCount
    });
}