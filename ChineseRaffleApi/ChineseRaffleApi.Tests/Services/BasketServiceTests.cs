using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;
using ChineseRaffleApi.Repository.DI;
using ChineseRaffleApi.Services;
using ChineseRaffleApi.Models;
using ChineseRaffleApi.Dto;

using ChineseRaffleApi.Services.DI;

namespace ChineseRaffleApi.Tests.Services
{
    public class BasketServiceTests
    {
        [Fact]
        public async Task AddBasketAsync_WhenExistingBasket_ShouldUpdateAndReturnExistingId()
        {
            var repoMock = new Mock<IBasketRepo>();
            var ticketServiceMock = new Mock<ITicketService>();
            var mapperMock = new Mock<IMapper>();

            var addDto = new AddBasketDto { UserId = 1, GiftId = 2, Quantity = 3 };
            var mappedBasket = new Basket { Id = 7, UserId = 1, GiftId = 2, Quantity = 3 };
            mapperMock.Setup(m => m.Map<Basket>(addDto)).Returns(mappedBasket);

            var existing = new Basket { Id = 7, UserId = 1, GiftId = 2, Quantity = 1 };
            repoMock.Setup(r => r.GetByUserAndGiftAsync(1, 2)).ReturnsAsync(existing);

            var service = new BasketService(repoMock.Object, mapperMock.Object, ticketServiceMock.Object);

            var result = await service.AddBasketAsync(addDto);

            result.Should().Be(existing.Id);
            repoMock.Verify(r => r.UpdateBasketAsync(existing.Id, It.Is<Basket>(b => b.Quantity == 4)), Times.Once);
        }
    }
}
