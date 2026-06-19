using ChineseRaffleApi.Dto;

namespace ChineseRaffleApi.Services.DI
{
    public interface IKafkaEventPublisher
    {
        Task PublishAsync(KafkaTransactionEventDto eventDto, CancellationToken cancellationToken = default);
    }
}