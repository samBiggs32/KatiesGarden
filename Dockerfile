FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Api/KatiesGarden.Api.csproj", "Api/"]
RUN dotnet restore "Api/KatiesGarden.Api.csproj"
COPY Api/ Api/
RUN dotnet publish "Api/KatiesGarden.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KatiesGarden.Api.dll"]
