FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/SimpleCard.Api/SimpleCard.Api.csproj", "src/SimpleCard.Api/"]
COPY ["src/SimpleCard.Application/SimpleCard.Application.csproj", "src/SimpleCard.Application/"]
COPY ["src/SimpleCard.Domain/SimpleCard.Domain.csproj", "src/SimpleCard.Domain/"]
COPY ["src/SimpleCard.Infrastructure/SimpleCard.Infrastructure.csproj", "src/SimpleCard.Infrastructure/"]
COPY ["src/SimpleCard.ExchangeRateClient/SimpleCard.ExchangeRateClient.csproj", "src/SimpleCard.ExchangeRateClient/"]
RUN dotnet restore "src/SimpleCard.Api/SimpleCard.Api.csproj"
COPY . .
WORKDIR "/src/src/SimpleCard.Api"
RUN dotnet build "SimpleCard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SimpleCard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleCard.Api.dll"]
