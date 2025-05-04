internal class HttpClientFactory : IHttpClientFactory
{
  public HttpClient CreateClient(string name)
  {
    return new HttpClient();
  }
}