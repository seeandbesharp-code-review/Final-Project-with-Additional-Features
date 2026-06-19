namespace ChineseRaffleApi.Configuration
{
    public sealed class KafkaSettings
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public KafkaProducerSettings Producer { get; set; } = new();
    }

    public sealed class KafkaProducerSettings
    {
        public string? ClientId { get; set; }
        public string Acks { get; set; } = "All";
        public bool EnableIdempotence { get; set; } = true;
        public int MessageSendMaxRetries { get; set; } = 3;
        public int RetryBackoffMs { get; set; } = 250;
        public int LingerMs { get; set; } = 0;
        public string CompressionType { get; set; } = "None";
        public int? SocketTimeoutMs { get; set; }
    }
}