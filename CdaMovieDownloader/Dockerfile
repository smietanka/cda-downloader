FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

WORKDIR /app

##################################################

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS unit

COPY . .

FROM unit AS build

RUN dotnet restore CdaMovieDownloader.csproj
RUN dotnet publish CdaMovieDownloader.csproj -r linux-x64 -c Release

##################################################

FROM base AS final

ENTRYPOINT ["dotnet", "WebAPI.dll"]

COPY --from=build bin/Release/net6.0/linux-x64/publish .
