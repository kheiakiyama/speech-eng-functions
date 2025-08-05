#r "System.Configuration"

using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Linq;
using System.Collections.Generic;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    if (req.Method == HttpMethod.Get)
        return await Get(req, log);
    else
        return req.CreateResponse(HttpStatusCode.InternalServerError, "Not implimentation.");
}

private static async Task<HttpResponseMessage> Get(HttpRequestMessage req, TraceWriter log)
{
    return req.CreateResponse(HttpStatusCode.OK, Environment.GetEnvironmentVariable("BingSpeechKey"));
}