#!/bin/bash
# deploy-vps.sh - Script to be run on the VPS to setup and deploy stayhub

set -e # Exit immediately if a command exits with a non-zero status

echo "=========================================================="
echo " Starting StayHub VPS Automated Deployment Script"
echo "=========================================================="

# 1. Pull latest code
echo ">>> Checking out deployment branch and pulling latest code..."
git checkout chore/deploy-setup || git checkout -b chore/deploy-setup origin/chore/deploy-setup
git pull origin chore/deploy-setup

# 2. Run Backend in Docker
echo ">>> Starting Backend services, SQL Server, and Redis in Docker..."
cd StayHub
docker compose down || true
docker compose up -d --build
cd ..

# 3. Import Databases
echo ">>> Initializing database schema and seed data..."
chmod +x init-db-docker.sh
./init-db-docker.sh

# 4. Handle Frontend
echo ">>> Setting up Frontend React app..."
echo "Would you like to build Frontend directly on this VPS? (y/n)"
echo "Warning: Building on VPS requires npm/node installed and might consume high CPU/RAM."
read -p "Select option: " build_fe

if [ "$build_fe" = "y" ] || [ "$build_fe" = "Y" ]; then
    echo "Installing packages and building frontend..."
    cd ../../Frontend/SEP490_08-Frontend/stayhub
    # Update .env base url to point to VPS IP
    if [ -f .env ]; then
        sed -i 's|VITE_API_BASE_URL=.*|VITE_API_BASE_URL=http://103.82.193.153|g' .env
    else
        echo "VITE_API_BASE_URL=http://103.82.193.153" > .env
    fi
    npm install
    npm run build
    mkdir -p /var/www/stayhub/html
    rm -rf /var/www/stayhub/html/*
    cp -r dist/* /var/www/stayhub/html/
    cd ../../../Backend/SEP490_08-Backend
else
    echo "Using pre-built dist.tar.gz..."
    if [ ! -f /tmp/dist.tar.gz ]; then
        echo "Error: /tmp/dist.tar.gz not found! Please build frontend locally and copy it to VPS first."
        exit 1
    fi
    mkdir -p /var/www/stayhub/html
    rm -rf /var/www/stayhub/html/*
    tar -xzf /tmp/dist.tar.gz -C /var/www/stayhub/html --strip-components=1
fi

# 5. Configure Nginx
echo ">>> Applying Nginx configurations..."
cp stayhub.nginx.conf /etc/nginx/sites-available/default
nginx -t
systemctl reload nginx

echo "=========================================================="
echo " Triển khai StayHub thành công!"
echo "=========================================================="
