internal class DiscordClientException : Exception
{
  public DiscordClientException(string message) : base(message)
  {
  }

  public DiscordClientException(string message, Exception innerException) : base(message, innerException)
  {
  }
}