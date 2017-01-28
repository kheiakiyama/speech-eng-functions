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
    else if (req.Method == HttpMethod.Post)
        return await Post(req, log);
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
    
    var question = ((QuestionEntity)retrievedResult.Result);
    log.Info(question.RowKey);
    return req.CreateResponse(HttpStatusCode.OK, new { 
        sentence = question.Sentence,
        total = question.ResultCount,
        correct = question.CorrectCount
    });
}

private static async Task<HttpResponseMessage> Post(HttpRequestMessage req, TraceWriter log)
{
    string id = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "id", true) == 0)
        .Value;
    string sentence = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "sentence", true) == 0)
        .Value;

    dynamic data = await req.Content.ReadAsAsync<object>();
    id = id ?? data?.id;
    sentence = sentence ?? data?.sentence;
    if (id == null || sentence == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a id and sentence on the query string or in the request body");
    
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["speechengfunction_STORAGE"]);
    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
    CloudTable table = tableClient.GetTableReference("sentences");
    TableOperation retrieveOperation = TableOperation.Retrieve<QuestionEntity>("speech-eng", id);
    TableResult retrievedResult = table.Execute(retrieveOperation);
    if (retrievedResult.Result == null)
        return req.CreateResponse(HttpStatusCode.InternalServerError, "This is a bug, maybe..");
    
    var question = ((QuestionEntity)retrievedResult.Result);
    log.Info(question.RowKey);
    question.ResultCount = question.ResultCount + 1;
    if (question.Sentence == sentence)
        question.CorrectCount = question.CorrectCount + 1;
    TableOperation updateOperation = TableOperation.Replace(question);
    table.Execute(updateOperation);
    return req.CreateResponse(HttpStatusCode.OK, new {
    });
}