#r "System.Configuration"
#load "../QuestionEntity.csx"

using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NMeCab;
using Microsoft.Extensions.Logging;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ILogger log)
{
    if (req.Method == HttpMethod.Get)
        return await Get(req, log);
    else if (req.Method == HttpMethod.Post)
        return await Post(req, log);
    else
        return req.CreateResponse<ErrorEntity>(HttpStatusCode.InternalServerError, new ErrorEntity(){ error = "Not implemented." });
}

private static async Task<HttpResponseMessage> Get(HttpRequestMessage req, ILogger log)
{
    string timeText = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "time", true) == 0)
        .Value;

    dynamic data = await req.Content.ReadAsAsync<object>();
    timeText = timeText ?? data?.id;
    if (timeText == null)
        return req.CreateResponse<ErrorEntity>(HttpStatusCode.BadRequest, new ErrorEntity(){ error = "Please pass time on the query string or in the request body." });
    
    DateTime time;
    if (!DateTime.TryParse(timeText, out time))
        return req.CreateResponse<ErrorEntity>(HttpStatusCode.BadRequest, new ErrorEntity(){ error = "Parse time failed." });

    var question = QuestionEntity.GetEntity(time, log);
    if (question == null)
        return req.CreateResponse<ErrorEntity>(HttpStatusCode.InternalServerError, new ErrorEntity(){ error = "This is a bug, maybe..." });
    
    return req.CreateResponse<QuestionGetEntity>(HttpStatusCode.OK, new QuestionGetEntity(){
        id = question.RowKey,
        sentence = question.Sentence,
        total = question.ResultCount,
        correct = question.CorrectCount,
        url = $"https://speechengfunction.blob.core.windows.net/speechs/{question.RowKey}.wav",
        time = question.Timestamp
    });
}

private static async Task<HttpResponseMessage> Post(HttpRequestMessage req, ILogger log)
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
        return req.CreateResponse<ErrorEntity>(HttpStatusCode.BadRequest, new ErrorEntity(){ error = "Please pass a id and sentence on the query string or in the request body" });
    
    var question = QuestionEntity.GetEntity(id);
    if (question == null)
        return req.CreateResponse<ErrorEntity>(HttpStatusCode.InternalServerError, new ErrorEntity(){ error = "This is a bug, maybe..." });
    
    question.ResultCount = question.ResultCount + 1;
    var cos = calculate(question.Sentence, sentence, log);
    log.LogInformation($"cos:{cos}");
    var perfect = double.Parse(Environment.GetEnvironmentVariable("BORDER_PERFECT"));
    var good = double.Parse(Environment.GetEnvironmentVariable("BORDER_GOOD"));
    string comment;
    if (cos > perfect)
    {
        question.CorrectCount = question.CorrectCount + 1;
        comment = "PERFECT!!";
    }    
    else if (cos > good)
    {
        question.CorrectCount = question.CorrectCount + 1;
        comment = "GOOD!";
    }
    else
        comment = "OOPS...";
    question.Replace();
    return req.CreateResponse<QuestionPostEntity>(HttpStatusCode.OK, new QuestionPostEntity(){
        cos = cos,
        comment = comment
    });
}

private static double calculate(string text1, string text2, ILogger log) 
{
    var ar1 = breakUp(text1, log);
    var ar2 = breakUp(text2, log);
    var uniques = ar1.Concat(ar2).Distinct().ToArray();
    var flgs1 = make_flags(uniques, ar1.Distinct().ToArray());
    var flgs2 = make_flags(uniques, ar2.Distinct().ToArray());
    return dot(flgs1, flgs2, uniques.Length) / (double)uniques.Length;
}

private static string[] breakUp(string text, ILogger log)
{
    MeCabParam param = new MeCabParam();
    param.DicDir = ConfigurationManager.AppSettings["MeCabDicDir"];
    var mecab = MeCabTagger.Create(param);
    var node = mecab.ParseToNode(text);
    var ret = new List<string>();
    while (node != null)
    {
        ret.Add(node.Surface);
        log.LogInformation(node.Surface);
        node = node.Next;
    }
    return ret.ToArray();
}

private static int[] make_flags(string[] uniques, string[] elements)
{
    var ret = new List<int>();
    foreach (var word in uniques)
    {
        ret.Add(elements.Contains(word) ? 1 : 0);
    }
    return ret.ToArray();
}

private static int dot(int[] i1, int[] i2, int length)
{
    var ret = 0;
    for (var i = 0; i < length; i++)
        ret += i1[i] * i2[i];
    return ret;
}