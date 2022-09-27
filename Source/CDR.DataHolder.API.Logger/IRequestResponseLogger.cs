namespace CDR.DataHolder.API.Logger
{
    using Serilog;

    public interface IRequestResponseLogger
    {
        ILogger Log { get; }
    }
}