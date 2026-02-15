# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore (respecting dependencies)
COPY 3-Domain/GestorFinanceiro.Financeiro.Domain/GestorFinanceiro.Financeiro.Domain.csproj 3-Domain/GestorFinanceiro.Financeiro.Domain/
COPY 4-Infra/GestorFinanceiro.Financeiro.Infra/GestorFinanceiro.Financeiro.Infra.csproj 4-Infra/GestorFinanceiro.Financeiro.Infra/
COPY 2-Application/GestorFinanceiro.Financeiro.Application/GestorFinanceiro.Financeiro.Application.csproj 2-Application/GestorFinanceiro.Financeiro.Application/
COPY 1-Services/GestorFinanceiro.Financeiro.API/GestorFinanceiro.Financeiro.API.csproj 1-Services/GestorFinanceiro.Financeiro.API/

# Restore dependencies for the API project
WORKDIR /src/1-Services/GestorFinanceiro.Financeiro.API
RUN dotnet restore

# Copy all source code
WORKDIR /src
COPY 3-Domain/ 3-Domain/
COPY 4-Infra/ 4-Infra/
COPY 2-Application/ 2-Application/
COPY 1-Services/ 1-Services/

# Build and publish the API project
WORKDIR /src/1-Services/GestorFinanceiro.Financeiro.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends wget \
	&& rm -rf /var/lib/apt/lists/*

# Copy published output from build stage
COPY --from=build /app/publish .

# Configure the application to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Expose port 8080
EXPOSE 8080

# Set the entrypoint
ENTRYPOINT ["dotnet", "GestorFinanceiro.Financeiro.API.dll"]
