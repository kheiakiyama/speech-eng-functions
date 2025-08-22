using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SpeechEng.Functions;

public class OxfordKey
{
    private readonly ILogger<OxfordKey> _logger;

    public OxfordKey(ILogger<OxfordKey> logger)
    {
        _logger = logger;
    }

    [Function("OxfordKey")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        return new OkObjectResult(new OxfordEntity
        {
            key = Environment.GetEnvironmentVariable("BingSpeechKey")
        });
    }
}