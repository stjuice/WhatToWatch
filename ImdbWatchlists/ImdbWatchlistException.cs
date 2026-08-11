namespace ImdbWatchlists;

public class ImdbWatchlistException : Exception
{
    public ImdbWatchlistException(string message)
        : base(message)
    {
    }

    public ImdbWatchlistException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
