using FluentAssertions;
using PaymentGateway.Application.Idempotency;
using Xunit;

namespace PaymentGateway.Application.Tests;

public class IdempotencyServiceTests
{
    private readonly IdempotencyService _sut = new();

    private static IdempotentResponse CreateResponse(int statusCode = 200, string body = "{}")
    {
        return new IdempotentResponse
        {
            StatusCode = statusCode,
            Body = body,
            ContentType = "application/json"
        };
    }

    #region GetAsync / SetAsync

    [Fact]
    public async Task Get_WhenKeyDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Get_AfterSet_ReturnsCachedResponse()
    {
        // Arrange
        var response = CreateResponse(201, "{\"id\":\"123\"}");
        await _sut.SetAsync("key-1", response);

        // Act
        var result = await _sut.GetAsync("key-1");

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        result.Body.Should().Be("{\"id\":\"123\"}");
    }

    [Fact]
    public async Task Get_WithExpiredResponse_ReturnsNull()
    {
        // Arrange - Create a response that's already expired (>24h old)
        var expiredResponse = new IdempotentResponse
        {
            StatusCode = 200,
            Body = "{}",
            CreatedAt = DateTime.UtcNow.AddHours(-25)
        };
        await _sut.SetAsync("expired-key", expiredResponse);

        // Act
        var result = await _sut.GetAsync("expired-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Get_WithFreshResponse_ReturnsResponse()
    {
        // Arrange - Response created recently
        var freshResponse = new IdempotentResponse
        {
            StatusCode = 200,
            Body = "{}",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        await _sut.SetAsync("fresh-key", freshResponse);

        // Act
        var result = await _sut.GetAsync("fresh-key");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Set_OverwritesExistingResponse()
    {
        // Arrange
        await _sut.SetAsync("key", CreateResponse(200, "first"));
        await _sut.SetAsync("key", CreateResponse(201, "second"));

        // Act
        var result = await _sut.GetAsync("key");

        // Assert
        result!.StatusCode.Should().Be(201);
        result.Body.Should().Be("second");
    }

    #endregion

    #region Lock Acquisition

    [Fact]
    public async Task TryAcquireLock_WhenNotLocked_ReturnsTrue()
    {
        var acquired = await _sut.TryAcquireLockAsync("lock-key");

        acquired.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireLock_WhenAlreadyLocked_ReturnsFalse()
    {
        // Arrange
        await _sut.TryAcquireLockAsync("lock-key");

        // Act
        var secondAttempt = await _sut.TryAcquireLockAsync("lock-key");

        // Assert
        secondAttempt.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireLock_AfterRelease_ReturnsTrue()
    {
        // Arrange
        await _sut.TryAcquireLockAsync("lock-key");
        await _sut.ReleaseLockAsync("lock-key");

        // Act
        var acquired = await _sut.TryAcquireLockAsync("lock-key");

        // Assert
        acquired.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireLock_DifferentKeys_BothSucceed()
    {
        var first = await _sut.TryAcquireLockAsync("key-1");
        var second = await _sut.TryAcquireLockAsync("key-2");

        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseLock_WhenNotLocked_DoesNotThrow()
    {
        var act = async () => await _sut.ReleaseLockAsync("nonexistent");

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region CleanupExpired

    [Fact]
    public async Task Cleanup_RemovesExpiredResponses()
    {
        // Arrange
        var expiredResponse = new IdempotentResponse
        {
            StatusCode = 200,
            Body = "{}",
            CreatedAt = DateTime.UtcNow.AddHours(-25)
        };
        await _sut.SetAsync("expired", expiredResponse);
        await _sut.SetAsync("fresh", CreateResponse());

        // Act
        _sut.CleanupExpired();

        // Assert
        (await _sut.GetAsync("expired")).Should().BeNull();
        (await _sut.GetAsync("fresh")).Should().NotBeNull();
    }

    #endregion
}