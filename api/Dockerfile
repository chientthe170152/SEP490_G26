# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MTCA.sln ./
COPY MTCA.Domain/MTCA.Domain.csproj MTCA.Domain/
COPY MTCA.Application/MTCA.Application.csproj MTCA.Application/
COPY MTCA.Infrastructure/MTCA.Infrastructure.csproj MTCA.Infrastructure/
COPY MTCA.Api/MTCA.Api.csproj MTCA.Api/
RUN dotnet restore MTCA.sln

COPY . .
RUN dotnet publish MTCA.Api/MTCA.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

EXPOSE 5080
ENV ASPNETCORE_URLS=http://0.0.0.0:5080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

ENTRYPOINT ["dotnet", "MTCA.Api.dll"]
