using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MeCab;

namespace SpeechEng.Functions;

public class PostAnswerDTO
{
    public string? Id { get; set; }
    public string? Sentence { get; set; }
}

public class Answer
{
    private readonly ILogger<Answer> _logger;

    public Answer(ILogger<Answer> logger)
    {
        _logger = logger;
    }

    [Function("Answers")]
    public IActionResult PostAnswer([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("You can get additional information about the request such as:");
        _logger.LogInformation($" length : {req.ContentLength}");
        _logger.LogInformation($" type   : {req.ContentType}");

        var dto = new PostAnswerDTO();
        dto.Id = req.Form["id"];
        dto.Sentence = req.Form["sentence"];

        _logger.LogInformation($"dto:{dto.Id},{dto.Sentence}");
        if (dto.Id == null || dto.Sentence == null)
            return new BadRequestObjectResult(new { error = "Please pass a id and sentence on the query string or in the request body." });

        var question = QuestionEntity.GetEntity(dto.Id);
        _logger.LogInformation($"question:{question?.RowKey},{question?.Sentence}");
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
        var cos = calculate(question.Sentence, dto.Sentence);
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
        var parameter = new MeCabParam(Environment.GetEnvironmentVariable("MeCabDicDir"));
        using (var tagger = MeCabTagger.Create(parameter))
        {
            return tagger.ParseToNodes(text).Select(q => q.Surface).ToArray();
        }
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