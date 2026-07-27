# Railway / Linux container image for the InnovationToImpact API.
# Builds the API project directly rather than InnovationToImpact.sln: the solution still lists two test
# projects whose folders are not present in this repo, so a solution-level restore fails.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/src/InnovationToImpact.Domain/InnovationToImpact.Domain.csproj backend/src/InnovationToImpact.Domain/
COPY backend/src/InnovationToImpact.Infrastructure/InnovationToImpact.Infrastructure.csproj backend/src/InnovationToImpact.Infrastructure/
COPY backend/src/InnovationToImpact.Api/InnovationToImpact.Api.csproj backend/src/InnovationToImpact.Api/
RUN dotnet restore backend/src/InnovationToImpact.Api/InnovationToImpact.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/src/InnovationToImpact.Api/InnovationToImpact.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# QuestPDF (report rendering) needs native font libraries, which the aspnet image does not ship.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

# Matches the *Storage:RootPath values in appsettings.Production.json. Mount a Railway volume here to
# keep uploads across redeploys; the container filesystem is otherwise ephemeral.
RUN mkdir -p /app/storage/reports /app/storage/evidence /app/storage/templates \
    /app/storage/home-media /app/storage/email-attachments

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "InnovationToImpact.Api.dll"]
