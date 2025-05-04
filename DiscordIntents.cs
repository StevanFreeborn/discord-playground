internal static class DiscordIntents
{
  public const int Guilds = 1 << 0;
  public const int GuildMembers = 1 << 1;
  public const int GuildModeration = 1 << 2;
  public const int GuildExpressions = 1 << 3;
  public const int GuildIntegrations = 1 << 4;
  public const int GuildWebhooks = 1 << 5;
  public const int GuildInvites = 1 << 6;
  public const int GuildVoiceStates = 1 << 7;
  public const int GuildPresences = 1 << 8;
  public const int GuildMessages = 1 << 9;
  public const int GuildMessageReactions = 1 << 10;
  public const int GuildMessageTyping = 1 << 11;
  public const int DirectMessages = 1 << 12;
  public const int DirectMessageReactions = 1 << 13;
  public const int DirectMessageTyping = 1 << 14;
  public const int MessageContent = 1 << 15;
  public const int GuildScheduledEvents = 1 << 16;
  public const int AutoModerationConfiguration = 1 << 20;
  public const int AutoModerationExecution = 1 << 21;
  public const int GuildMessagePolls = 1 << 24;
  public const int DirectMessagePolls = 1 << 25;
  public const int All = Guilds
    | GuildMembers
    | GuildModeration
    | GuildExpressions
    | GuildIntegrations
    | GuildWebhooks
    | GuildInvites
    | GuildVoiceStates
    | GuildPresences
    | GuildMessages
    | GuildMessageReactions
    | GuildMessageTyping
    | DirectMessages
    | DirectMessageReactions
    | DirectMessageTyping
    | MessageContent
    | GuildScheduledEvents
    | AutoModerationConfiguration
    | AutoModerationExecution
    | GuildMessagePolls
    | DirectMessagePolls;
}