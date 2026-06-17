FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Hub/WebhookHub/WebhookHub.csproj", "src/Hub/WebhookHub/"]
RUN dotnet restore "src/Hub/WebhookHub/WebhookHub.csproj"

COPY . .
WORKDIR "/src/src/Hub/WebhookHub"
RUN dotnet publish "WebhookHub.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebhookHub.dll"]