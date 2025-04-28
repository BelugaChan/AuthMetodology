using System.Diagnostics.Metrics;

namespace AuthMetodology.Application.Metrics
{
    public class MetricsRegistry : IDisposable
    {
        private readonly Meter meter;

        public Counter<int> HttpRequestCounter { get; }

        public Histogram<double> HttpResponseTimeMs { get; }

        public ObservableGauge<long> MemoryUsageBytes { get; }

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

            MemoryUsageBytes = meter.CreateObservableGauge<long>(
                name: "process_memory_bytes",
                unit: "bytes",
                description: "Current memory usage of the process",
                observeValues: () => [new Measurement<long>(
                    value: Random.Shared.Next(),
                    tags: new KeyValuePair<string, object?>("service", "auth-api")
                    )]
            );
        }

        public void Dispose() => meter?.Dispose();
    }
}
