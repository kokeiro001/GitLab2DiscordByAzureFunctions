#r "Newtonsoft.Json"

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;


public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    // ヘッダーにGitLab.comからの目印が無いならスキップ
    var gitLabEventHeader = req.Headers.FirstOrDefault(p => p.Key.ToLower() == "x-gitlab-event");

    if(gitLabEventHeader.Key == null)
    {
        // await NotifyTestAsync(log);
        return req.CreateResponse(HttpStatusCode.BadRequest, "X-Gitlab-Eventが見つからん");
    }

    var headerValue = gitLabEventHeader.Value.First();
    log.Info($"HeaderKeyValuePair= {gitLabEventHeader.Key}:{headerValue}");

    string jsonContent = await req.Content.ReadAsStringAsync();
    //  log.Info(jsonContent);

    await HandleRecievedJsonAsync(headerValue, jsonContent, log);

    return req.CreateResponse(HttpStatusCode.OK, "OK!");
}

private static async Task NotifyTestAsync(TraceWriter log)
{
    var model = new NotifyModel()
    {
        Serif = "セリフのテストだよー！",
        InlineMessage = $"メッセージだよー！",
        AuthorName = "投稿者の名前だよー！",
        Title = "タイトルだよー！",
    };
    await NotifiyDiscordAsync(model, log);
}

[JsonObject("message")]
public class MessageModel
{
    [JsonProperty("content")]
    public string Content { get; set; }
    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; set; }
    [JsonProperty("embeds")]
    public EmbedModel[] Embeds { get; set; }
}

[JsonObject("embed")]
public class EmbedModel
{
    [JsonProperty("title")]
    public string Title { get; set; }
    [JsonProperty("description")]
    public string Description { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; } = "rich"; // Webhookのときは必ずrich
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("color")]
    public int Color { get; set; } = 0x4169e1;
    [JsonProperty("footer")]
    public FooterModel Footer { get; set; }
    [JsonProperty("image")]
    public ImageModel Image { get; set; }
    [JsonProperty("thumbnail")]
    public ThumbnailModel Thumbnail { get; set; }
    [JsonProperty("video")]
    public VideoModel Video { get; set; }
    [JsonProperty("provider")]
    public ProviderModel Provider { get; set; }
    [JsonProperty("author")]
    public AuthorModel Author { get; set; }
    [JsonProperty("fields")]
    public FieldModel[] Fields { get; set; }
}

[JsonObject("footer")]
public class FooterModel
{
    [JsonProperty("text")]
    public string Text { get; set; }
    [JsonProperty("icon_url")]
    public string IconUrl { get; set; }
    [JsonProperty("proxy_icon_url")]
    public string ProxyIconUrl { get; set; }
}
[JsonObject("image")]
public class ImageModel
{
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("proxy_url")]
    public string ProxyUrl { get; set; }
    [JsonProperty("height")]
    public int Height { get; set; }
    [JsonProperty("width")]
    public int Width { get; set; }
}
[JsonObject("thumbnail")]
public class ThumbnailModel
{
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("proxy_url")]
    public string ProxyUrl { get; set; }
    [JsonProperty("height")]
    public int Height { get; set; }
    [JsonProperty("width")]
    public int Width { get; set; }
}
[JsonObject("video")]
public class VideoModel
{
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("height")]
    public int Height { get; set; }
    [JsonProperty("width")]
    public int Width { get; set; }
}
[JsonObject("provider")]
public class ProviderModel
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("url")]
    public string Url { get; set; }
}
[JsonObject("author")]
public class AuthorModel
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("url")]
    public string Url { get; set; }
    [JsonProperty("icon_url")]
    public string IconUrl { get; set; }
    [JsonProperty("proxy_icon_url")]
    public string ProxyIconUrl { get; set; }
 }

[JsonObject("field")]
public class FieldModel
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("value")]
    public string Value { get; set; }
    [JsonProperty("inline")]
    public bool Inline { get; set; }
}

public class NotifyModel
{
    public string Serif { get; set; }
    public string InlineMessage { get; set; }
    public int Color { get; set; } = 0x4169e1;
    public string Title { get; set; }
    public string Url { get; set; }
    public string AuthorName { get; set; }
    public string AuthorIconUrl { get; set; }
}

private static async Task NotifiyDiscordAsync(NotifyModel model, TraceWriter log)
{
    var webhookUrl = @"your discord webhook url";

    var messageModel = new MessageModel()
    {
        Content = model.Serif,
        Embeds = new EmbedModel[]
        {
           new EmbedModel() 
           {
               Title = model.Title,
               Url = model.Url,
               Description = model.InlineMessage,
               Author = new AuthorModel()
               {
                    Name = model.AuthorName,
                    IconUrl = model.AuthorIconUrl
               },
           },
        }
   };
    string json = Newtonsoft.Json.JsonConvert.SerializeObject(messageModel, Formatting.Indented);
    log.Info($"formated json...\n{json}");

    using (var client = new HttpClient())
    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
    {
        var response = await client.PostAsync(webhookUrl, content);
        // log.Info(await response.Content.ReadAsStringAsync());
    }
}

private static async Task HandleRecievedJsonAsync(string headerValue, string jsonContent, TraceWriter log)
{
    dynamic json = JsonConvert.DeserializeObject(jsonContent);
    switch(headerValue)
    {
        case "Issue Hook":
            {
                var action = json.object_attributes.action;
                var title = json.object_attributes.title;
                var model = new NotifyModel()
                {
                    AuthorName = json.user.username,
                    AuthorIconUrl = json.user.avatar_url,
                    Title = title,
                    Url = json.object_attributes.url,
                };

                if (action == "open")
                {
                    model.Serif = $"{model.AuthorName}さんが新しいIssueを作成したよー！";
                    model.InlineMessage = $"```\n{model.Title}\n{model.Url}\n```";
                    await NotifiyDiscordAsync(model, log);
                }
                else
                {
                    model.Serif = $"{model.AuthorName}さんがIssueを操作したよー";
                    model.InlineMessage = $"```\n！(API):action={action} Issue Hook\n{model.Title}\n{model.Url}\n```";
                    await NotifiyDiscordAsync(model, log);
                }
            }
            break;
        case "Note Hook":
            {
                var noteBody = json.object_attributes.note;
                var noteTargetType = json.object_attributes.noteable_type;
                var userName = json.user.username;
                var url = json.object_attributes.url;

                var model = new NotifyModel()
                {
                    Serif = $"{userName}さんが{noteTargetType}にコメントしたよー！",
                    InlineMessage = $"```\n{noteBody}\n```\n{url}",
                    AuthorName = userName,
                    AuthorIconUrl = json.user.avatar_url,
                    Title = noteTargetType,
                    Url = url,
                };
                await NotifiyDiscordAsync(model, log);
            }
            break;
        case "Push Hook":
            {
                string @ref = json.@ref;
                var splited = @ref.Split('/');
                var branchName = splited.Last();
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("```");
                foreach(var commit in json.commits)
                {
                    var userName = commit.author.name;
                    var message = commit.message;
                    sb.AppendLine($"{userName}\t{message}".Trim());
                }
                sb.AppendLine("```");
                var model = new NotifyModel()
                {
                    Serif = $"{json.user_name}さんが[{branchName}]ブランチにプッシュしたよー！",
                    InlineMessage = sb.ToString(),
                    AuthorName = json.user_name,
                    AuthorIconUrl = json.user_avatar,
                    Title = branchName,
                };
                await NotifiyDiscordAsync(model, log);
            }
            break;
        default:
            {
                log.Info($"headerValue {headerValue}");
                var model = new NotifyModel()
                {
                    Serif = "GitLabから通知が来たけど、意味が分からないよ～",
                    InlineMessage = $"```headerValue...\n{headerValue}\n```",
                    AuthorName = "",
                    AuthorIconUrl = "",
                    Title = "",
                    Url = "",
                };
                await NotifiyDiscordAsync(model, log);
            }
            break;
    }
}