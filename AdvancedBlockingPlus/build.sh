#!/bin/bash
# Build script for Advanced Blocking Plus
# This script extracts required DLLs from a Technitium Docker image and builds the app

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== Advanced Blocking Plus Build Script ==="
echo ""

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found. Please install .NET 9.0 SDK."
    echo "       https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "0")
echo "Using .NET SDK version: $DOTNET_VERSION"

# Create lib directory
mkdir -p lib

# Check if DLLs already exist
if [ -f "lib/TechnitiumLibrary.dll" ] && [ -f "lib/TechnitiumLibrary.Net.dll" ] && [ -f "lib/DnsServerCore.ApplicationCommon.dll" ]; then
    echo "Required DLLs already present in lib/"
else
    echo ""
    echo "Extracting required DLLs from Technitium Docker image..."

    # Check for Docker
    if ! command -v docker &> /dev/null; then
        echo ""
        echo "ERROR: Docker not found. Please either:"
        echo "  1. Install Docker and re-run this script"
        echo "  2. Manually copy these DLLs to lib/:"
        echo "     - TechnitiumLibrary.dll"
        echo "     - TechnitiumLibrary.Net.dll"
        echo "     - DnsServerCore.ApplicationCommon.dll"
        echo ""
        echo "DLL locations:"
        echo "  Docker: docker cp <container>:/opt/technitium/dns/ ."
        echo "  Linux:  /opt/technitium/dns/"
        echo "  Windows: C:\\Program Files\\Technitium\\DNS Server\\"
        exit 1
    fi

    # Pull and extract from official Technitium image
    echo "Pulling technitium/dns-server:latest..."
    docker pull technitium/dns-server:latest

    # Create a temporary container to extract files
    echo "Creating temporary container..."
    CONTAINER_ID=$(docker create technitium/dns-server:latest)

    echo "Extracting DLLs..."
    docker cp "$CONTAINER_ID:/opt/technitium/dns/TechnitiumLibrary.dll" lib/
    docker cp "$CONTAINER_ID:/opt/technitium/dns/TechnitiumLibrary.Net.dll" lib/
    docker cp "$CONTAINER_ID:/opt/technitium/dns/DnsServerCore.ApplicationCommon.dll" lib/

    echo "Removing temporary container..."
    docker rm "$CONTAINER_ID"

    echo "DLLs extracted successfully!"
fi

echo ""
echo "Building Advanced Blocking Plus..."

# Build
dotnet build -c Release AdvancedBlockingPlus.csproj

echo ""
echo "=== Build Complete ==="
echo ""
echo "Output files:"
echo "  bin/Release/net9.0/AdvancedBlockingPlus.dll"
echo "  bin/Release/net9.0/dnsApp.config"
echo ""
echo "To install:"
echo "  1. Via Technitium GUI: Apps > Install > Upload AdvancedBlockingPlus.dll"
echo "  2. Manual: Copy DLL to /etc/dns/apps/AdvancedBlockingPlus/"
echo ""
