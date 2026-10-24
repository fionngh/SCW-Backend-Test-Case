# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props CloudWorks.sln ./
COPY src/CloudWorks.Common/*.csproj src/CloudWorks.Common/
COPY src/CloudWorks.Domain/*.csproj src/CloudWorks.Domain/
COPY src/CloudWorks.Application/*.csproj src/CloudWorks.Application/
COPY src/CloudWorks.Infrastructure/*.csproj src/CloudWorks.Infrastructure/
COPY src/CloudWorks.Api/*.csproj src/CloudWorks.Api/
COPY tests/CloudWorks.UnitTests/*.csproj tests/CloudWorks.UnitTests/
COPY tests/CloudWorks.IntegrationTests/*.csproj tests/CloudWorks.IntegrationTests/
RUN dotnet restore CloudWorks.sln
COPY src/ src/
RUN dotnet publish src/CloudWorks.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CloudWorks.Api.dll"]
