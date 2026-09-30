# Derleme bağlamı depo köküdür (docker-compose.yml): dokümanlar imaja kopyalanır.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY api/global.json .
COPY api/src/BilgiAsistani.Core/BilgiAsistani.Core.csproj src/BilgiAsistani.Core/
COPY api/src/BilgiAsistani.Api/BilgiAsistani.Api.csproj src/BilgiAsistani.Api/
RUN dotnet restore src/BilgiAsistani.Api/BilgiAsistani.Api.csproj
COPY api/src/BilgiAsistani.Core src/BilgiAsistani.Core
COPY api/src/BilgiAsistani.Api src/BilgiAsistani.Api
RUN dotnet publish src/BilgiAsistani.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY data/documents ./data/documents
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BilgiAsistani.Api.dll"]
