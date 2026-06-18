using ChineseRaffleApi.Dto;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaConsumerOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddHostedService<KafkaConsumerWorker>();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddLogging(logging => logging.AddSerilog(dispose: true));

var host = builder.Build();
await host.RunAsync();

public sealed class KafkaConsumerOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string AutoOffsetReset { get; set; } = "Earliest";
    public bool EnableAutoCommit { get; set; } = false;
    public bool EnableAutoOffsetStore { get; set; } = false;
    public int SessionTimeoutMs { get; set; } = 10000;
    public int HeartbeatIntervalMs { get; set; } = 3000;
    public int MaxPollIntervalMs { get; set; } = 300000;
    public string ClientId { get; set; } = string.Empty;
}

public sealed class KafkaConsumerWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<KafkaConsumerWorker> _logger;
    private readonly KafkaConsumerOptions _options;

    public KafkaConsumerWorker(ILogger<KafkaConsumerWorker> logger, IOptions<KafkaConsumerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            ClientId = string.IsNullOrWhiteSpace(_options.ClientId) ? Environment.MachineName : _options.ClientId,
            AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_options.AutoOffsetReset, true, out var autoOffsetReset)
                ? autoOffsetReset
                : AutoOffsetReset.Earliest,
            EnableAutoCommit = _options.EnableAutoCommit,
            EnableAutoOffsetStore = _options.EnableAutoOffsetStore,
            SessionTimeoutMs = _options.SessionTimeoutMs,
            HeartbeatIntervalMs = _options.HeartbeatIntervalMs,
            MaxPollIntervalMs = _options.MaxPollIntervalMs
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig)
            .SetErrorHandler((_, error) => _logger.LogWarning("Kafka consumer error: {Reason} ({Code})", error.Reason, error.Code))
            .SetPartitionsAssignedHandler((_, partitions) =>
            {
                _logger.LogInformation("Assigned partitions: {Partitions}", string.Join(", ", partitions));
            })
            .SetPartitionsRevokedHandler((_, partitions) =>
            {
                _logger.LogInformation("Revoked partitions: {Partitions}", string.Join(", ", partitions));
                return partitions;
            })
            .Build();

        consumer.Subscribe(_options.TopicName);
        _logger.LogInformation("Kafka consumer started for topic {Topic} with group {GroupId}", _options.TopicName, _options.GroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null)
                {
                    continue;
                }

                var payload = JsonSerializer.Deserialize<KafkaTransactionEventDto>(result.Message.Value, SerializerOptions);
                if (payload is null)
                {
                    _logger.LogWarning("Received null payload at {TopicPartitionOffset}", result.TopicPartitionOffset);
                    continue;
                }

                _logger.LogInformation(
                    "Kafka message consumed from {TopicPartitionOffset}: {@Payload}",
                    result.TopicPartitionOffset,
                    payload);

                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume failed: {Reason}", ex.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Kafka payload deserialization failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Kafka consumer error");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        consumer.Close();
        await Task.CompletedTask;
    }
}