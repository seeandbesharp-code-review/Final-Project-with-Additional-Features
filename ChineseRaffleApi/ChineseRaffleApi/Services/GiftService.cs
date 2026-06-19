using AutoMapper;
using ChineseRaffleApi.Dto;
using ChineseRaffleApi.Models;
using ChineseRaffleApi.Repository;
using ChineseRaffleApi.Repository.DI;
using ChineseRaffleApi.Services.DI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ChineseRaffleApi.Services
{
    public class GiftService : IGiftService
    {
        private readonly IGiftRepo _giftRepo;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _giftByIdCacheTtl;

        public GiftService(IGiftRepo giftRepo, IMapper mapper, IDistributedCache? cache = null, IConfiguration? configuration = null)
        {
            _giftRepo = giftRepo;
            _mapper = mapper;
            _cache = cache ?? new NoOpDistributedCache();
            var ttlSeconds = configuration?.GetValue<int?>("CacheSettings:GiftByIdTtlSeconds") ?? 60;
            _giftByIdCacheTtl = TimeSpan.FromSeconds(ttlSeconds);
        }

        public async Task<bool> IsRaffleLocked()
        {
            return await _giftRepo.IsRaffleLocked();
        }



        public async Task<GetGiftDto?> GetGiftByIdAsync(int id)
        {
            var cacheKey = $"gift:{id}";
            var cachedGift = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrWhiteSpace(cachedGift))
            {
                try
                {
                    return JsonSerializer.Deserialize<GetGiftDto>(cachedGift);
                }
                catch
                {
                    // If cache contains invalid data, ignore and refresh from the repository.
                }
            }

            var gift = await _giftRepo.GetGiftByIdAsync(id);
            var dto = _mapper.Map<GetGiftDto>(gift);
            if (dto != null)
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _giftByIdCacheTtl
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), cacheOptions);
            }
            return dto;
        }

        public async Task<PagedResult<GetGiftDto>> GetAllGiftsAsync(int pageNumber, int pageSize)
        {
            var pagedGifts = await _giftRepo.GetAllGiftsAsync(pageNumber, pageSize);

            return new PagedResult<GetGiftDto>
            {
                TotalCount = pagedGifts.TotalCount,
                PageNumber = pagedGifts.PageNumber,
                PageSize = pagedGifts.PageSize,
                Items = _mapper.Map<List<GetGiftDto>>(pagedGifts.Items)
            };
        }
     

        public async Task<int> AddGiftAsync(AddGiftDto gift)
        {
            var trimmedTitle = gift.Title?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                throw new ArgumentException("Gift title is required.");
            }

            if (await _giftRepo.GiftExistsAsync(trimmedTitle))
            {
                throw new ArgumentException($"Gift title '{trimmedTitle}' is already existing.");
            }

            var newGift = new Gift
            {
                Title = trimmedTitle, 
                CategoryId = gift.CategoryId,
                DonorId = gift.DonorId,
                TicketPrice = gift.TicketPrice,
                Image = gift.Image,
            };

            await _giftRepo.AddGiftAsync(newGift);
            return newGift.Id; 
        }

        public async Task<bool> UpdateGiftAsync(int id, UpdateGiftDto gift)
        {

            var existing = await _giftRepo.GetGiftByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Gift with id {id} was not found.");

            if (!string.IsNullOrWhiteSpace(gift.Title))
            {
                var trimmedTitle = gift.Title.Trim();
                if (!string.Equals(trimmedTitle, existing.Title, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (await _giftRepo.GiftExistsAsync(trimmedTitle))
                        throw new ArgumentException($"Gift title '{trimmedTitle}' is already existing.");
                }

                existing.Title = trimmedTitle;
            }
            if (gift.CategoryId.HasValue)
                existing.CategoryId = gift.CategoryId;

            if (gift.DonorId.HasValue)
                existing.DonorId = gift.DonorId.Value;

            if (gift.TicketPrice.HasValue)
                existing.TicketPrice = gift.TicketPrice.Value;

            if (gift.Image != null)
                existing.Image = gift.Image;

            if (gift.WinnerId.HasValue)
                existing.WinnerId = gift.WinnerId;

            var updated = await _giftRepo.UpdateGiftAsync(id, existing);
            if (!updated)
                throw new InvalidOperationException($"Failed to update gift with id {id}.");
            return true;

        }

        public async Task<bool> DeleteGiftAsync(int id)
        {
            return await _giftRepo.DeleteGiftAsync(id);
        }

        public async Task<bool> GiftExistsAsync(string title)
        {
            return await _giftRepo.GiftExistsAsync(title);
        }

        public async Task<IEnumerable<GetGiftDto>> GetGiftByDonorNameAsync(string name)
        {
            var gifts = await _giftRepo.GetGiftByDonorNameAsync(name);
            return _mapper.Map<List<GetGiftDto>>(gifts);
        }

        public async Task<GetGiftDto?> GetGiftByTitleAsync(string title)
        {
            var gift = await _giftRepo.GetGiftByTitleAsync(title);
            return _mapper.Map<GetGiftDto>(gift);
        }
        public async Task<IEnumerable<GetGiftWithTicketsDto>> GetGiftsWithTicketsAsync()
        {
            var gifts = await _giftRepo.GetGiftsWithTicketsAsync();
            return _mapper.Map<IEnumerable<GetGiftWithTicketsDto>>(gifts);
        }
        public async Task<IEnumerable<GetGiftDto>> GetGiftsWithMaxPriceAsync()
        {
            var gifts = await _giftRepo.GetGiftsWithMaxPriceAsync();
            return _mapper.Map<IEnumerable<GetGiftDto>>(gifts);
        }
        public async Task<IEnumerable<GetGiftDto>> GetGiftsWithMaxTicketsAsync()
        {
            var gifts = await _giftRepo.GetGiftsWithMaxTicketsAsync();
            return _mapper.Map<IEnumerable<GetGiftDto>>(gifts);
        }
        public async Task<IEnumerable<GetGiftWithBuyersDto>> GetGiftsWithBuyersAsync()
        {

            var gifts = await _giftRepo.GetGiftsWithBuyersAsync();
            
            return _mapper.Map<IEnumerable<GetGiftWithBuyersDto>>(gifts);
        }
        public async Task<IEnumerable<GetGiftDto>> GetSortedGiftsByPriceAsync()
        {
            var gifts = await _giftRepo.GetSortedGiftsByPriceAsync();
            return _mapper.Map<IEnumerable<GetGiftDto>>(gifts);
        }
        public async Task<IEnumerable<GetGiftDto>> GetSortedGiftsByCategoryAsync()
        {
            var gifts = await _giftRepo.GetSortedGiftsByCategoryAsync();
            return _mapper.Map<IEnumerable<GetGiftDto>>(gifts);
        }

        private class NoOpDistributedCache : IDistributedCache
        {
            public byte[]? Get(string key) => null;
            public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);
            public void Refresh(string key) { }
            public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
            public void Remove(string key) { }
            public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;
            public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }
            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.CompletedTask;
        }
    }
}
