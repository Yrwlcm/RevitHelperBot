FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RevitHelperBot.Api/RevitHelperBot.Api.csproj", "RevitHelperBot.Api/"]
COPY ["RevitHelperBot.Application/RevitHelperBot.Application.csproj", "RevitHelperBot.Application/"]
COPY ["RevitHelperBot.Core/RevitHelperBot.Core.csproj", "RevitHelperBot.Core/"]
RUN dotnet restore "RevitHelperBot.Api/RevitHelperBot.Api.csproj"
COPY . .
WORKDIR "/src/RevitHelperBot.Api"
RUN dotnet build "RevitHelperBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RevitHelperBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RevitHelperBot.Api.dll"]
