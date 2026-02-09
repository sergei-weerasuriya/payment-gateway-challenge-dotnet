using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PaymentGateway.Api.Filters;

public sealed class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasIdempotentAttribute = context.MethodInfo.GetCustomAttributes(typeof(IdempotentAttribute), inherit: true).Length > 0;
        if (!hasIdempotentAttribute)
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IdempotencyFilter.IdempotencyKeyHeader,
            In = ParameterLocation.Header,
            Required = true,
            Description = "A unique key to ensure idempotent processing of the request. Must be 64 characters or less.",
            Schema = new OpenApiSchema { Type = "string", MaxLength = 64 }
        });
    }
}