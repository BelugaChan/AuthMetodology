

using System.Diagnostics.Metrics;

namespace AuthMetodology.Application.Metrics
{
    public class MetricsRegistry : IDisposable
    {
        private readonly Meter meter;

        public Counter<int> HttpRequestCounter { get; }

        public Histogram<double> HttpResponseTimeMs { get; }

        public MetricsRegistry(IMeterFactory meterFactory)
        {
            meter = meterFactory.Create("ApiRequestMetrics");

            HttpRequestCounter = meter.CreateCounter<int>(
                name: "http_requests",
                unit: "requests",
                description: "Counts the number of requests to the API"
                );

            HttpResponseTimeMs = meter.CreateHistogram<double>(
                name: "http_response",
                unit: "ms",
                description: "HTTP response time in milliseconds"
            );
        }

        //public void IncrementRequestCounter(string endpointName, string method, int statusCode)
        //{
        //    HttpRequestCounter.Add(1, 
        //        new KeyValuePair<string, object?>("endpoint", endpointName),
        //        new KeyValuePair<string, object?>("method", method),
        //        new KeyValuePair<string, object?>("status", statusCode));
        //}

        public void Dispose() => meter?.Dispose();
    }
}
