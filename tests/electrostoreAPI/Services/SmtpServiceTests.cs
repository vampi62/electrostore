using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using AutoMapper;
using System.Net;
using System.Net.Mail;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.SmtpService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class SmtpServiceTests
    {
        [Fact]
        public async Task SendEmailAsync_ShouldSendEmail_WhenParametersAreValid()
        {
            // Arrange
            var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            mockConfiguration.Setup(c => c["SMTP:Enable"]).Returns("true");
            mockConfiguration.Setup(c => c["SMTP:Host"]).Returns("smtp.test.com");
            mockConfiguration.Setup(c => c["SMTP:Port"]).Returns("587");
            mockConfiguration.Setup(c => c["SMTP:Username"]).Returns("testUser@test.com");
            mockConfiguration.Setup(c => c["SMTP:Password"]).Returns("testPassword");
            
            var mockSmtpService = new Mock<SmtpService>(mockConfiguration.Object) { CallBase = true };
            mockSmtpService.Setup(s => s.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.CompletedTask);

            var to = "testUser@test.com";
            var subject = "Test Subject";
            var body = "<h1>Test Body</h1>";
            
            // Act
            await mockSmtpService.Object.SendEmailAsync(to, subject, body);
            
            // Assert
            mockSmtpService.Verify(s => s.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }
    }
}