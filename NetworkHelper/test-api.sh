#!/bin/bash

# Test script for Network Helper API
BASE_URL="${TECHNITIUM_URL:-http://localhost:5380}/api/networkhelper"
TOKEN="${TECHNITIUM_TOKEN:-your-token-here}"

echo "Testing Network Helper API at: $BASE_URL"
echo ""

# Check if token is set
if [ "$TOKEN" == "your-token-here" ]; then
    echo "⚠️  Please set TECHNITIUM_TOKEN environment variable"
    echo "   export TECHNITIUM_TOKEN=your-admin-token"
    exit 1
fi

# Test 1: Get stats
echo "1. Getting stats..."
curl -s "$BASE_URL/stats?token=$TOKEN" | jq .
echo ""

# Test 2: List devices
echo "2. Listing all devices..."
curl -s "$BASE_URL/devices?token=$TOKEN" | jq .
echo ""

# Test 3: Add a device
echo "3. Adding test device..."
curl -s -X POST "$BASE_URL/devices?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "ip": "10.0.0.198",
    "hostname": "nightcrawler.home",
    "hostnameSource": "ptr",
    "customName": "Living Room TV",
    "mac": "aa:bb:cc:dd:ee:ff",
    "vendor": "Samsung",
    "notes": "Main streaming device",
    "tags": ["entertainment", "iot"],
    "group": "Living Room",
    "icon": "📺"
  }' | jq .
echo ""

# Test 4: Get specific device
echo "4. Getting device 10.0.0.198..."
curl -s "$BASE_URL/devices/get?token=$TOKEN&ip=10.0.0.198" | jq .
echo ""

# Test 5: Update device
echo "5. Updating device (add favorite)..."
curl -s -X POST "$BASE_URL/devices?token=$TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "ip": "10.0.0.198",
    "hostname": "nightcrawler.home",
    "customName": "Living Room TV",
    "favorite": true
  }' | jq .
echo ""

# Test 6: Get updated stats
echo "6. Getting updated stats..."
curl -s "$BASE_URL/stats?token=$TOKEN" | jq .
echo ""

# Test 7: Export as JSON
echo "7. Exporting devices as JSON..."
curl -s "$BASE_URL/export?token=$TOKEN&format=json" | jq . | head -20
echo ""

# Test 8: Export as CSV
echo "8. Exporting devices as CSV..."
curl -s "$BASE_URL/export?token=$TOKEN&format=csv" | head -10
echo ""

echo "✓ API tests completed"
