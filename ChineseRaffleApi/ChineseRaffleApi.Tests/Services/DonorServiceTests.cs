using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using FluentAssertions;
using Xunit;
using ChineseRaffleApi.Models;
using ChineseRaffleApi.Repository.DI;
using ChineseRaffleApi.Repository;

using ChineseRaffleApi.Services;
using ChineseRaffleApi;
using AutoMapper;
using ChineseRaffleApi.Dto;


namespace ChineseRaffleApi.Tests.Services
{
    public class DonorServiceTests
    {
        [Fact]
        public async Task GetDonorByIdAsync_WhenDonorExists_ShouldReturnDonorDto()
        {

            var repositoryMock = new Mock<IDonorRepo>();
            var mapperMock = new Mock<IMapper>();

            var donorModel = new Donor { Id = 1, Name = "Test Donor" };
            var donorDto = new GetDonorDto { Id = 1, Name = "Test Donor" };

            repositoryMock.Setup(r => r.GetDonorByIdAsync(1)).ReturnsAsync(donorModel);

            mapperMock.Setup(m => m.Map<GetDonorDto>(donorModel)).Returns(donorDto);

            var service = new DonorService(repositoryMock.Object, mapperMock.Object);

            var result = await service.GetDonorByIdAsync(1);

            result.Should().NotBeNull();
            result.Name.Should().Be("Test Donor");
            result.Id.Should().Be(1);
        }

        [Fact]
        public async Task AddDonorAsync_WhenCalled_ShouldReturnAssignedId()
        {
            var repositoryMock = new Mock<IDonorRepo>();
            var mapperMock = new Mock<IMapper>();

            repositoryMock
                .Setup(r => r.AddDonorAsync(It.IsAny<Donor>()))
                .ReturnsAsync(1)
                .Callback<Donor>(d => d.Id = 42);

            var service = new DonorService(repositoryMock.Object, mapperMock.Object);

            var addDto = new AddDonorDto { Name = "New", Email = "n@e.com", PhoneNumber = "123" };

            var result = await service.AddDonorAsync(addDto);

            result.Should().Be(42);
            repositoryMock.Verify(r => r.AddDonorAsync(It.IsAny<Donor>()), Times.Once);
        }

        [Fact]
        public async Task GetPagedDonorsAsync_WhenCalled_ShouldReturnPagedResult()
        {
            var repositoryMock = new Mock<IDonorRepo>();
            var mapperMock = new Mock<IMapper>();

            var donors = new List<Donor>
            {
                new Donor { Id = 1, Name = "A" },
                new Donor { Id = 2, Name = "B" }
            };

            repositoryMock.Setup(r => r.GetPagedDonorsAsync(1, 2)).ReturnsAsync((donors, donors.Count));

            mapperMock.Setup(m => m.Map<IEnumerable<GetDonorDto>>(It.IsAny<IEnumerable<Donor>>()))
                .Returns((IEnumerable<Donor> src) => src.Select(d => new GetDonorDto { Id = d.Id, Name = d.Name }).ToList());

            var service = new DonorService(repositoryMock.Object, mapperMock.Object);

            var paged = await service.GetPagedDonorsAsync(1, 2);

            paged.Should().NotBeNull();
            paged.TotalCount.Should().Be(donors.Count);
            paged.Items.Should().HaveCount(donors.Count);
            paged.PageNumber.Should().Be(1);
            paged.PageSize.Should().Be(2);
        }
    }
}
