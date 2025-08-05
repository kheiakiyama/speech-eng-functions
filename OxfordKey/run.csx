#r "System.Configuration"

using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Linq;
using System.Collections.Generic;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ILogger log)
{
    if (req.Method == HttpMethod.Get)
        return await Get(req, log);
    else
        return req.CreateResponse(HttpStatusCode.InternalServerError, new { error = "Not implemented." });
}

private static async Task<HttpResponseMessage> Get(HttpRequestMessage req, ILogger log)
{
    return req.CreateResponse(HttpStatusCode.OK, new { key = Environment.GetEnvironmentVariable("BingSpeechKey") });
}