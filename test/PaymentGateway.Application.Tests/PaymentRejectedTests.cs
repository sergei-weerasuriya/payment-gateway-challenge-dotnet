using FluentAssertions;
using PaymentGateway.Application.Common;
using Xunit;

namespace PaymentGateway.Application.Tests;

public class PaymentRejectedTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var rejected = new PaymentRejected("Something went wrong");

        rejected.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public void Constructor_WithNullErrors_InitializesEmptyDictionary()
    {
        var rejected = new PaymentRejected("error", errors: null);

        rejected.Errors.Should().NotBeNull();
        rejected.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithErrors_SetsErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Field1", new[] { "Error 1" } }
        };

        var rejected = new PaymentRejected("error", errors);

        rejected.Errors.Should().ContainKey("Field1");
        rejected.Errors["Field1"].Should().Contain("Error 1");
    }

    [Fact]
    public void FromFieldError_CreatesSingleFieldError()
    {
        var rejected = PaymentRejected.FromFieldError("CardNumber", "Card is invalid.");

        rejected.Message.Should().Contain("validation errors");
        rejected.Errors.Should().HaveCount(1);
        rejected.Errors.Should().ContainKey("CardNumber");
        rejected.Errors["CardNumber"].Should().ContainSingle("Card is invalid.");
    }

    [Fact]
    public void FromFieldErrors_CreatesMultipleFieldErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "CardNumber", new[] { "Invalid card" } },
            { "Cvv", new[] { "Too short", "Must be numeric" } }
        };

        var rejected = PaymentRejected.FromFieldErrors(errors);

        rejected.Message.Should().Contain("validation errors");
        rejected.Errors.Should().HaveCount(2);
        rejected.Errors["Cvv"].Should().HaveCount(2);
    }

    [Fact]
    public void BankUnavailable_CreatesCorrectMessage()
    {
        var rejected = PaymentRejected.BankUnavailable();

        rejected.Message.Should().Contain("bank");
        rejected.Message.Should().Contain("unavailable");
        rejected.Errors.Should().BeEmpty();
    }
}