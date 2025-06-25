namespace CDR.DataHolder.Shared.API.Logger
{
    using Serilog;

    public interface IRequestResponseLogger
    {
        ILogger Log { get; }
    }
}
