using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CarrierService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CarrierServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService = new();

        public CarrierServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private CarrierService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object);

        private static Carriers BuildCarrier(int key, string name = "carrier") => new()
        {
            key_carrier = key,
            name_carrier = name
        };

        // --- GetCarriers ---

        [Fact]
        public async Task GetCarriers_ShouldReturnAllCarriers()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.AddRange(BuildCarrier(1, "a"), BuildCarrier(2, "b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCarriers();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        [Fact]
        public async Task GetCarriers_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildCarrier(1, "a");
            var b = BuildCarrier(2, "b");
            context.Carriers.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCarriers(rsql: [new FilterDto { field = "id_carrier", search_type = "eq", value = b.id_carrier.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_carrier, single.id_carrier);
        }

        [Fact]
        public async Task GetCarriers_ShouldApplyLimitAndOffset()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.AddRange(BuildCarrier(1), BuildCarrier(2), BuildCarrier(3));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCarriers(limit: 1, offset: 1);

            Assert.Equal(3, result.pagination.total);
            Assert.Single(result.data);
        }

        // --- GetCarrierById ---

        [Fact]
        public async Task GetCarrierById_ShouldReturnCarrier_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var carrier = BuildCarrier(7, "ups");
            context.Carriers.Add(carrier);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCarrierById(carrier.id_carrier);

            Assert.Equal("ups", result.name_carrier);
            Assert.Equal(7, result.key_carrier);
        }

        [Fact]
        public async Task GetCarrierById_ShouldThrowKeyNotFoundException_WhenCarrierDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCarrierById(999));
        }

        // --- CreateCarrier ---

        [Fact]
        public async Task CreateCarrier_ShouldPersistCarrier_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var result = await service.CreateCarrier(new CreateCarrierDto { key_carrier = 11, name_carrier = "dhl" });

            Assert.Equal("dhl", result.name_carrier);
            Assert.Equal(1, await context.Carriers.CountAsync());
        }

        [Theory]
        [InlineData(UserRole.User)]
        [InlineData(UserRole.Moderator)]
        public async Task CreateCarrier_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin(UserRole role)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateCarrier(new CreateCarrierDto { key_carrier = 1 }));
            Assert.Equal(0, await context.Carriers.CountAsync());
        }

        // --- CreateFirstCarrier ---

        [Fact]
        public async Task CreateFirstCarrier_ShouldPersistCarrier_WithoutRoleCheck()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            var result = await service.CreateFirstCarrier(new CreateCarrierDto { key_carrier = 3, name_carrier = "seed", email_carrier = "a@b.c" });

            Assert.Equal("seed", result.name_carrier);
            Assert.Equal(1, await context.Carriers.CountAsync());
        }

        // --- UpdateCarrier ---

        [Fact]
        public async Task UpdateCarrier_ShouldUpdateAllProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var carrier = BuildCarrier(1, "old");
            context.Carriers.Add(carrier);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateCarrier(carrier.id_carrier, new UpdateCarrierDto
            {
                country_carrier = 33,
                country_iso_carrier = "FR",
                email_carrier = "contact@carrier.test",
                tel_carrier = "+33100000000",
                url_carrier = "https://carrier.test",
                name_carrier = "new"
            });

            Assert.Equal(33, result.country_carrier);
            Assert.Equal("FR", result.country_iso_carrier);
            Assert.Equal("contact@carrier.test", result.email_carrier);
            Assert.Equal("+33100000000", result.tel_carrier);
            Assert.Equal("https://carrier.test", result.url_carrier);
            Assert.Equal("new", result.name_carrier);
        }

        [Fact]
        public async Task UpdateCarrier_ShouldKeepValues_WhenDtoIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var carrier = BuildCarrier(1, "keep");
            context.Carriers.Add(carrier);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateCarrier(carrier.id_carrier, new UpdateCarrierDto());

            Assert.Equal("keep", result.name_carrier);
        }

        [Fact]
        public async Task UpdateCarrier_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Moderator);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateCarrier(1, new UpdateCarrierDto()));
        }

        [Fact]
        public async Task UpdateCarrier_ShouldThrowKeyNotFoundException_WhenCarrierDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCarrier(999, new UpdateCarrierDto()));
        }

        // --- DeleteCarrier ---

        [Fact]
        public async Task DeleteCarrier_ShouldRemoveCarrier_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var carrier = BuildCarrier(1);
            context.Carriers.Add(carrier);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteCarrier(carrier.id_carrier);

            Assert.Equal(0, await context.Carriers.CountAsync());
        }

        [Fact]
        public async Task DeleteCarrier_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteCarrier(1));
        }

        [Fact]
        public async Task DeleteCarrier_ShouldThrowKeyNotFoundException_WhenCarrierDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteCarrier(999));
        }
    }
}
