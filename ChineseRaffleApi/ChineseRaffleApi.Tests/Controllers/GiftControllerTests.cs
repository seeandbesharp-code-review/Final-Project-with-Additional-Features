using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using ChineseRaffleApi.Controllers;
using ChineseRaffleApi.Services.DI;
using ChineseRaffleApi.Dto;

namespace ChineseRaffleApi.Tests.Controllers
{
    public class GiftControllerTests
    {
        [Fact]
        public async Task IsRaffleLocked_ReturnsOkWithValue()
        {
            var svc = new Mock<IGiftService>();
            var logger = new Mock<ILogger<GiftController>>();
            svc.Setup(s => s.IsRaffleLocked()).ReturnsAsync(true);

            var controller = new GiftController(svc.Object, logger.Object);

            var res = await controller.IsRaffleLocked();

            var ok = res.Result as OkObjectResult;
            ok.Should().NotBeNull();
            ((bool)ok!.Value).Should().BeTrue();
        }

        [Fact]
        public async Task GetGift_WhenNotFound_ReturnsNotFound()
        {
            var svc = new Mock<IGiftService>();
            var logger = new Mock<ILogger<GiftController>>();
            svc.Setup(s => s.GetGiftByIdAsync(9)).ReturnsAsync((GetGiftDto?)null);

            var controller = new GiftController(svc.Object, logger.Object);

            var res = await controller.GetGift(9);

            res.Result.Should().BeOfType<NotFoundResult>();
        }
    }
}
