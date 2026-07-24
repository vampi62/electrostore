using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandService;
using ElectrostoreAPI.Services.WebHookService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class WebHookServiceTests : TestBase
    {
        private const string ApiKey = "test-api-key";

        private readonly Mock<ICommandService> _commandService;

        public WebHookServiceTests()
        {
            _commandService = new Mock<ICommandService>();
        }

        private WebHookService CreateService(ApplicationDbContext context, IConfiguration configuration)
        {
            return new WebHookService(_mapper, context, configuration, _commandService.Object);
        }

        private static IConfiguration BuildConfiguration(bool track17Enable = true, string? apiKey = ApiKey)
        {
            var settings = new Dictionary<string, string?>
            {
                ["Track17:Enable"] = track17Enable.ToString()
            };
            if (apiKey != null)
            {
                settings["Track17:ApiKey"] = apiKey;
            }
            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        private static Commands BuildCommand(int id, string trackingNumber, int carrierId, TrackingStatus? lastStatus = null, TrackingSubStatus? lastSubStatus = null)
        {
            return new Commands
            {
                id_command = id,
                url_command = "https://example.com/order",
                tracking_number = trackingNumber,
                id_carrier = carrierId,
                date_command = DateTime.UtcNow,
                last_status = lastStatus,
                last_sub_status = lastSubStatus
            };
        }

        private static JsonElement BuildWebhookBody(string eventName, string trackingNumber, int carrierId, string status = "InTransit", string subStatus = "InTransit_Other")
        {
            var json = $$"""
            {
                "event": "{{eventName}}",
                "data": {
                    "number": "{{trackingNumber}}",
                    "carrier": {{carrierId}},
                    "track_info": {
                        "latest_status": { "status": "{{status}}", "sub_status": "{{subStatus}}" },
                        "latest_event": {
                            "description": "Package departed facility",
                            "location": "Paris Hub",
                            "stage": "Departure",
                            "time_utc": "2026-07-01T10:00:00Z",
                            "time_raw": { "timezone": "UTC" },
                            "address": {
                                "country": "FR", "state": "IDF", "city": "Paris", "postal_code": "75001",
                                "coordinates": { "latitude": "48.8566", "longitude": "2.3522" }
                            }
                        },
                        "shipping_info": {
                            "shipper_address": { "raw": "shipper info" },
                            "recipient_address": { "raw": "recipient info" }
                        }
                    }
                }
            }
            """;
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        private static string ComputeSignature(JsonElement body, string apiKey)
        {
            var src = JsonSerializer.Serialize(body) + "/" + apiKey;
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(src));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        // --- Process17TrackWebhook ---

        [Fact]
        public async Task Process17TrackWebhook_ShouldThrowArgumentException_WhenTrack17Disabled()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context, BuildConfiguration(track17Enable: false));
            var body = BuildWebhookBody("TRACKING_UPDATED", "TRACK1", 1);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.Process17TrackWebhook(body, "any-signature");
            });
        }

        [Fact]
        public async Task Process17TrackWebhook_ShouldThrowArgumentException_WhenApiKeyMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context, BuildConfiguration(apiKey: null));
            var body = BuildWebhookBody("TRACKING_UPDATED", "TRACK1", 1);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.Process17TrackWebhook(body, "any-signature");
            });
        }

        [Fact]
        public async Task Process17TrackWebhook_ShouldThrowArgumentException_WhenSignatureIsInvalid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context, BuildConfiguration());
            var body = BuildWebhookBody("TRACKING_UPDATED", "TRACK1", 1);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.Process17TrackWebhook(body, "wrong-signature");
            });
        }

        [Fact]
        public async Task Process17TrackWebhook_ShouldThrowArgumentException_WhenEventIsNotTrackingUpdated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context, BuildConfiguration());
            var body = BuildWebhookBody("TRACKING_STOPPED", "TRACK1", 1);
            var signature = ComputeSignature(body, ApiKey);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.Process17TrackWebhook(body, signature);
            });
        }

        [Fact]
        public async Task Process17TrackWebhook_ShouldThrowArgumentException_WhenStatusIsInvalid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context, BuildConfiguration());
            var body = BuildWebhookBody("TRACKING_UPDATED", "TRACK1", 1, status: "NotARealStatus");
            var signature = ComputeSignature(body, ApiKey);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.Process17TrackWebhook(body, signature);
            });
        }

        [Fact]
        public async Task Process17TrackWebhook_ShouldUpdateCommandAndCreateHistory_WhenStatusChanged()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1, "TRACK1", 1, lastStatus: TrackingStatus.InfoReceived, lastSubStatus: TrackingSubStatus.InfoReceived));
            await context.SaveChangesAsync();
            var service = CreateService(context, BuildConfiguration());
            var body = BuildWebhookBody("TRACKING_UPDATED", "TRACK1", 1, status: "InTransit", subStatus: "InTransit_Other");
            var signature = ComputeSignature(body, ApiKey);
            // Act
            await service.Process17TrackWebhook(body, signature);
            // Assert
            var updatedCommand = await context.Commands.FindAsync(1);
            Assert.Equal(TrackingStatus.InTransit, updatedCommand!.last_status);
            Assert.Equal(TrackingSubStatus.InTransit_Other, updatedCommand.last_sub_status);
            var history = Assert.Single(context.CommandsHistory);
            Assert.Equal(TrackingStatus.InTransit, history.status);
            Assert.Equal(1, history.id_command);
            Assert.Equal("48.8566", history.latitude);
            Assert.Equal("2.3522", history.longitude);
        }

        [Fact]
        public async Task Process17TrackWebhook_ShouldLeaveCoordinatesNull_WhenAddressHasNoCoordinates()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1, "TRACK1", 1, lastStatus: TrackingStatus.InfoReceived, lastSubStatus: TrackingSubStatus.InfoReceived));
            await context.SaveChangesAsync();
            var service = CreateService(context, BuildConfiguration());
            var json = """
            {
                "event": "TRACKING_UPDATED",
                "data": {
                    "number": "TRACK1",
                    "carrier": 1,
                    "track_info": {
                        "latest_status": { "status": "InTransit", "sub_status": "InTransit_Other" },
                        "latest_event": {
                            "description": "Package departed facility",
                            "location": "Paris Hub",
                            "stage": "Departure",
                            "time_utc": "2026-07-01T10:00:00Z",
                            "time_raw": { "timezone": "UTC" },
                            "address": { "country": "FR", "state": "IDF", "city": "Paris", "postal_code": "75001" }
                        },
                        "shipping_info": {
                            "shipper_address": { "raw": "shipper info" },
                            "recipient_address": { "raw": "recipient info" }
                        }
                    }
                }
            }
            """;
            using var doc = JsonDocument.Parse(json);
            var body = doc.RootElement.Clone();
            var signature = ComputeSignature(body, ApiKey);
            // Act
            await service.Process17TrackWebhook(body, signature);
            // Assert
            var history = Assert.Single(context.CommandsHistory);
            Assert.Null(history.latitude);
            Assert.Null(history.longitude);
        }

        [Fact]
        public async Task Process17TrackWebhook_ShouldNotCreateHistory_WhenStatusUnchanged()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1, "TRACK1", 1, lastStatus: TrackingStatus.InTransit, lastSubStatus: TrackingSubStatus.InTransit_Other));
            await context.SaveChangesAsync();
            var service = CreateService(context, BuildConfiguration());
            var body = BuildWebhookBody("TRACKING_UPDATED", "TRACK1", 1, status: "InTransit", subStatus: "InTransit_Other");
            var signature = ComputeSignature(body, ApiKey);
            // Act
            await service.Process17TrackWebhook(body, signature);
            // Assert
            Assert.Empty(context.CommandsHistory);
        }

        [Fact]
        public async Task Process17TrackWebhook_ShouldIgnoreCommands_WithDifferentTrackingNumberOrCarrier()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1, "OTHER-TRACK", 1, lastStatus: TrackingStatus.InfoReceived));
            context.Commands.Add(BuildCommand(2, "TRACK1", 999, lastStatus: TrackingStatus.InfoReceived));
            await context.SaveChangesAsync();
            var service = CreateService(context, BuildConfiguration());
            var body = BuildWebhookBody("TRACKING_UPDATED", "TRACK1", 1, status: "InTransit");
            var signature = ComputeSignature(body, ApiKey);
            // Act
            await service.Process17TrackWebhook(body, signature);
            // Assert
            Assert.Empty(context.CommandsHistory);
            var unchangedCommand1 = await context.Commands.FindAsync(1);
            var unchangedCommand2 = await context.Commands.FindAsync(2);
            Assert.Equal(TrackingStatus.InfoReceived, unchangedCommand1!.last_status);
            Assert.Equal(TrackingStatus.InfoReceived, unchangedCommand2!.last_status);
        }
    }
}
