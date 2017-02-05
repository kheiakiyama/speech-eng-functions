#r "System.Configuration"
#r "System.Collections"
#r "System.Xml"
#r "System.Xml.Linq"
#load "../QuestionEntity.csx"
#load "../Authentication.csx"
#load "../Synthesize.csx"

using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task Run(string queueItem, 
    DateTimeOffset expirationTime, 
    DateTimeOffset insertionTime, 
    DateTimeOffset nextVisibleTime,
    string queueTrigger,
    string id,
    string popReceipt,
    int dequeueCount,
    QuestionEntity entity,
    Stream outBlob,
    TraceWriter log)
{
    speechBinary = null;
    log.Info($"C# Queue trigger function processed: {queueItem}\n" +
    $"queueTrigger={queueTrigger}\n" +
    $"expirationTime={expirationTime}\n" +
    $"insertionTime={insertionTime}\n" +
    $"nextVisibleTime={nextVisibleTime}\n" +
    $"id={id}\n" +
    $"popReceipt={popReceipt}\n" + 
    $"dequeueCount={dequeueCount}\n" + 
    $"rowKey={entity.RowKey}");
    
    string accessToken;
    log.Info(ConfigurationManager.AppSettings["BingSpeechKey"]);
    Authentication auth = new Authentication(ConfigurationManager.AppSettings["BingSpeechKey"]);
    try
    {
        accessToken = auth.GetAccessToken();
        log.Info($"Token: {accessToken}");
    }
    catch (Exception ex)
    {
        log.Info("Failed authentication.");
        log.Info(ex.ToString());
        log.Info(ex.Message);
        outBlob = null;
        return;
    }
    string requestUri = "https://speech.platform.bing.com/synthesize";
    var cortana = new Synthesize(new Synthesize.InputOptions()
    {
        RequestUri = new Uri(requestUri),
        Text = entity.Sentence,
        VoiceType = Gender.Female,
        Locale = "en-US",
        VoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)",
        OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
        AuthorizationToken = "Bearer " + accessToken,
    });

    cortana.OnAudioAvailable += PlayAudio;
    cortana.OnError += ErrorHandler;
    await cortana.Speak(CancellationToken.None);
    using (MemoryStream ms = new MemoryStream())
    {
        speechBinary.CopyTo(ms);
        var byteArray = ms.ToArray();
        await outBlob.WriteAsync(byteArray, 0, byteArray.Length);
    }
}

private static Stream speechBinary = null;

private static void ErrorHandler(object sender, GenericEventArgs<Exception> e)
{
    Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
}

private static void PlayAudio(object sender, GenericEventArgs<Stream> args)
{
    speechBinary = args.EventData;
}