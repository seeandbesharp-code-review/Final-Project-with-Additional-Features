using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using ChineseRaffleApi.Controllers;
using ChineseRaffleApi.Services.DI;
using ChineseRaffleApi.Dto;

namespace ChineseRaffleApi.Tests.Controllers
{
    public class BasketControllerTests
    {
        [Fact]
        public async Task AddBasket_WhenAuthorizedAndRaffleOpen_ReturnsCreated()
        {
            var basketService = new Mock<IBasketService>();
            var giftService = new Mock<IGiftService>();
            var logger = new Mock<ILogger<BasketController>>();

            giftService.Setup(g => g.IsRaffleLocked()).ReturnsAsync(false);
            basketService.Setup(b => b.AddBasketAsync(It.IsAny<AddBasketDto>())).ReturnsAsync(100);

            var controller = new BasketController(basketService.Object, logger.Object, giftService.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.NameIdentifier, "42")
            }, "mock"));

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var add = new AddBasketDto { UserId = 42, GiftId = 2, Quantity = 1 };

            var res = await controller.AddBasket(add);

            res.Should().BeOfType<CreatedAtActionResult>();
        }
    }
}
