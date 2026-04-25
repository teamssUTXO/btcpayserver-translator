namespace BTCPayTranslator.Tests;

internal sealed class QueueHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public QueueHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    private int _callCount;
    public int CallCount => Volatile.Read(ref _callCount);

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        LastRequestUri = request.RequestUri;
        return Task.FromResult(_responder(request));
    }
}
