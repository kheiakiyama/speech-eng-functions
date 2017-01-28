#r "System.Configuration"
#load "../QuestionEntity.csx"

using System.Net;
using System.Net.Http;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table; 

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("ResultCount function processed a request.");
    if (req.Method == HttpMethod.Get)
        return await Get(req, log);
    else
        return req.CreateResponse(HttpStatusCode.InternalServerError, "Not implimentation");
}

private static async Task<HttpResponseMessage> Get(HttpRequestMessage req, TraceWriter log)
{
    string id = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "id", true) == 0)
        .Value;

    dynamic data = await req.Content.ReadAsAsync<object>();
    id = id ?? data?.id;
    if (id == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a id on the query string or in the request body");
    
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["speechengfunction_STORAGE"]);
    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
    CloudTable table = tableClient.GetTableReference("sentences");
    TableOperation retrieveOperation = TableOperation.Retrieve<QuestionEntity>("speech-eng", id);
    TableResult retrievedResult = table.Execute(retrieveOperation);
    if (retrievedResult.Result == null)
        return req.CreateResponse(HttpStatusCode.InternalServerError, "This is a bug, maybe..");
    
    var issue = ((QuestionEntity)retrievedResult.Result);
    log.Info(issue.RowKey);
    return req.CreateResponse(HttpStatusCode.OK, new { 
        sentence = issue.Sentence,
        total = issue.ResultCount,
        correct = issue.CorrectCount
    });
}