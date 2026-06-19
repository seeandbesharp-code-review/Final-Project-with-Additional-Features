using System.Text.Json;
using ChineseRaffleApi.Configuration;
using ChineseRaffleApi.Dto;
using ChineseRaffleApi.Services.DI;
using Confluent.Kafka;

namespace ChineseRaffleApi.Services
{
    public sealed class KafkaEventPublisher : IKafkaEventPublisher
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IProducer<Null, string> _producer;
        private readonly KafkaSettings _kafkaSettings;
        private readonly ILogger<KafkaEventPublisher> _logger;

        public KafkaEventPublisher(IProducer<Null, string> producer, KafkaSettings kafkaSettings, ILogger<KafkaEventPublisher> logger)
        {
            _producer = producer;
            _kafkaSettings = kafkaSettings;
            _logger = logger;
        }

        public async Task PublishAsync(KafkaTransactionEventDto eventDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = JsonSerializer.Serialize(eventDto, SerializerOptions);
                var message = new Message<Null, string>
                {
                    Value = payload
                };

                await _producer.ProduceAsync(_kafkaSettings.TopicName, message, cancellationToken);
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Kafka produce failed for topic {Topic}", _kafkaSettings.TopicName);
            }
            catch (KafkaException ex)
            {
                _logger.LogError(ex, "Kafka error while publishing to topic {Topic}", _kafkaSettings.TopicName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while publishing Kafka event to topic {Topic}", _kafkaSettings.TopicName);
            }
        }
    }
}