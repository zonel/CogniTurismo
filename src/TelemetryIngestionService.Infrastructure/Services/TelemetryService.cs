using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Infrastructure.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly ITelemetryBufferService _bufferService;
        
        public TelemetryService(ITelemetryBufferService bufferService)
        {
            _bufferService = bufferService;
        }
        
        public Task ProcessTelemetryData(TelemetryData data)
        {
            // Simply add the data to the buffer and return
            // This allows for maximum message processing throughput
            _bufferService.AddTelemetryData(data);
            
            // Return completed task since we're not waiting for the database operation
            return Task.CompletedTask;
        }
    }
}