using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SpeechEng.Functions;

public class Questions
{
    private readonly ILogger<Questions> _logger;

    public Questions(ILogger<Questions> logger)
    {
        _logger = logger;
    }

    [Function("Questions")]
    public Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        if (req.Method == "GET")
        {
            return Get(req);
        }
        else
        {
            return Post(req);
        }
    }

private async Task<IActionResult> Get(HttpRequest req)
{
    string? timeText = req.Query["time"].FirstOrDefault();
    dynamic? data = await req.ReadFromJsonAsync<object>();
    timeText = timeText ?? (data?.id as string);

    if (timeText == null)
        return new BadRequestObjectResult(new { error = "Please pass time on the query string or in the request body." });

    if (!DateTime.TryParse(timeText, out DateTime time))
        return new BadRequestObjectResult(new { error = "Parse time failed." });

    var question = QuestionEntity.GetEntity(time, _logger);
    if (question == null)
    {        
        var customResponse = new
        {
            Code = 500,
            Message = "Internal Server Error",
            ErrorMessage = "This is a bug, maybe.."
        };
        return new ObjectResult(customResponse) 
        { 
            StatusCode = StatusCodes.Status500InternalServerError 
        };
    }

    return new ObjectResult(new
    {
        id = question.RowKey,
        sentence = question.Sentence,
        total = question.ResultCount,
        correct = question.CorrectCount,
        url = $"https://speechengfunction.blob.core.windows.net/speechs/{question.RowKey}.wav",
        time = question.Timestamp
    });
}

private async Task<IActionResult> Post(HttpRequest req)
{
    string? id = req.Query["id"].FirstOrDefault();
    string? sentence = req.Query["sentence"].FirstOrDefault();
    dynamic? data = await req.ReadFromJsonAsync<object>();
    id = id ?? data?.id;
    sentence = sentence ?? data?.sentence;

    if (id == null || sentence == null)
        return new BadRequestObjectResult(new { error = "Please pass a id and sentence on the query string or in the request body." });

    var question = QuestionEntity.GetEntity(id);
    if (question == null)
    {
        var customResponse = new
        {
            Code = 500,
            Message = "Internal Server Error",
            ErrorMessage = "This is a bug, maybe.."
        };
        return new ObjectResult(customResponse) 
        { 
            StatusCode = StatusCodes.Status500InternalServerError 
        };
    }
    question.ResultCount += 1;
    var cos = calculate(question.Sentence, sentence);
    _logger.LogInformation($"cos:{cos}");
    var perfectStr = Environment.GetEnvironmentVariable("BORDER_PERFECT");
    var goodStr = Environment.GetEnvironmentVariable("BORDER_GOOD");
    if (string.IsNullOrEmpty(perfectStr) || string.IsNullOrEmpty(goodStr))
    {
        return new ObjectResult(new
        {
            Code = 500,
            Message = "Internal Server Error",
            ErrorMessage = "Environment variable BORDER_PERFECT or BORDER_GOOD is not set."
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
    }
    var perfect = double.Parse(perfectStr);
    var good = double.Parse(goodStr);
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
    return new OkObjectResult(new { cos, comment });
}

private double calculate(string text1, string text2)
{
    var ar1 = breakUp(text1);
    var ar2 = breakUp(text2);
    var uniques = ar1.Concat(ar2).Distinct().ToArray();
    var flgs1 = make_flags(uniques, ar1.Distinct().ToArray());
    var flgs2 = make_flags(uniques, ar2.Distinct().ToArray());
    return dot(flgs1, flgs2, uniques.Length) / (double)uniques.Length;
}

    private string[] breakUp(string text)
    {
    throw new NotImplementedException();
    // using (var tagger = MeCabTagger.Create(Environment.GetEnvironmentVariable("MeCabDicDir")))
        // {
        //     var node = tagger.Parse(text);
        //     var ret = new List<string>();
        //     while (node != null)
        //     {
        //         ret.Add(node.Surface);
        //         log.LogInformation(node.Surface);
        //         node = node.Next;
        //     }
        //     return ret.ToArray();
        // }
    }

private static int[] make_flags(string[] uniques, string[] elements)
{
    return uniques.Select(word => elements.Contains(word) ? 1 : 0).ToArray();
}

private static int dot(int[] i1, int[] i2, int length)
{
    return Enumerable.Range(0, length).Sum(i => i1[i] * i2[i]);
}
}