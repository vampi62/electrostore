using electrostore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace electrostoreAPI.Tests.Middleware
{
    public class ExceptionsHandlerTests
    {
        private readonly Mock<ILogger<ExceptionsHandler>> _mockLogger;
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly ExceptionsHandler _middleware;
        private readonly DefaultHttpContext _context;

        public ExceptionsHandlerTests()
        {
            _mockLogger = new Mock<ILogger<ExceptionsHandler>>();
            _mockNext = new Mock<RequestDelegate>();
            _middleware = new ExceptionsHandler(_mockNext.Object, _mockLogger.Object);
            _context = new DefaultHttpContext();
            _context.Response.Body = new MemoryStream();
        }

        [Fact]
        public async Task InvokeAsync_WhenNoException_ShouldCallNext()
        {
            // Arrange
            _mockNext.Setup(x => x(_context)).Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            _mockNext.Verify(x => x(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenArgumentException_ShouldReturn400BadRequest()
        {
            // Arrange
            var exception = new ArgumentException("Test argument exception");
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, _context.Response.StatusCode);
            Assert.Equal("application/json", _context.Response.ContentType);

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var expectedResponse = JsonSerializer.Serialize(new { error = exception.Message });
            Assert.Equal(expectedResponse, responseBody);

            // Verify that ArgumentException is not logged as an error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WhenKeyNotFoundException_ShouldReturn404NotFound()
        {
            // Arrange
            var exception = new KeyNotFoundException("Test key not found exception");
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, _context.Response.StatusCode);
            Assert.Equal("application/json", _context.Response.ContentType);

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var expectedResponse = JsonSerializer.Serialize(new { error = exception.Message });
            Assert.Equal(expectedResponse, responseBody);

            // Verify that KeyNotFoundException is not logged as an error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WhenInvalidOperationException_ShouldReturn409Conflict()
        {
            // Arrange
            var exception = new InvalidOperationException("Test invalid operation exception");
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.Equal(StatusCodes.Status409Conflict, _context.Response.StatusCode);
            Assert.Equal("application/json", _context.Response.ContentType);

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var expectedResponse = JsonSerializer.Serialize(new { error = exception.Message });
            Assert.Equal(expectedResponse, responseBody);

            // Verify that InvalidOperationException is not logged as an error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WhenUnauthorizedAccessException_ShouldReturn401Unauthorized()
        {
            // Arrange
            var exception = new UnauthorizedAccessException("Test unauthorized exception");
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, _context.Response.StatusCode);
            Assert.Equal("application/json", _context.Response.ContentType);

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var expectedResponse = JsonSerializer.Serialize(new { error = exception.Message });
            Assert.Equal(expectedResponse, responseBody);

            // Verify that UnauthorizedAccessException is logged as an error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenNotImplementedException_ShouldReturn501NotImplemented()
        {
            // Arrange
            var exception = new NotImplementedException("Test not implemented exception");
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.Equal(StatusCodes.Status501NotImplemented, _context.Response.StatusCode);
            Assert.Equal("application/json", _context.Response.ContentType);

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var expectedResponse = JsonSerializer.Serialize(new { error = exception.Message });
            Assert.Equal(expectedResponse, responseBody);

            // Verify that NotImplementedException is logged as an error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenHttpRequestException_ShouldReturn502BadGateway()
        {
            // Arrange
            var exception = new HttpRequestException("Test HTTP request exception");
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.Equal(StatusCodes.Status502BadGateway, _context.Response.StatusCode);
            Assert.Equal("application/json", _context.Response.ContentType);

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var expectedResponse = JsonSerializer.Serialize(new { error = exception.Message });
            Assert.Equal(expectedResponse, responseBody);

            // Verify that HttpRequestException is logged as an error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenGenericException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var exception = new Exception("Test generic exception");
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, _context.Response.StatusCode);
            Assert.Equal("application/json", _context.Response.ContentType);

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var expectedResponse = JsonSerializer.Serialize(new { error = exception.Message });
            Assert.Equal(expectedResponse, responseBody);

            // Verify that generic exception is logged as an error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest)]
        [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized)]
        [InlineData(typeof(KeyNotFoundException), StatusCodes.Status404NotFound)]
        [InlineData(typeof(InvalidOperationException), StatusCodes.Status409Conflict)]
        [InlineData(typeof(NotImplementedException), StatusCodes.Status501NotImplemented)]
        [InlineData(typeof(HttpRequestException), StatusCodes.Status502BadGateway)]
        public async Task InvokeAsync_WithVariousExceptions_ShouldReturnCorrectStatusCode(Type exceptionType, int expectedStatusCode)
        {
            // Arrange
            var exception = (Exception)Activator.CreateInstance(exceptionType, "Test exception message");
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            Assert.Equal(expectedStatusCode, _context.Response.StatusCode);
            Assert.Equal("application/json", _context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WhenExceptionWithSpecialCharacters_ShouldSerializeCorrectly()
        {
            // Arrange
            var exceptionMessage = "Test exception with special characters: éàü\"\\";
            var exception = new ArgumentException(exceptionMessage);
            _mockNext.Setup(x => x(_context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(_context);

            // Assert
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var expectedResponse = JsonSerializer.Serialize(new { error = exceptionMessage });
            Assert.Equal(expectedResponse, responseBody);
        }
    }
}