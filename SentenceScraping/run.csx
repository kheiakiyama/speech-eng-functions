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
using LinqToTwitter;

public static async Task Run(
    TimerInfo myTimer, 
    IAsyncCollector<string> queueBinding, 
    IAsyncCollector<QuestionEntity> tableBinding, 
    TraceWriter log)
{
    log.Info("function processed a request.");
    var auth = new LinqToTwitter.SingleUserAuthorizer
    {
        CredentialStore = new LinqToTwitter.SingleUserInMemoryCredentialStore
        {
            ConsumerKey = Environment.GetEnvironmentVariable("Twitter_ConsumerKey"),
            ConsumerSecret = Environment.GetEnvironmentVariable("Twitter_ConsumerSecret"),
            AccessToken = Environment.GetEnvironmentVariable("Twitter_AccessToken"),
            AccessTokenSecret = Environment.GetEnvironmentVariable("Twitter_AccessTokenSecret")
        }
    };
    var twitter = new TwitterContext(auth);
    var dic = await EigoMeigen_bot(twitter, log);
    foreach (var key in dic.Keys)
    {
        var keyText = key.ToString();
        log.Info(keyText);
        var existEntity = QuestionEntity.GetEntity(keyText);
        if (existEntity != null)
            continue;
        var entity = new QuestionEntity(key) {
            Sentence = dic[key],
        };
        await tableBinding.AddAsync(entity);
        await queueBinding.AddAsync(keyText);
    }
}

private static async Task<Dictionary<ulong, string>> EigoMeigen_bot(TwitterContext context, TraceWriter log)
{
    var tweets = await context.Status
        .Where(tweet => tweet.Type == StatusType.User && tweet.ScreenName == "EigoMeigen_bot")
        .ToListAsync();
    Dictionary<ulong, string> dic = new Dictionary<ulong, string>();
    tweets.ForEach((obj) => {
        var english = obj.Text.Split(new string[] { "\n" }, StringSplitOptions.None).FirstOrDefault();
        dic.Add(obj.StatusID, english);
        log.Info(english);
    });
    return dic;
}