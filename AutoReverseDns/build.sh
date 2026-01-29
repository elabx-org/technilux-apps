#!/bin/bash
set -e

APP_NAME="AutoReverseDns"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== Building $APP_NAME ==="

# Create libs directory
mkdir -p libs

# Extract DLLs from Technitium Docker image if not present
if [ ! -f "libs/DnsServerCore.ApplicationCommon.dll" ]; then
    echo "Extracting Technitium DLLs from Docker image..."

    CONTAINER_ID=$(docker create technitium/dns-server:latest)
    docker cp "$CONTAINER_ID:/opt/technitium/dns/DnsServerCore.dll" libs/
    docker cp "$CONTAINER_ID:/opt/technitium/dns/DnsServerCore.ApplicationCommon.dll" libs/
    docker cp "$CONTAINER_ID:/opt/technitium/dns/TechnitiumLibrary.dll" libs/
    docker cp "$CONTAINER_ID:/opt/technitium/dns/TechnitiumLibrary.Net.dll" libs/
    docker rm "$CONTAINER_ID"

    echo "DLLs extracted successfully"
fi

# Restore and build
echo "Building..."
dotnet restore
dotnet build "$APP_NAME.csproj" -c Release --no-restore

# Create distribution zip
echo "Creating distribution zip..."
OUTPUT_DIR="./build"
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

cp "bin/Release/net9.0/$APP_NAME.dll" "$OUTPUT_DIR/"
cp "bin/Release/net9.0/$APP_NAME.deps.json" "$OUTPUT_DIR/"
cp dnsApp.config "$OUTPUT_DIR/"

cd "$OUTPUT_DIR"
zip -j "../$APP_NAME.zip" ./*
cd ..

echo "=== Build complete: $APP_NAME.zip ==="
