FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PaymentGateway.sln .
COPY src/PaymentGateway.Api/PaymentGateway.Api.csproj src/PaymentGateway.Api/
COPY src/PaymentGateway.Application/PaymentGateway.Application.csproj src/PaymentGateway.Application/
COPY src/PaymentGateway.Contracts/PaymentGateway.Contracts.csproj src/PaymentGateway.Contracts/
COPY src/PaymentGateway.Client/PaymentGateway.Client.csproj src/PaymentGateway.Client/
COPY test/PaymentGateway.Api.Tests/PaymentGateway.Api.Tests.csproj test/PaymentGateway.Api.Tests/
COPY test/PaymentGateway.Application.Tests/PaymentGateway.Application.Tests.csproj test/PaymentGateway.Application.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/PaymentGateway.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentGateway.Api.dll"]
