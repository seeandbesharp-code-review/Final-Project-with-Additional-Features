namespace ChineseRaffleApi.Dto
{
    public sealed class KafkaTransactionEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public int? UserId { get; set; }
        public int? BasketId { get; set; }
        public int? GiftId { get; set; }
        public string? GiftTitle { get; set; }
        public int? Quantity { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? TotalRevenue { get; set; }
        public IReadOnlyCollection<KafkaRaffleWinnerDto>? Winners { get; set; }
    }

    public sealed class KafkaRaffleWinnerDto
    {
        public int GiftId { get; set; }
        public string GiftTitle { get; set; } = string.Empty;
        public int? WinnerId { get; set; }
        public string WinnerName { get; set; } = string.Empty;
    }
}