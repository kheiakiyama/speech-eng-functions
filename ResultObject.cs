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
    public string error { get; set; }
}
public class OxfordEntity
{
    public string key { get; set; } = string.Empty;
}
public class QuestionGetEntity
{
    public string id { get; set; }
    public string sentence { get; set; }
    public int total { get; set; }
    public int correct { get; set; }
    public string url { get; set; }
    public DateTimeOffset? time { get; set; }
}
public class QuestionPostEntity
{
    public double cors { get; set; }
    public string comment { get; set; }
}