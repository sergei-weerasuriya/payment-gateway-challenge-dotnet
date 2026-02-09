using FluentAssertions;
using PaymentGateway.Application.Common;
using Xunit;

namespace PaymentGateway.Application.Tests;

public class ResultTests
{
    #region Factory Methods

    [Fact]
    public void Success_CreatesSuccessResult()
    {
        var result = Result<string, string>.Success("ok");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_CreatesFailureResult()
    {
        var result = Result<string, string>.Failure("error");

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region Match (Func)

    [Fact]
    public void Match_OnSuccess_ExecutesSuccessBranch()
    {
        var result = Result<int, string>.Success(42);

        var output = result.Match(
            value => $"value:{value}",
            error => $"error:{error}");

        output.Should().Be("value:42");
    }

    [Fact]
    public void Match_OnFailure_ExecutesFailureBranch()
    {
        var result = Result<int, string>.Failure("bad");

        var output = result.Match(
            value => $"value:{value}",
            error => $"error:{error}");

        output.Should().Be("error:bad");
    }

    #endregion

    #region Match (Action)

    [Fact]
    public void MatchAction_OnSuccess_ExecutesSuccessAction()
    {
        var result = Result<int, string>.Success(42);
        var successCalled = false;
        var failureCalled = false;

        result.Match(
            _ => successCalled = true,
            _ => failureCalled = true);

        successCalled.Should().BeTrue();
        failureCalled.Should().BeFalse();
    }

    [Fact]
    public void MatchAction_OnFailure_ExecutesFailureAction()
    {
        var result = Result<int, string>.Failure("bad");
        var successCalled = false;
        var failureCalled = false;

        result.Match(
            _ => successCalled = true,
            _ => failureCalled = true);

        successCalled.Should().BeFalse();
        failureCalled.Should().BeTrue();
    }

    #endregion

    #region GetValueOrThrow

    [Fact]
    public void GetValueOrThrow_OnSuccess_ReturnsValue()
    {
        var result = Result<int, string>.Success(42);

        result.GetValueOrThrow().Should().Be(42);
    }

    [Fact]
    public void GetValueOrThrow_OnFailure_ThrowsInvalidOperationException()
    {
        var result = Result<int, string>.Failure("bad");

        var act = () => result.GetValueOrThrow();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    #endregion

    #region GetValueOrDefault

    [Fact]
    public void GetValueOrDefault_OnSuccess_ReturnsValue()
    {
        var result = Result<int, string>.Success(42);

        result.GetValueOrDefault(-1).Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_ReturnsDefault()
    {
        var result = Result<int, string>.Failure("bad");

        result.GetValueOrDefault(-1).Should().Be(-1);
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_WithNoDefault_ReturnsTypeDefault()
    {
        var result = Result<int, string>.Failure("bad");

        result.GetValueOrDefault().Should().Be(0);
    }

    #endregion

    #region Implicit Conversions

    [Fact]
    public void ImplicitConversion_FromSuccessValue_CreatesSuccess()
    {
        Result<string, int> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.GetValueOrThrow().Should().Be("hello");
    }

    [Fact]
    public void ImplicitConversion_FromFailureValue_CreatesFailure()
    {
        Result<string, int> result = 404;

        result.IsFailure.Should().BeTrue();
        result.Match(_ => -1, error => error).Should().Be(404);
    }

    #endregion
}