namespace TelemetryIngestionService.Infrastructure.Configuration
{
    public class BufferSettings
    {
        public int BatchSize { get; set; } = 500;
        public int MaxBatchSize { get; set; } = 1000;
        public int FlushIntervalMs { get; set; } = 1000;
        public int CheckIntervalMs { get; set; } = 200;
        public int CopyThreshold { get; set; } = 100;
    }
}