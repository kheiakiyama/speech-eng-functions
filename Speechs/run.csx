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
    Authentication auth = new Authentication(ConfigurationManager.AppSettings["BingSpeechKey"]);
    try
    {
        accessToken = auth.GetAccessToken();
        Console.WriteLine("Token: {0}\n", accessToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed authentication.");
        Console.WriteLine(ex.ToString());
        Console.WriteLine(ex.Message);
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
        OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
        AuthorizationToken = "Bearer " + accessToken,
    });

    cortana.OnAudioAvailable += PlayAudio;
    cortana.OnError += ErrorHandler;
    await cortana.Speak(CancellationToken.None);
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