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

public static async Task<Stream> Run(string queueItem, 
    DateTimeOffset expirationTime, 
    DateTimeOffset insertionTime, 
    DateTimeOffset nextVisibleTime,
    string queueTrigger,
    string id,
    string popReceipt,
    int dequeueCount,
    QuestionEntity entity,
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
        return null;
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
    log.Info(speechBinary.Length.ToString());
    return speechBinary;
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