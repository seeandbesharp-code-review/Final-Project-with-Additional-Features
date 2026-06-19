using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ChineseRaffleApi.Data;
using ChineseRaffleApi.Models;
using ChineseRaffleApi.Repository;

namespace ChineseRaffleApi.Tests.Repositories
{
    public class DonorRepoTests
    {
        [Fact]
        public async Task AddDonor_Then_DonorExists_ReturnsTrue()
        {
            var options = new DbContextOptionsBuilder<MyContext>().UseInMemoryDatabase(databaseName: "donor_db1").Options;

            using (var context = new MyContext(options))
            {
                var repo = new DonorRepo(context);
                var donor = new Donor { Name = "D1", Email = "d@d" };
                var id = await repo.AddDonorAsync(donor);

                var exists = await repo.DonorExistsAsync("D1");
                exists.Should().BeTrue();
            }
        }
    }
}
