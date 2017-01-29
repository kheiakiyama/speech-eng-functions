#r "System.Configuration"
#load "../QuestionEntity.csx"

using System.Net;
using System.Net.Http;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using CommonMark;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("ResultCount function processed a request.");
    if (req.Method == HttpMethod.Post)
        return await Post(req, log);
    else
        return req.CreateResponse(HttpStatusCode.InternalServerError, "Not implimentation");
}

private static async Task<HttpResponseMessage> Post(HttpRequestMessage req, TraceWriter log)
{
    string url = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "url", true) == 0)
        .Value;

    dynamic data = await req.Content.ReadAsAsync<object>();
    url = url ?? data?.url;
    if (url == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a url and sentence on the query string or in the request body");
    
    using (HttpClient client = new HttpClient())
    {
        try
        {
           var content = await client.GetStringAsync(new Uri(url));
           var sentences = DivideSentence(content, log);
           foreach (var item in sentences)
           {
               log.Info(item);
           }
        }
        catch (HttpRequestException e)
        {

        }
    }
    return req.CreateResponse(HttpStatusCode.OK, new {
    });
}

private static string[] DivideSentence(string content, TraceWriter log)
{
    var document = CommonMarkConverter.Convert(content);
    return document
        .AsEnumerable()
        .Where(q => q.Block != null || q.Inline != null)
        .Select(q => q.Block != null ? q.Block.StringContent.ToString() : q.Inline.LiteralContent);
}