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

public static void Run(string queueItem, 
    DateTimeOffset expirationTime, 
    DateTimeOffset insertionTime, 
    DateTimeOffset nextVisibleTime,
    string queueTrigger,
    string id,
    string popReceipt,
    int dequeueCount,
    QuestionEntity entity, 
    out string outputBlob, 
    TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {queueItem}\n" +
    $"queueTrigger={queueTrigger}\n" +
    $"expirationTime={expirationTime}\n" +
    $"insertionTime={insertionTime}\n" +
    $"nextVisibleTime={nextVisibleTime}\n" +
    $"id={id}\n" +
    $"popReceipt={popReceipt}\n" + 
    $"dequeueCount={dequeueCount}" + 
    $"rowKey={entity.RowKey}");
    outputBlob = queueItem;
}