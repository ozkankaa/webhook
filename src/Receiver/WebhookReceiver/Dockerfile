FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Receiver/WebhookReceiver/WebhookReceiver.csproj", "src/Receiver/WebhookReceiver/"]
RUN dotnet restore "src/Receiver/WebhookReceiver/WebhookReceiver.csproj"

COPY . .
WORKDIR "/src/src/Receiver/WebhookReceiver"
RUN dotnet publish "WebhookReceiver.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebhookReceiver.dll"]