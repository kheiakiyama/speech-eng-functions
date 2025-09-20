using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace SpeechEng.Functions;
public class ErrorEntity
{
    public string Error { get; set; } = string.Empty;
}
public class OxfordEntity
{
    public string Key { get; set; } = string.Empty;
}
public class QuestionGetEntity
{
    public string Id { get; set; } = string.Empty;
    public string Sentence { get; set; } = string.Empty;
    public int Total { get; set; } = 0;
    public int Correct { get; set; } = 0;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset? Time { get; set; }
}
public class QuestionPostEntity
{
    public double Cos { get; set; } = 0;
    public string Comment { get; set; } = string.Empty;
}