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
    speechBinary = "";
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
        outputBlob = "";
        return;
    }
    string requestUri = "https://speech.platform.bing.com/synthesize";
    var cortana = new Synthesize(new Synthesize.InputOptions()
    {
        RequestUri = new Uri(requestUri),
        // Text to be spoken.
        Text = entity.Sentence,
        VoiceType = Gender.Female,
        // Refer to the documentation for complete list of supported locales.
        Locale = "en-US",
        // You can also customize the output voice. Refer to the documentation to view the different
        // voices that the TTS service can output.
        VoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)",
        // Service can return audio in different output format. 
        OutputFormat = AudioOutputFormat.Audio16khz64kbitrateMonoMp3,
        AuthorizationToken = "Bearer " + accessToken,
    });

    cortana.OnAudioAvailable += PlayAudio;
    cortana.OnError += ErrorHandler;
    cortana.Speak(CancellationToken.None).Wait();
    log.Info(speechBinary.Length.ToString());
    outputBlob = speechBinary;
}

private static string speechBinary = "";

private static void ErrorHandler(object sender, GenericEventArgs<Exception> e)
{
    Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
}

private static void PlayAudio(object sender, GenericEventArgs<Stream> args)
{
    using (StreamReader sr = new StreamReader(args.EventData))
    {
        speechBinary = sr.ReadToEnd();
    }
    args.EventData.Dispose();
}