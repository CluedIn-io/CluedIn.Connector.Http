using System;

namespace CluedIn.Connector.Http.Services
{
    public interface ICorrelationIdGenerator
    {
        string Next();
    }

    internal class CorrelationIdGenerator : ICorrelationIdGenerator
    {
        public string Next()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
