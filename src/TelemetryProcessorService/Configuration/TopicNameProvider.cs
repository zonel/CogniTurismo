using Microsoft.Extensions.Configuration;

namespace TelemetryProcessorService.Configuration;

public class TopicNameProvider
{
    private readonly IConfiguration _configuration;
    
    public TopicNameProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        var massTransitSettings = _configuration.GetSection("MassTransit").Get<MassTransitSettings>();
        AnomalyTopic = massTransitSettings?.AnomalyTopic ?? "telemetry-anomaly";
    }
    
    public string AnomalyTopic { get; }
}