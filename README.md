# Payment Gateway
An API-based payment gateway that allows merchants to process card payments and retrieve payment details.

## Running the Application

### Docker
```bash
docker-compose up -d
```

This starts all services:

| Service | URL |
|---------|-----|
| Payment Gateway API | http://localhost:5067 |
| Swagger UI | http://localhost:5067/swagger |
| Bank Simulator | http://localhost:9080 |

### Local Development
```bash
# Start the bank simulator
docker-compose up -d bank_simulator

# Run the API
dotnet run --project src/PaymentGateway.Api
```

### Running Tests
```bash
dotnet test
```

## API Endpoints
All endpoints require the `X-Api-Key` header for merchant authentication.

### Process a Payment
POST /api/payments
Requires an `Idempotency-Key` header (GUID) to prevent duplicate charges.

**Request body:**
```json
{
  "cardNumber": "2222405343248877",
  "expiryMonth": 4,
  "expiryYear": 2025,
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}
```

**Responses:**
- `201 Created` - Payment processed (status will be `Authorized` or `Declined`)
- `400 Bad Request` - Validation failed or bank unavailable
- `409 Conflict` - Idempotency key reused with different request body

### Retrieve a Payment
GET /api/payments/{id}

**Responses:**
- `200 OK` - Payment details (card number masked to last 4 digits)
- `404 Not Found` - Payment doesn't exist or belongs to a different merchant

## Project Structure
src/
  PaymentGateway.Api            Web API layer (controllers, filters, middleware)
  PaymentGateway.Application    Business logic (services, validation, bank client)
  PaymentGateway.Contracts      Shared request/response models and enums
  PaymentGateway.Client         Typed HTTP client SDK for consuming the API
test/
  PaymentGateway.Api.Tests          Integration tests (WebApplicationFactory)
  PaymentGateway.Application.Tests  Unit tests (services, validation, bank client)


## Design Decisions

**Layered architecture.** The solution separates API concerns (routing, filters, serialization) from business logic (validation, bank communication, storage). The Contracts project is shared between the API and client SDK so consumers work with the same types. This structure felt adequate to fulfil the requirements laid out in the assessment.

**FluentValidation over data annotations.** The validation rules require composite logic (e.g., expiry month + year together determine if a card is expired). FluentValidation handles this naturally with `Must()` rules, whereas data annotations would require custom attributes for the same thing.

**Result pattern for bank responses.** `ProcessPaymentAsync` returns `Result<PaymentResult, PaymentRejected>` instead of throwing exceptions for expected outcomes like bank unavailability or declined payments. This makes the control flow explicit.

**Encryption at rest.** Card numbers and CVVs are AES-encrypted before storage (I know the current implementation is in-memory, but once this is swapped out the encryption would just work as is).

**Idempotency via `Idempotency-Key` header.** Payment processing is not inherently idempotent, network retries could create duplicate charges. The idempotency filter captures the response on first execution and replays it for subsequent requests with the same key. Implemented as an action filter.

**API key authentication.** Each merchant authenticates via `X-Api-Key` header. Payments are scoped to the authenticated merchant. A merchant can only retrieve their own payments. Implemented as an action filter rather than middleware so it only applies to controller actions that opt in.

**Typed HTTP client for bank communication.** `BankClient` uses `IHttpClientFactory` via `AddHttpClient<IBankClient, BankClient>()`, which manages `HttpMessageHandler` pooling and avoids socket exhaustion.

## Assumptions

- **Supported currencies** are limited to USD, GBP, and EUR. The validator rejects other curency codes. In production this would be configurable per merchant.
- **Expiry date validation** uses UTC. A card expiring in Februaury 2026 is valid for the entire month (compared against current month, not day).
- **Amount must be strictly positive.** Zero-value authorizations are not supported.
- **Declined payments are stored.** A declined payment was processed by the bank and has a meaningful status. It makes sense to record it as it can be retrieved later. Only rejected payments (validation failures that never reach the bank) are not stored.
- **Merchant API keys are hardcoded** in an in-memory repository for the exercise. In production, these would come from a database.

## Production Considerations

If this were a real payment gateway, I would add:

- **Persistent storage** (PostgreSQL or similar) behind the existing repository interface
- **Distributed idempotency** using Redis with TTL-based expiry
- **Secret management** (Azure Key Vault, AWS Secrets Manager) for encryption keys and API credentials instead of `appsettings.json`
- **Rate limiting** per merchant to prevent abuse
- **Re-tries & Circuit breaker** (Polly) on the bank client
- **Audit logging** I would use Event Sourcing
