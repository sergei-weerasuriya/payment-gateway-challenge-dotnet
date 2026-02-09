using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Application.Common;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Contracts.Requests;
using PaymentGateway.Contracts.Responses;

namespace PaymentGateway.Api.Mapping;

public static class ContractMapping
{
    public static ProcessPaymentCommand ToProcessPaymentCommand(this CreatePaymentRequest request)
    {
        return new ProcessPaymentCommand
        {
            CardNumber = request.CardNumber,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Cvv = request.Cvv,
            Currency = request.Currency,
            Amount = request.Amount
        };
    }

    public static CreatePaymentResponse ToCreatePaymentResponse(this PaymentResult result)
    {
        return new CreatePaymentResponse
        {
            Id = result.Id,
            Status = result.Status,
            CardNumberLastFour = result.CardNumberLastFour,
            ExpiryMonth = result.ExpiryMonth,
            ExpiryYear = result.ExpiryYear,
            Currency = result.Currency,
            Amount = result.Amount
        };
    }

    public static GetPaymentResponse ToGetPaymentResponse(this PaymentResult result)
    {
        return new GetPaymentResponse
        {
            Id = result.Id,
            Status = result.Status,
            CardNumberLastFour = result.CardNumberLastFour,
            ExpiryMonth = result.ExpiryMonth,
            ExpiryYear = result.ExpiryYear,
            Currency = result.Currency,
            Amount = result.Amount
        };
    }

    public static ValidationProblemDetails ToProblemDetails(this PaymentRejected rejected)
    {
        return new ValidationProblemDetails(rejected.Errors)
        {
            Title = "Payment Rejected",
            Detail = rejected.Message,
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };
    }
}