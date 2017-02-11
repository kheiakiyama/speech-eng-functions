#r "System.Configuration"

using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    if (req.Method == HttpMethod.Get)
        return await Get(req, log);
    else
        return req.CreateResponse(HttpStatusCode.InternalServerError, "Not implimentation.");
}

private static async Task<HttpResponseMessage> Get(HttpRequestMessage req, TraceWriter log)
{
    return req.CreateResponse(HttpStatusCode.OK, ConfigurationManager.AppSettings["BingSpeechKey"]);
}