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
    public IActionResult GetRandomList([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        string? timeText = req.Query["time"].FirstOrDefault();
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
}