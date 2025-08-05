#r "System.Configuration"
#load "../ResultObject.csx"

using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public static async Task<HttpResponseData> Run(HttpRequestData req, FunctionContext context)
{
    var log = context.GetLogger("Run");
    if (req.Method == "GET")
        return await Get(req, log);
    else
    {
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        await response.WriteAsJsonAsync(new ErrorEntity(){ error = "Not implemented." });
        return response;
    }
}

private static async Task<HttpResponseData> Get(HttpRequestData req, ILogger log)
{
    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(new OxfordEntity(){ key = Environment.GetEnvironmentVariable("BingSpeechKey") });
    return response;
}