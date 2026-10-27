FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/JobForge.Ui/JobForge.Ui.csproj", "src/JobForge.Ui/"]
COPY ["Directory.Packages.props", "./"]
RUN dotnet restore "src/JobForge.Ui/JobForge.Ui.csproj"
COPY . .
# no --no-restore: the Razor SDK adds the framework static-assets PackageDownload during build,
# so a restore limited to the csproj would publish without wwwroot/_framework (blazor.web.js).
RUN dotnet publish "src/JobForge.Ui/JobForge.Ui.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8096
EXPOSE 8096
USER $APP_UID
ENTRYPOINT ["dotnet", "JobForge.Ui.dll"]