using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PaymentGateway.Application.BankClient;
using Xunit;

namespace PaymentGateway.Application.Tests;

public class BankClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<BankClient.BankClient>> _loggerMock;
    private readonly BankClient.BankClient _sut;

    public BankClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:9080")
        };
        _loggerMock = new Mock<ILogger<BankClient.BankClient>>();
        _sut = new BankClient.BankClient(_httpClient, _loggerMock.Object);
    }

    private static BankRequest CreateValidRequest() => new()
    {
        CardNumber = "4111111111111111",
        ExpiryDate = "12/2027",
        Currency = "USD",
        Amount = 1000,
        Cvv = "123"
    };

    #region Successful Responses

    [Fact]
    public async Task AuthorizePayment_WhenBankAuthorizes_ReturnsSuccess()
    {
        // Arrange
        var bankResponse = new BankResponse { Authorized = true, AuthorizationCode = "AUTH123" };
        SetupHandler(HttpStatusCode.OK, bankResponse);

        // Act
        var result = await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.GetValueOrThrow();
        response.Authorized.Should().BeTrue();
        response.AuthorizationCode.Should().Be("AUTH123");
    }

    [Fact]
    public async Task AuthorizePayment_WhenBankDeclines_ReturnsDeclinedResponse()
    {
        // Arrange
        var bankResponse = new BankResponse { Authorized = false, AuthorizationCode = "" };
        SetupHandler(HttpStatusCode.OK, bankResponse);

        // Act
        var result = await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.GetValueOrThrow().Authorized.Should().BeFalse();
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task AuthorizePayment_WhenBankReturns503_ReturnsFailure()
    {
        // Arrange
        SetupHandler(HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Match(
            _ => throw new Exception("Expected failure"),
            rejected => rejected.Message.Should().Contain("unavailable"));
    }

    [Fact]
    public async Task AuthorizePayment_WhenBankReturnsNonSuccess_ReturnsFailure()
    {
        // Arrange
        SetupHandler(HttpStatusCode.InternalServerError);

        // Act
        var result = await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizePayment_WhenBankReturns400_ReturnsFailure()
    {
        // Arrange
        SetupHandler(HttpStatusCode.BadRequest);

        // Act
        var result = await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizePayment_WhenResponseBodyIsNull_ReturnsFailure()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };
        SetupHandlerRaw(response);

        // Act
        var result = await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizePayment_WhenHttpRequestException_ReturnsFailure()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizePayment_WhenTimeout_ReturnsFailure()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout", new TimeoutException()));

        // Act
        var result = await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Request Verification

    [Fact]
    public async Task AuthorizePayment_SendsPostToPaymentsEndpoint()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new BankResponse { Authorized = true, AuthorizationCode = "X" })
            });

        // Act
        await _sut.AuthorizePaymentAsync(CreateValidRequest());

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.PathAndQuery.Should().Be("/payments");
    }

    #endregion

    #region Helpers

    private void SetupHandler(HttpStatusCode statusCode, BankResponse? body = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (body is not null)
        {
            response.Content = JsonContent.Create(body);
        }
        SetupHandlerRaw(response);
    }

    private void SetupHandlerRaw(HttpResponseMessage response)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    #endregion
}