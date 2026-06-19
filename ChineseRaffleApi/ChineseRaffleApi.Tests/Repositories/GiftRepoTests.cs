using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ChineseRaffleApi.Data;
using ChineseRaffleApi.Models;
using ChineseRaffleApi.Repository;

namespace ChineseRaffleApi.Tests.Repositories
{
    public class GiftRepoTests
    {
        [Fact]
        public async Task AddAndGetGift_ById_ShouldReturnSame()
        {
            var options = new DbContextOptionsBuilder<MyContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new MyContext(options))
            {
                var donor = new Donor { Id = 1, Name = "תורם בדיקה" };
                context.Donors.Add(donor);
                await context.SaveChangesAsync();

                var repo = new GiftRepo(context);
                var gift = new Gift { Title = "ספר", TicketPrice = 10, DonorId = 1, CategoryId = 1 };

                await repo.AddGiftAsync(gift);
                var fetched = await repo.GetGiftByIdAsync(gift.Id);

                fetched.Should().NotBeNull("המתנה אמורה להימצא יחד עם התורם שלה");
                fetched.Title.Should().Be("ספר");
                fetched.Donor.Should().NotBeNull("ה-Include אמור לשלוף גם את התורם");
            }
        }
    }
}
