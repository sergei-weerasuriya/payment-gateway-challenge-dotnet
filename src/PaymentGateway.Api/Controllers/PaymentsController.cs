using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Filters;
using PaymentGateway.Api.Mapping;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Contracts.Requests;
using PaymentGateway.Contracts.Responses;

namespace PaymentGateway.Api.Controllers;

[ApiController]
[ApiKeyAuth]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }
    
    [HttpPost(ApiEndpoints.Payments.Create)]
    [Idempotent]
    [ProducesResponseType(typeof(CreatePaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var merchant = HttpContext.GetMerchant();
        var processPaymentCommand = request.ToProcessPaymentCommand();
        var result = await _paymentService.ProcessPaymentAsync(processPaymentCommand, merchant.Id);

        return result.Match<IActionResult>(
            paymentResult => CreatedAtAction(nameof(Get), new { id = paymentResult.Id }, paymentResult.ToCreatePaymentResponse()),
            rejected => BadRequest(rejected.ToProblemDetails())
        );
    }
    
    [HttpGet(ApiEndpoints.Payments.Get)]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var merchant = HttpContext.GetMerchant();
        var payment = await _paymentService.GetPaymentAsync(id, merchant.Id);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(payment.ToGetPaymentResponse());
    }
}