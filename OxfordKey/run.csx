#r "System.Configuration"
#load "../ResultObject.csx"

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
        return req.CreateResponse<ErrorEntity>(HttpStatusCode.InternalServerError, new ErrorEntity(){ error = "Not implemented." });
}

private static async Task<HttpResponseMessage> Get(HttpRequestMessage req, ILogger log)
{
    return req.CreateResponse<OxfordEntity>(HttpStatusCode.OK, new OxfordEntity(){ key = Environment.GetEnvironmentVariable("BingSpeechKey") });
}