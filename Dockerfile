FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/JobForge.Ui/JobForge.Ui.csproj", "src/JobForge.Ui/"]
COPY ["Directory.Packages.props", "./"]
RUN dotnet restore "src/JobForge.Ui/JobForge.Ui.csproj"
COPY . .
RUN dotnet publish "src/JobForge.Ui/JobForge.Ui.csproj" -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8096
EXPOSE 8096
USER $APP_UID
ENTRYPOINT ["dotnet", "JobForge.Ui.dll"]