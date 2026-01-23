#!/bin/bash
set -e

VERSION="1.0.0"
OUTPUT_DIR="./build"
PACKAGE_NAME="NetworkHelper"

echo "Building Network Helper v${VERSION}..."

# Clean
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Build
dotnet build NetworkHelper.csproj -c Release

# Copy files to output
cp bin/Release/net8.0/NetworkHelper.dll "$OUTPUT_DIR/"
cp bin/Release/net8.0/NetworkHelper.deps.json "$OUTPUT_DIR/deps.json" || true
cp dnsApps.config "$OUTPUT_DIR/"

# Create ZIP package
cd "$OUTPUT_DIR"
zip -r "../${PACKAGE_NAME}-${VERSION}.zip" ./*
cd ..

echo "✓ Package created: ${PACKAGE_NAME}-${VERSION}.zip"
echo "✓ Size: $(du -h ${PACKAGE_NAME}-${VERSION}.zip | cut -f1)"
ls -lh "${PACKAGE_NAME}-${VERSION}.zip"
