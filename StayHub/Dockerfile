FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file and project files to restore dependencies
COPY StayHub.sln ./
COPY StayHub.Common/StayHub.Common.csproj StayHub.Common/
COPY AIAPI/AIAPI.csproj AIAPI/
COPY AuthAPI/AuthAPI.csproj AuthAPI/
COPY BookingAPI/BookingAPI.csproj BookingAPI/
COPY ContentAPI/ContentAPI.csproj ContentAPI/
COPY GatewayAPI/GatewayAPI.csproj GatewayAPI/
COPY PaymentAPI/PaymentAPI.csproj PaymentAPI/
COPY SocialAPI/SocialAPI.csproj SocialAPI/
COPY SystemAPI/SystemAPI.csproj SystemAPI/
COPY TourAPI/TourAPI.csproj TourAPI/
COPY VoucherAPI/VoucherAPI.csproj VoucherAPI/

# Restore dependencies
RUN dotnet restore StayHub.sln

# Copy the remaining files and build the specified project
COPY . .
ARG PROJECT_NAME
RUN dotnet publish ${PROJECT_NAME}/${PROJECT_NAME}.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends libgomp1 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

# Optimize memory usage for VPS (Workstation GC instead of Server GC)
ENV DOTNET_System_GC_Server=0
ENV ASPNETCORE_URLS=http://+:8080

ARG PROJECT_NAME
ENV DLL_NAME=${PROJECT_NAME}.dll

ENTRYPOINT exec dotnet $DLL_NAME
