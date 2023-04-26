using System;

namespace CluedIn.Connector.Http.Services
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
    }

    internal class Clock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
