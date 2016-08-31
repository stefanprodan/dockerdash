FROM microsoft/dotnet:latest

# Set environment variables
ENV ASPNETCORE_URLS="http://*:5050"
ENV ASPNETCORE_ENVIRONMENT="Staging"

# Copy files to app directory
COPY /src/Docker.DotNet /app/src/Docker.DotNet
COPY /src/DockerDash /app/src/DockerDash
COPY NuGet.Config /app/src/DockerDash/NuGet.Config

# RethinkDbLogProvider
WORKDIR /app/src/Docker.DotNet
RUN ["dotnet", "restore"]

# Set working directory
WORKDIR /app/src/DockerDash

# Restore NuGet packages
RUN ["dotnet", "restore"]

# Build the app
RUN ["dotnet", "build"]

# Open port
EXPOSE 5050/tcp

# Run the app
ENTRYPOINT ["dotnet", "run"]