#!/bin/bash
echo "Starting Backend Microservices..."
cd /home/kiuthi/Storage/Projects/Backend/SEP490_08-Backend/StayHub
dotnet run --project GatewayAPI/GatewayAPI.csproj --launch-profile "https" &
dotnet run --project AIAPI/AIAPI.csproj --launch-profile "https" &
dotnet run --project AuthAPI/AuthAPI.csproj --launch-profile "https" &
dotnet run --project BookingAPI/BookingAPI.csproj --launch-profile "https" &
dotnet run --project ContentAPI/ContentAPI.csproj --launch-profile "https" &
dotnet run --project PaymentAPI/PaymentAPI.csproj --launch-profile "https" &
dotnet run --project SocialAPI/SocialAPI.csproj --launch-profile "https" &
dotnet run --project SystemAPI/SystemAPI.csproj --launch-profile "https" &
dotnet run --project TourAPI/TourAPI.csproj --launch-profile "https" &
dotnet run --project VoucherAPI/VoucherAPI.csproj --launch-profile "https" &

echo "Starting Frontend..."
cd /home/kiuthi/Storage/Projects/Frontend/SEP490_08-Frontend/stayhub
npm run dev &
