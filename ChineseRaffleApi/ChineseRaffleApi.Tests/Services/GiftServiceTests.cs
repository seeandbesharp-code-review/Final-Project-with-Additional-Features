using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;
using ChineseRaffleApi.Services;
using ChineseRaffleApi.Repository.DI;
using ChineseRaffleApi.Models;
using ChineseRaffleApi.Dto;

namespace ChineseRaffleApi.Tests.Services
{
    public class GiftServiceTests
    {
        [Fact]
        public async Task AddGiftAsync_WhenTitleExists_ShouldThrowArgumentException()
        {
            var repoMock = new Mock<IGiftRepo>();
            var mapperMock = new Mock<IMapper>();

            repoMock.Setup(r => r.GiftExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var service = new GiftService(repoMock.Object, mapperMock.Object);

            var dto = new AddGiftDto { Title = " Exists ", CategoryId = 1 };

            await Assert.ThrowsAsync<ArgumentException>(() => service.AddGiftAsync(dto));
        }

        [Fact]
        public async Task GetGiftByIdAsync_WhenGiftExists_ShouldReturnDto()
        {
            var repoMock = new Mock<IGiftRepo>();
            var mapperMock = new Mock<IMapper>();

            var gift = new Gift { Id = 5, Title = "G" };
            var dto = new GetGiftDto { Id = 5, Title = "G" };

            repoMock.Setup(r => r.GetGiftByIdAsync(5)).ReturnsAsync(gift);
            mapperMock.Setup(m => m.Map<GetGiftDto>(gift)).Returns(dto);

            var service = new GiftService(repoMock.Object, mapperMock.Object);

            var result = await service.GetGiftByIdAsync(5);

            result.Should().NotBeNull();
            result.Id.Should().Be(5);
            result.Title.Should().Be("G");
        }
    }
}
