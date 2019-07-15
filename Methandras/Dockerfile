FROM mcr.microsoft.com/dotnet/core/runtime:2.1-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["Methandras/AccountAllocator.csproj", "Methandras/"]
RUN dotnet restore "Methandras/AccountAllocator.csproj"
COPY . .
WORKDIR "/src/Methandras"
RUN dotnet build "AccountAllocator.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "AccountAllocator.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AccountAllocator.dll"]