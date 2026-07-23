# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/InnovationToImpact.Domain/InnovationToImpact.Domain.csproj src/InnovationToImpact.Domain/
COPY src/InnovationToImpact.Infrastructure/InnovationToImpact.Infrastructure.csproj src/InnovationToImpact.Infrastructure/
COPY src/InnovationToImpact.Api/InnovationToImpact.Api.csproj src/InnovationToImpact.Api/
RUN dotnet restore src/InnovationToImpact.Api/InnovationToImpact.Api.csproj

COPY src/ src/
RUN dotnet publish src/InnovationToImpact.Api/InnovationToImpact.Api.csproj \
    -c Release \
    -o /app \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd -r appuser && useradd -r -g appuser appuser \
    && mkdir -p /app/evidence-storage /app/report-storage /app/email-template-attachments \
    && chown -R appuser:appuser /app
USER appuser

COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Staging
# Railway (and most PaaS hosts) inject PORT at runtime; Program.cs reads it to bind 0.0.0.0:$PORT.
EXPOSE 8080

ENTRYPOINT ["dotnet", "InnovationToImpact.Api.dll"]
