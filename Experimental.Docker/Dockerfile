FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /src
# Copy everything
COPY . ./
# Restore dependencies
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o /src/publish


# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
EXPOSE 80
COPY --from=build-env /src/publish .
ENTRYPOINT ["dotnet", "Experimental.Docker.dll"]