#r "System.Configuration"
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

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("ResultCount function processed a request.");
    if (req.Method == HttpMethod.Post)
        return await Post(req, log);
    else
        return req.CreateResponse(HttpStatusCode.InternalServerError, "Not implimentation");
}

private static async Task<HttpResponseMessage> Post(HttpRequestMessage req, TraceWriter log)
{
    var auth = new LinqToTwitter.SingleUserAuthorizer
    {
        CredentialStore = new LinqToTwitter.SingleUserInMemoryCredentialStore
        {
            ConsumerKey = ConfigurationManager.AppSettings["Twitter_ConsumerKey"],
            ConsumerSecret = ConfigurationManager.AppSettings["Twitter_ConsumerSecret"],
            AccessToken = ConfigurationManager.AppSettings["Twitter_AccessToken"],
            AccessTokenSecret = ConfigurationManager.AppSettings["Twitter_AccessTokenSecret"]
        }
    };
    var twitter = new TwitterContext(auth);
    var dic = await EigoMeigen_bot(twitter);
    foreach (var key in dic.Keys)
    {
        var existEntity = QuestionEntity.GetEntity(key.ToString());
        if (existEntity != null)
            continue;
        var entity = new QuestionEntity(key) {
            Sentence = dic[key],
        };
        entity.Insert();
    }
    return req.CreateResponse(HttpStatusCode.OK, new {
    });
}

private static async Task<Dictionary<ulong, string>> EigoMeigen_bot(TwitterContext context)
{
    var tweets = await context.Status
        .Where(tweet => tweet.Type == StatusType.User && tweet.ScreenName == "EigoMeigen_bot")
        .ToListAsync();
    Dictionary<ulong, string> dic = new Dictionary<ulong, string>();
    tweets.ForEach((obj) => {
        var english = obj.Text.Split(new string[] { "¥n" }, StringSplitOptions.None).FirstOrDefault();
        dic.Add(obj.StatusID, english);
    });
    return dic;
}