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

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    if (req.Method == HttpMethod.Get)
        return await Get(req, log);
    else if (req.Method == HttpMethod.Post)
        return await Post(req, log);
    else
        return req.CreateResponse(HttpStatusCode.InternalServerError, "Not implimentation.");
}

private static async Task<HttpResponseMessage> Get(HttpRequestMessage req, TraceWriter log)
{
    string timeText = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "time", true) == 0)
        .Value;

    dynamic data = await req.Content.ReadAsAsync<object>();
    timeText = timeText ?? data?.id;
    if (timeText == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass time on the query string or in the request body.");
    
    DateTime time;
    if (!DateTime.TryParse(timeText, out time))
        return req.CreateResponse(HttpStatusCode.BadRequest, "Parse time failed.");

    var question = QuestionEntity.GetEntity(time, log);
    if (question == null)
        return req.CreateResponse(HttpStatusCode.InternalServerError, "This is a bug, maybe..");
    
    return req.CreateResponse(HttpStatusCode.OK, new {
        id = question.RowKey,
        sentence = question.Sentence,
        total = question.ResultCount,
        correct = question.CorrectCount,
        url = $"https://speechengfunction.blob.core.windows.net/speechs/{question.RowKey}.wav",
        time = question.Timestamp
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
    
    var question = QuestionEntity.GetEntity(id);
    if (question == null)
        return req.CreateResponse(HttpStatusCode.InternalServerError, "This is a bug, maybe..");
    
    question.ResultCount = question.ResultCount + 1;
    var cos = calculate(question.Sentence, sentence, log);
    log.Info($"{cos}");
    if (question.Sentence == sentence)
        question.CorrectCount = question.CorrectCount + 1;
    question.Replace();
    return req.CreateResponse(HttpStatusCode.OK, new {
    });
}

private static double calculate(string text1, string text2, TraceWriter log) 
{
    var ar1 = breakUp(text1, log);
    var ar2 = breakUp(text2, log);
    var uniques = ar1.Concat(ar2).Distinct().ToArray();
    var flgs1 = make_flags(uniques, ar1.Distinct().ToArray());
    var flgs2 = make_flags(uniques, ar2.Distinct().ToArray());
    var vct1 = new Vector<int>(flgs1);
    var vct2 = new Vector<int>(flgs2);

    return dot(vct1, vct2, uniques.Length) / (double)uniques.Length;
}

private static string[] breakUp(string text, TraceWriter log)
{
    MeCabParam param = new MeCabParam();
    param.DicDir = ConfigurationManager.AppSettings["MeCabDicDir"];
    var mecab = MeCabTagger.Create(param);
    var node = mecab.ParseToNode(text);
    var ret = new List<string>();
    while (node != null)
    {
        ret.Add(node.Surface);
        log.Info(node.Surface + " - " + node.Feature);
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

private static int dot(Vector<int> i1, Vector<int> i2, int length)
{
    var ret = 0;
    for (var i = 0; i < length; i++)
        ret += i1[i] * i2[i];
    return ret;
}