using System.Collections.Concurrent;
using System.Net;
using RumbleViewBot;
using RumbleViewBot.Models;

namespace RumbleLib;

public class BotManager
{
    private readonly BotSettings _botSettings;

    private ConcurrentDictionary<string, RumbleTask> _botTasks = new();

    private ProxyService _proxyManager;
    private UserAgentService _userAgentProvider;
    private WebProxy _rotatingProxy;
    public BotManager(string proxiesPath, string userAgentsPath, BotSettings botSettings, string rotatingProxy)
    {
        //create proxy from rotatinproxy
        if (!string.IsNullOrEmpty(rotatingProxy))
        {
            var split = rotatingProxy.Split(':');
            if (split.Length == 4)
            {
                WebProxy proxy = new WebProxy(split[0], int.Parse(split[1]));
                proxy.Credentials = new NetworkCredential(split[2], split[3]);
                _rotatingProxy = proxy;
            }
        }
        else
        {
            _botSettings = botSettings;
        }
        _botSettings = botSettings;
        _proxyManager = new ProxyService(proxiesPath);
        _userAgentProvider = new UserAgentService(userAgentsPath);
    }

    public async Task<RumbleTask> StartNewTask(StartNewTaskSettings startNewTaskSettings)
    {
        if (startNewTaskSettings.Type == TaskType.viewers)
        {
            var viewBot = await StartViewBotAsync(startNewTaskSettings.StartViewersSettings);
            RumbleTask task = new RumbleTask()
            {
                Creationtime = DateTime.Now,
                Id = Guid.NewGuid().ToString(),
                Type = TaskType.viewers,
                ViewersSettings = startNewTaskSettings.StartViewersSettings,
                ViewBot = viewBot
            };

            _botTasks.TryAdd(task.Id, task);

            return task;
        }

        if (startNewTaskSettings.Type == TaskType.chatBot)
        {
            var chatBot = StartChatBot(startNewTaskSettings.StartChatBotSettings);
            RumbleTask task = new RumbleTask()
            {
                Creationtime = DateTime.Now,
                Id = Guid.NewGuid().ToString(),
                Type = TaskType.chatBot,
                ChatBotSettings = startNewTaskSettings.StartChatBotSettings,
                ChatBot = chatBot
            };
            
            _botTasks.TryAdd(task.Id, task);

            return task;
        }

        throw new Exception("Invalid task type");
    }

    private async Task<VideoViewBot> StartViewBotAsync(StartViewersSettings startViewersSettings)
    {
        var videoId = await Helper.GetVideoIdFromUrlAsync(startViewersSettings.VideoUrl);
        Helper.ValidateVideoId(videoId);
        

        VideoViewBot viewBot = new VideoViewBot(videoId, startViewersSettings.ViewCount, _botSettings, _proxyManager, _userAgentProvider);
        _ = viewBot.StartAsync();

        return viewBot;
    }

    private ChatBot StartChatBot( StartChatBotSettings chatBotSettings)
    {
        ChatBot chatBot = new ChatBot(chatBotSettings, _rotatingProxy);
        chatBot.Start();
        return chatBot;
    }

    public bool StopTask(string botId)
    {
        if (_botTasks.TryRemove(botId, out var botTask))
        {
            if (botTask.Type == TaskType.viewers)
            {
                botTask.ViewBot.Stop();
            }
            else if (botTask.Type == TaskType.chatBot)
            {
                botTask.ChatBot.Cancel();
            }
            return true;
        }
        return false;
    }
    
    public RumbleTask? GetTask(string botId)
    {
        if (_botTasks.TryGetValue(botId, out var botTask))
        {
            return botTask;
        }

        return null;
    }


    public IEnumerable<RumbleTask> ListTasks()
    {
        return _botTasks.Values;
    }
}

public class ViewTask
{
    public string Id { get; set; }
    public int ViewCount { get; set; }
    public string VideoId { get; set; }
    public DateTime StartTime { get; set; }
    public VideoViewBot Bot { get; set; }
}

public class RumbleTask
{
    public string Id { get; set; }
    public DateTime Creationtime { get; set; }
    public TaskType Type { get; set; }
    public StartViewersSettings ViewersSettings { get; set; }
    public StartChatBotSettings ChatBotSettings { get; set; }
    public VideoViewBot ViewBot { get; set; }
    public ChatBot ChatBot { get; set; }
}

public class StartNewTaskSettings
{
    public TaskType Type { get; set; }
    public StartViewersSettings? StartViewersSettings { get; set; }
    public StartChatBotSettings? StartChatBotSettings { get; set; }
}

public class StartViewersSettings
{
    public string VideoUrl { get; set; }
    public int ViewCount { get; set; }
}

public class StartChatBotSettings
{
    public string VideoUrl { get; set; }
    public int MessagesPerMinute { get; set; }
    public List<ChatMessage> Messages { get; set; }
}

public class ChatMessage
{
    public string Message { get; set; }
}

public enum TaskType
{
    viewers = 0,
    chatBot = 1
}
