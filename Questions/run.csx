#r "System.Configuration"
#load "../QuestionEntity.csx"
#load "../ResultObject.csx"

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

public static async Task<HttpResponseMessage> Run(HttpRequestData req, FunctionContext context)
{
    var log = context.GetLogger("QuestionsFunction");
    if (req.Method == "GET")
        return await Get(req, log);
    else if (req.Method == "POST")
        return await Post(req, log);
    else
        return req.CreateResponse(HttpStatusCode.InternalServerError, new { error = "Not implemented." });
}

private static async Task<HttpResponseMessage> Get(HttpRequestData req, ILogger log)
{
    string timeText = req.Query["time"];
    dynamic data = await req.ReadFromJsonAsync<object>();
    timeText = timeText ?? data?.id;

    if (timeText == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass time on the query string or in the request body." });

    if (!DateTime.TryParse(timeText, out DateTime time))
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Parse time failed." });

    var question = QuestionEntity.GetEntity(time, log);
    if (question == null)
        return req.CreateResponse(HttpStatusCode.InternalServerError, new { error = "This is a bug, maybe..." });

    return req.CreateResponse(HttpStatusCode.OK, new
    {
        id = question.RowKey,
        sentence = question.Sentence,
        total = question.ResultCount,
        correct = question.CorrectCount,
        url = $"https://speechengfunction.blob.core.windows.net/speechs/{question.RowKey}.wav",
        time = question.Timestamp
    });
}

private static async Task<HttpResponseMessage> Post(HttpRequestData req, ILogger log)
{
    string id = req.Query["id"];
    string sentence = req.Query["sentence"];
    dynamic data = await req.ReadFromJsonAsync<object>();
    id = id ?? data?.id;
    sentence = sentence ?? data?.sentence;

    if (id == null || sentence == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass a id and sentence on the query string or in the request body." });

    var question = QuestionEntity.GetEntity(id);
    if (question == null)
        return req.CreateResponse(HttpStatusCode.InternalServerError, new { error = "This is a bug, maybe..." });

    question.ResultCount += 1;
    var cos = calculate(question.Sentence, sentence, log);
    log.LogInformation($"cos:{cos}");
    var perfect = double.Parse(Environment.GetEnvironmentVariable("BORDER_PERFECT"));
    var good = double.Parse(Environment.GetEnvironmentVariable("BORDER_GOOD"));
    string comment;
    if (cos > perfect)
    {
        question.CorrectCount += 1;
        comment = "PERFECT!!";
    }
    else if (cos > good)
    {
        question.CorrectCount += 1;
        comment = "GOOD!";
    }
    else
        comment = "OOPS...";
    question.Replace();
    return req.CreateResponse(HttpStatusCode.OK, new { cos, comment });
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
    MeCabParam param = new MeCabParam { DicDir = Environment.GetEnvironmentVariable("MeCabDicDir") };
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
    return uniques.Select(word => elements.Contains(word) ? 1 : 0).ToArray();
}

private static int dot(int[] i1, int[] i2, int length)
{
    return Enumerable.Range(0, length).Sum(i => i1[i] * i2[i]);
}