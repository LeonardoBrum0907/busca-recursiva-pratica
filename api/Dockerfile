FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BuscaApi.sln ./
COPY BuscaApi/BuscaApi.csproj BuscaApi/
COPY BuscaApi.Application/BuscaApi.Application.csproj BuscaApi.Application/
COPY BuscaApi.Domain/BuscaApi.Domain.csproj BuscaApi.Domain/
COPY BuscaApi.Infrastructure/BuscaApi.Infrastructure.csproj BuscaApi.Infrastructure/
COPY BuscaApi.Testes/BuscaApi.Testes.csproj BuscaApi.Testes/

RUN dotnet restore BuscaApi/BuscaApi.csproj

COPY . .
RUN dotnet publish BuscaApi/BuscaApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BuscaApi.dll"]
