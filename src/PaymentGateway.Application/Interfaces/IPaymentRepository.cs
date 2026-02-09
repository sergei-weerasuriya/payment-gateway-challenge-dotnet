using PaymentGateway.Application.DTOs;

namespace PaymentGateway.Application.Interfaces;

public interface IPaymentRepository
{
    Task<bool> CreateAsync(CreatePaymentDto paymentDto);
    Task<PaymentDto?> GetByIdAsync(Guid id, Guid merchantId);
}
