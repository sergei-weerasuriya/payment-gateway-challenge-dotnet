using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentGateway.Application.BankClient;
using PaymentGateway.Application.Common;
using PaymentGateway.Contracts.Models;
using PaymentGateway.Contracts.Requests;
using PaymentGateway.Contracts.Responses;

namespace PaymentGateway.Api.Tests;

// Integration tests for the PaymentsController. Tests the full HTTP pipeline including validation, bank communication, and storage.
public class PaymentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    #region Create Payment Tests

    [Fact]
    public async Task CreatePayment_WithValidRequest_ReturnsAuthorizedPayment()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankResponse { Authorized = true, AuthorizationCode = "AUTH123" });

        var client = CreateClientWithMockBank(mockBankClient.Object);

        var request = CreateValidPaymentRequest(cardNumber: "4111111111111111"); // Odd ending = authorized

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        paymentResponse.Should().NotBeNull();
        paymentResponse!.Status.Should().Be(PaymentStatus.Authorized);
        paymentResponse.Amount.Should().Be(request.Amount);
        paymentResponse.Currency.Should().Be(request.Currency.ToUpperInvariant());
        paymentResponse.CardNumberLastFour.Should().Be("1111");
        paymentResponse.ExpiryMonth.Should().Be(request.ExpiryMonth);
        paymentResponse.ExpiryYear.Should().Be(request.ExpiryYear);
    }

    [Fact]
    public async Task CreatePayment_WithDeclinedCard_ReturnsDeclinedPayment()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankResponse { Authorized = false, AuthorizationCode = "" });

        var client = CreateClientWithMockBank(mockBankClient.Object);

        var request = CreateValidPaymentRequest(cardNumber: "4111111111111112"); // Even ending = declined

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        paymentResponse.Should().NotBeNull();
        paymentResponse!.Status.Should().Be(PaymentStatus.Declined);
    }

    [Fact]
    public async Task CreatePayment_WithBankUnavailable_ReturnsBadRequest()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentRejected.BankUnavailable());

        var client = CreateClientWithMockBank(mockBankClient.Object);

        var request = CreateValidPaymentRequest();

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("123", "Card number must be between 14 and 19 characters")]
    [InlineData("123456789012345678901", "Card number must be between 14 and 19 characters")]
    [InlineData("1234567890123A", "Card number must contain only numeric characters")]
    public async Task CreatePayment_WithInvalidCardNumber_ReturnsBadRequest(string cardNumber, string expectedError)
    {
        // Arrange
        var client = CreateClientWithMockBank(Mock.Of<IBankClient>());
        var request = CreateValidPaymentRequest(cardNumber: cardNumber);

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(expectedError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public async Task CreatePayment_WithInvalidExpiryMonth_ReturnsBadRequest(int expiryMonth)
    {
        // Arrange
        var client = CreateClientWithMockBank(Mock.Of<IBankClient>());
        var request = CreateValidPaymentRequest(expiryMonth: expiryMonth);

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_WithExpiredCard_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClientWithMockBank(Mock.Of<IBankClient>());
        var request = CreateValidPaymentRequest(expiryMonth: 1, expiryYear: 2020);

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("XXX")]
    public async Task CreatePayment_WithInvalidCurrency_ReturnsBadRequest(string currency)
    {
        // Arrange
        var client = CreateClientWithMockBank(Mock.Of<IBankClient>());
        var request = CreateValidPaymentRequest(currency: currency);

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreatePayment_WithInvalidAmount_ReturnsBadRequest(int amount)
    {
        // Arrange
        var client = CreateClientWithMockBank(Mock.Of<IBankClient>());
        var request = CreateValidPaymentRequest(amount: amount);

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12A")]
    public async Task CreatePayment_WithInvalidCvv_ReturnsBadRequest(string cvv)
    {
        // Arrange
        var client = CreateClientWithMockBank(Mock.Of<IBankClient>());
        var request = CreateValidPaymentRequest(cvv: cvv);

        // Act
        var response = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = CreateValidPaymentRequest();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Api-Key", HttpClientExtensions.DefaultApiKey);
        // Intentionally not adding Idempotency-Key header

        // Act
        var response = await client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Payment Tests

    [Fact]
    public async Task GetPayment_WhenPaymentExists_ReturnsPayment()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankResponse { Authorized = true, AuthorizationCode = "AUTH123" });

        var client = CreateClientWithMockBank(mockBankClient.Object);
        var request = CreateValidPaymentRequest();

        // Create a payment first
        var createResponse = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);
        var createdPayment = await createResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Act
        var getResponse = await client.GetWithApiKeyAsync($"/api/payments/{createdPayment!.Id}");
        var retrievedPayment = await getResponse.Content.ReadFromJsonAsync<GetPaymentResponse>();

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        retrievedPayment.Should().NotBeNull();
        retrievedPayment!.Id.Should().Be(createdPayment.Id);
        retrievedPayment.Status.Should().Be(createdPayment.Status);
        retrievedPayment.Amount.Should().Be(createdPayment.Amount);
    }

    [Fact]
    public async Task GetPayment_WhenPaymentDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetWithApiKeyAsync($"/api/payments/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_ReturnsIdempotentReplay()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankResponse { Authorized = true, AuthorizationCode = "AUTH123" });

        var client = CreateClientWithMockBank(mockBankClient.Object);
        var request = CreateValidPaymentRequest();
        var idempotencyKey = Guid.NewGuid().ToString();

        // First request
        var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        firstRequest.Headers.Add("X-Api-Key", HttpClientExtensions.DefaultApiKey);
        var firstResponse = await client.SendAsync(firstRequest);
        var firstPayment = await firstResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Second request with same key
        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        secondRequest.Headers.Add("X-Api-Key", HttpClientExtensions.DefaultApiKey);
        var secondResponse = await client.SendAsync(secondRequest);
        var secondPayment = await secondResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.Headers.Contains("X-Idempotent-Replay").Should().BeTrue();
        secondResponse.Headers.Location.Should().Be(firstResponse.Headers.Location);
        secondPayment!.Id.Should().Be(firstPayment!.Id);

        // Verify bank was only called once
        mockBankClient.Verify(
            x => x.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task CreatePayment_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = CreateValidPaymentRequest();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        // Intentionally not adding X-Api-Key header

        // Act
        var response = await client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePayment_WithInvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = CreateValidPaymentRequest();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        httpRequest.Headers.Add("X-Api-Key", "invalid-key");

        // Act
        var response = await client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPayment_AsDifferentMerchant_ReturnsNotFound()
    {
        // Arrange - Create payment as merchant 1
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankResponse { Authorized = true, AuthorizationCode = "AUTH123" });

        var client = CreateClientWithMockBank(mockBankClient.Object);
        var request = CreateValidPaymentRequest();

        var createResponse = await client.PostAsJsonWithIdempotencyAsync("/api/payments", request);
        var createdPayment = await createResponse.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        // Act - Try to retrieve it as merchant 2
        var getResponse = await client.GetWithApiKeyAsync(
            $"/api/payments/{createdPayment!.Id}",
            apiKey: HttpClientExtensions.SecondMerchantApiKey);

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private HttpClient CreateClientWithMockBank(IBankClient mockBankClient)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real bank client registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBankClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add mock bank client
                services.AddSingleton(mockBankClient);
            });
        }).CreateClient();
    }

    private static CreatePaymentRequest CreateValidPaymentRequest(
        string? cardNumber = null,
        int? expiryMonth = null,
        int? expiryYear = null,
        string? cvv = null,
        string? currency = null,
        int? amount = null)
    {
        var now = DateTime.UtcNow;

        return new CreatePaymentRequest
        {
            CardNumber = cardNumber ?? "4111111111111111",
            ExpiryMonth = expiryMonth ?? now.Month,
            ExpiryYear = expiryYear ?? now.Year + 1,
            Cvv = cvv ?? "123",
            Currency = currency ?? "USD",
            Amount = amount ?? 1050
        };
    }

    #endregion
}