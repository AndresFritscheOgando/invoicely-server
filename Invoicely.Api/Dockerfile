FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Invoicely.slnx .
COPY Invoicely.Api/Invoicely.Api.csproj Invoicely.Api/
COPY Invoicely.Core/Invoicely.Core.csproj Invoicely.Core/
COPY Invoicely.Infrastructure/Invoicely.Infrastructure.csproj Invoicely.Infrastructure/
RUN dotnet restore

COPY . .
RUN dotnet publish Invoicely.Api/Invoicely.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Invoicely.Api.dll"]
