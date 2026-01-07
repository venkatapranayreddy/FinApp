FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["backend/FinApp.API/FinApp.API.csproj", "FinApp.API/"]
RUN dotnet restore "FinApp.API/FinApp.API.csproj"
COPY backend/ .
WORKDIR "/src/FinApp.API"
RUN dotnet build "FinApp.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FinApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FinApp.API.dll"]
