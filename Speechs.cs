using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;

namespace SpeechEng.Functions;

public class Speechs
{
    private readonly ILogger<Speechs> _logger;

    public Speechs(ILogger<Speechs> logger)
    {
        _logger = logger;
    }

    [Function("Speechs")]
    [BlobOutput("speechs/{queueTrigger}.wav")]
    public async Task<byte[]> Run(
        [QueueTrigger("speech")] string queueItem,
        [TableInput("sentences", "speech-eng", "{queueTrigger}")] QuestionEntity entity)
    {
        _logger.LogInformation($"Queue trigger function processed: {queueItem}\n" +
        $"rowKey={entity.RowKey}");

        _logger.LogInformation(Environment.GetEnvironmentVariable("BingSpeechKey"));
        var speechConfig = SpeechConfig.FromSubscription(Environment.GetEnvironmentVariable("BingSpeechKey"), "en-US");
        using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);

        var result = await speechSynthesizer.SpeakTextAsync(entity.Sentence);
        using var audioStream = AudioDataStream.FromResult(result);
        var memoryStream = new MemoryStream();

        byte[] buffer = new byte[4096];
        uint bytesRead;
        while ((bytesRead = audioStream.ReadData(buffer)) > 0)
        {
            memoryStream.Write(buffer, 0, (int)bytesRead);
        }
        return memoryStream.ToArray();
    }

    private Stream ConvertToWaveFormat(MemoryStream rawAudioStream)
    {
        rawAudioStream.Position = 0;
        using var reader = new BinaryReader(rawAudioStream);
        using var wavStream = new MemoryStream();

        using var writer = new BinaryWriter(wavStream);
        int sampleRate = 16000;
        int bitsPerSample = 16;
        int channels = 1;

        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        int dataSize = (int)rawAudioStream.Length;

        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * (bitsPerSample / 8)));
        writer.Write((short)bitsPerSample);
        writer.Write("data".ToCharArray());
        writer.Write(dataSize);
        rawAudioStream.Position = 0;
        rawAudioStream.CopyTo(wavStream);
        wavStream.Position = 0;
        return new MemoryStream(wavStream.ToArray());
    }
}
