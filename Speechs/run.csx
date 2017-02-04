#r "System.Configuration"
#r "System.Collections"
#r "System.Linq.Expressions"
#load "../QuestionEntity.csx"

using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public static void Run(string queueItem, QuestionEntity entity, out string outputBlob, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {queueItem}");
    outputBlob = queueItem;
}