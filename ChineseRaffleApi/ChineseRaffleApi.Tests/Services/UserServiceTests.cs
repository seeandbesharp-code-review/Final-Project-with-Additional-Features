using System.Threading.Tasks;
using System.Text;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ChineseRaffleApi.Services;
using ChineseRaffleApi.Repository.DI;
using ChineseRaffleApi.Services.DI;
using ChineseRaffleApi.Models;
using ChineseRaffleApi.Dto;
using System.Collections.Generic;

namespace ChineseRaffleApi.Tests.Services
{
    public class UserServiceTests
    {
        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ReturnsLoginResponse()
        {
            var userRepo = new Mock<IUserRepo>();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<UserService>>();
            var mapper = new Mock<IMapper>();

            var inMemoryConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JwtSettings:ExpiryMinutes", "60" }
            }).Build();

            var password = "pass";
            var hashed = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));

            var user = new User { Id = 11, UserName = "u", Email = "e@e", PasswordHash = hashed, Role = Role.User };

            userRepo.Setup(r => r.GetUserByUserNameAsync("u")).ReturnsAsync(user);
            tokenService.Setup(t => t.GenerateToken(user.Id, user.Email, user.UserName, user.Role)).Returns("tok");
            mapper.Setup(m => m.Map<GetUserDto>(user)).Returns(new GetUserDto { Id = user.Id, UserName = user.UserName });

            var service = new UserService(userRepo.Object, tokenService.Object, inMemoryConfig, logger.Object, mapper.Object);

            var res = await service.AuthenticateAsync("u", password);

            res.Should().NotBeNull();
            res.Token.Should().Be("tok");
            res.User.Id.Should().Be(11);
        }
    }
}
