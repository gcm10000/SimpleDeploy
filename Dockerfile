# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Instala o openssh-client para permitir chamadas SSH
RUN apt-get update && apt-get install -y openssh-client && rm -rf /var/lib/apt/lists/*

# Gera chave SSH caso não exista (no diretório padrão /root/.ssh)
RUN mkdir -p /root/.ssh && \
    chmod 700 /root/.ssh && \
    [ -f /root/.ssh/id_rsa ] || ssh-keygen -t rsa -b 4096 -f /root/.ssh/id_rsa -q -N ""

# Adiciona o usuário app de volta e ajusta permissões
RUN if ! id app 2>/dev/null; then useradd -m app; fi && chown -R app /app
USER app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SimpleDeploy.Api/SimpleDeploy.Api.csproj", "SimpleDeploy.Api/"]
COPY ["SimpleDeploy.Application/SimpleDeploy.Application.csproj", "SimpleDeploy.Application/"]
RUN dotnet restore "./SimpleDeploy.Api/SimpleDeploy.Api.csproj"
COPY . ./
WORKDIR "/src/SimpleDeploy.Api"
RUN dotnet build "./SimpleDeploy.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SimpleDeploy.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SimpleDeploy.Api.dll"]
