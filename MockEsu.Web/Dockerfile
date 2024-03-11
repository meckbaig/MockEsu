#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 5150
EXPOSE 7150

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MockEsu.Web/MockEsu.Web.csproj", "MockEsu.Web/"]
COPY ["MockEsu.Application/MockEsu.Application.csproj", "MockEsu.Application/"]
COPY ["MockEsu.Domain/MockEsu.Domain.csproj", "MockEsu.Domain/"]
COPY ["MockEsu.Infrastructure/MockEsu.Infrastructure.csproj", "MockEsu.Infrastructure/"]
RUN dotnet restore "./MockEsu.Web/MockEsu.Web.csproj"
COPY . .
WORKDIR "/src/MockEsu.Web"
RUN dotnet build "./MockEsu.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MockEsu.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MockEsu.Web.dll"]