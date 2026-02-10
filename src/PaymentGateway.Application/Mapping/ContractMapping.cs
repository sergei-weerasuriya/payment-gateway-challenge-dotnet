using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Models;

namespace PaymentGateway.Application.Mapping;

public static class ContractMapping
{
    public static CreatePaymentDto ToCreatePaymentDto(this Payment payment)
    {
        return new CreatePaymentDto
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumber = payment.Card.Number,
            CardLastFour = payment.Card.GetLastFourDigits(),
            ExpiryMonth = payment.Card.ExpiryMonth,
            ExpiryYear = payment.Card.ExpiryYear,
            Cvv = payment.Card.Cvv,
            Currency = payment.Currency,
            Amount = payment.Amount,
            AuthorizationCode = payment.AuthorizationCode,
            CreatedAt = payment.CreatedAt,
            MerchantId = payment.MerchantId
        };
    }

    public static PaymentResult ToPaymentResult(this Payment payment)
    {
        return new PaymentResult
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.Card.GetLastFourDigits(),
            ExpiryMonth = payment.Card.ExpiryMonth,
            ExpiryYear = payment.Card.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        };
    }

    public static PaymentResult ToPaymentResult(this PaymentDto dto)
    {
        return new PaymentResult
        {
            Id = dto.Id,
            Status = dto.Status,
            CardNumberLastFour = dto.CardLastFour,
            ExpiryMonth = dto.ExpiryMonth,
            ExpiryYear = dto.ExpiryYear,
            Currency = dto.Currency,
            Amount = dto.Amount
        };
    }
}
