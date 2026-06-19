using AutoMapper;
using ChineseRaffleApi.Dto;
using ChineseRaffleApi.Models;
using ChineseRaffleApi.Repository.DI;
using ChineseRaffleApi.Services.DI;

namespace ChineseRaffleApi.Services
{
    public class BasketService : IBasketService
    {
        private readonly IBasketRepo _basketRepo;
        private readonly ITicketService _ticketService;
        private readonly IKafkaEventPublisher _kafkaEventPublisher;
        private readonly IMapper _mapper;

        public BasketService(IBasketRepo basketRepo, IMapper mapper, ITicketService ticketService, IKafkaEventPublisher kafkaEventPublisher)
        {
            _basketRepo = basketRepo;
            _ticketService = ticketService;
            _kafkaEventPublisher = kafkaEventPublisher;
            _mapper = mapper;
        }
        public async Task<GetBasketDto?> GetBasketByIdAsync(int id)
        {
            var basket = await _basketRepo.GetBasketByIdAsync(id);
            return _mapper.Map<GetBasketDto>(basket);
        }
        public async Task<IEnumerable<GetBasketDto>> GetBasketsByUserIdAsync(int userId)
        {
            var basket = await _basketRepo.GetBasketsByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<GetBasketDto>>(basket);
        }

        public async Task<IEnumerable<GetBasketDto>> GetBasketsByGiftIdAsync(int giftId)
        {
            var basket = await _basketRepo.GetBasketsByGiftIdAsync(giftId);
            return _mapper.Map<IEnumerable<GetBasketDto>>(basket);

        }


        public async Task<int?> AddBasketAsync(AddBasketDto basketDto)
        {
            var basket = _mapper.Map<Basket>(basketDto);

            var existingBasket = await _basketRepo
                .GetByUserAndGiftAsync(basket.UserId, basket.GiftId);

            if (existingBasket != null)
            {
                existingBasket.Quantity += basket.Quantity;
                await _basketRepo.UpdateBasketAsync(existingBasket.Id, existingBasket);
                return existingBasket.Id;
            }
            return await _basketRepo.AddBasketAsync(basket);
        }

        public async Task UpdateBasketAsync(int id, UpdateBasketDto basket)
        {
            if (basket.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero");

            var existingBasket = await _basketRepo.GetBasketByIdAsync(id);

            if (existingBasket == null)
                throw new KeyNotFoundException($"Basket with id {id} not found");

            existingBasket.Quantity = basket.Quantity;

            await _basketRepo.UpdateBasketAsync(id, existingBasket);
        }

        public async Task DeleteBasketAsync(int id)
        {
            await _basketRepo.DeleteBasketAsync(id);
        }

        public async Task<bool> BasketExistsAsync(int id)
        {
            return await _basketRepo.BasketExistsAsync(id);
        }
        public async Task BuyTicketsFromBasket(int userId)
        {
            var baskets = await _basketRepo.GetBasketsByUserIdAsync(userId);

            foreach (var basket in baskets)
            {
                var gift = basket.Gift;
                if (gift == null)
                    throw new Exception($"Gift with id {basket.GiftId} not found");
                var ticket = new AddTicketDto
                {
                    UserId = userId,
                    GiftId = gift.Id
                };
                for (int i = 0; i < basket.Quantity; i++)
                {
                  await _ticketService.AddTicketAsync(ticket);
                }
                await _basketRepo.DeleteBasketAsync(basket.Id);

                await _kafkaEventPublisher.PublishAsync(new KafkaTransactionEventDto
                {
                    EventType = "OrderCreated",
                    Operation = "BasketCheckout",
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    UserId = userId,
                    BasketId = basket.Id,
                    GiftId = gift.Id,
                    GiftTitle = gift.Title,
                    Quantity = basket.Quantity,
                    TotalAmount = basket.Quantity * gift.TicketPrice
                });
            }
        }
    }
}
