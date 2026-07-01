using OpenTelemetry.Trace;

namespace MemoryWiki.Api.Common;

public static class TelemetryExtensions
{
    /// <summary>Adds an OTLP exporter when OTEL_EXPORTER_OTLP_ENDPOINT is set; otherwise console.</summary>
    public static TracerProviderBuilder AddOtlpExporterIfConfigured(this TracerProviderBuilder builder, IConfiguration config)
    {
        var endpoint = config["OTEL_EXPORTER_OTLP_ENDPOINT"]
                       ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        return string.IsNullOrWhiteSpace(endpoint)
            ? builder.AddConsoleExporter()
            : builder.AddOtlpExporter(o => o.Endpoint = new Uri(endpoint));
    }
}
