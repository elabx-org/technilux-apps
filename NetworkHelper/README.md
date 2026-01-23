# Network Helper

A Technitium DNS Server app that provides persistent storage for network client information, hostname mappings, and device metadata.

## Features

- **Hostname Cache** - Store PTR-resolved hostnames with timestamps
- **Device Metadata** - Custom names, notes, tags, groups, icons
- **Source Tracking** - Track where hostname came from (PTR, DHCP, ARP, manual)
- **Auto-cleanup** - Remove stale entries after configurable TTL
- **Bulk Import/Export** - CSV and JSON support
- **RESTful API** - Full CRUD operations for devices and settings

## Installation

### From GitHub Release

1. Download `NetworkHelper.zip` from [releases](https://github.com/elabx-org/technilux-apps/releases)
2. Navigate to Technitium DNS Server → Apps
3. Click "Install" and upload the ZIP file
4. Configure settings as needed
5. App starts automatically

### Build from Source

Requirements:
- .NET 8.0 SDK
- Technitium DNS Server v12.0+

```bash
# Build the project
dotnet build NetworkHelper.csproj -c Release

# Or use the build script
chmod +x build.sh
./build.sh

# Install the generated ZIP file
ls NetworkHelper-*.zip
```

## API Endpoints

All endpoints require `?token=<your-admin-token>` authentication.

### Device Management

```bash
# List all devices
GET /api/networkhelper/devices

# Get single device
GET /api/networkhelper/devices/get?ip=10.0.0.100

# Create or update device
POST /api/networkhelper/devices
Content-Type: application/json
{
  "ip": "10.0.0.198",
  "hostname": "nightcrawler.home",
  "customName": "Living Room TV",
  "mac": "aa:bb:cc:dd:ee:ff",
  "vendor": "Samsung",
  "hostnameSource": "ptr",
  "notes": "Main streaming device",
  "tags": ["entertainment", "iot"],
  "group": "Living Room",
  "icon": "📺",
  "favorite": false
}

# Delete device
DELETE /api/networkhelper/devices?ip=10.0.0.100

# Bulk import
POST /api/networkhelper/devices/bulk
Content-Type: application/json
[{...device objects...}]
```

### Settings

```bash
# Get settings
GET /api/networkhelper/settings

# Update settings
POST /api/networkhelper/settings
Content-Type: application/json
{
  "cleanupEnabled": true,
  "cleanupIntervalHours": 24,
  "staleThresholdDays": 30,
  "autoResolveHostnames": false,
  "defaultGroup": "Ungrouped",
  "enableNotifications": false
}
```

### Utility

```bash
# Get statistics
GET /api/networkhelper/stats

# Manual cleanup of stale devices
POST /api/networkhelper/cleanup

# Export devices
GET /api/networkhelper/export?format=json
GET /api/networkhelper/export?format=csv
```

## Data Model

### Device Object

```json
{
  "ip": "10.0.0.198",
  "hostname": "nightcrawler.home",
  "customName": "Living Room TV",
  "mac": "aa:bb:cc:dd:ee:ff",
  "vendor": "Samsung Electronics",
  "hostnameSource": "ptr",
  "notes": "Main streaming device",
  "tags": ["entertainment", "iot"],
  "group": "Living Room",
  "icon": "📺",
  "firstSeen": "2026-01-22T10:30:00Z",
  "lastSeen": "2026-01-22T16:45:00Z",
  "lastUpdated": "2026-01-22T16:45:00Z",
  "queryCount": 240,
  "favorite": false
}
```

### Response Format

All endpoints return:

```json
{
  "status": "ok",
  "data": { ... },
  "message": "Optional message",
  "timestamp": "2026-01-22T16:45:00Z"
}
```

Error responses:

```json
{
  "status": "error",
  "message": "Error description",
  "timestamp": "2026-01-22T16:45:00Z"
}
```

## Storage

Data is stored in JSON files within the app's data directory:

- `/etc/dns/apps/NetworkHelper/devices.json` - Device database
- `/etc/dns/apps/NetworkHelper/settings.json` - App settings
- `/etc/dns/apps/NetworkHelper/backups/` - Automatic backups (last 5)

## Configuration

Default settings:

```json
{
  "cleanupEnabled": true,
  "cleanupIntervalHours": 24,
  "staleThresholdDays": 30,
  "autoResolveHostnames": false,
  "defaultGroup": "Ungrouped",
  "enableNotifications": false
}
```

## Testing

Set your Technitium admin token and run the test script:

```bash
export TECHNITIUM_TOKEN=your-admin-token
export TECHNITIUM_URL=http://localhost:5380

chmod +x test-api.sh
./test-api.sh
```

## UI Integration

The TechniLux UI automatically integrates with Network Helper when installed:

- **Network Clients Page** - Shows cached hostnames, custom names
- **DNS Record Forms** - Device picker with autocomplete
- **Query Logs** - Device names instead of IPs
- **Dashboard** - Top devices with friendly names
- **DHCP Scopes** - Network range suggestions

See the [TechniLux UI repository](https://github.com/elabx-org/technitium-ui) for details.

## Development

Project structure:

```
NetworkHelper/
├── NetworkHelper.csproj
├── App.cs                      # Main entry point
├── dnsApps.config             # App metadata
├── Models/
│   ├── Device.cs              # Device data model
│   ├── AppSettings.cs         # Settings model
│   └── ApiResponse.cs         # API response wrapper
├── Storage/
│   └── DeviceStore.cs         # JSON file storage with backups
├── Controllers/
│   ├── DevicesController.cs   # Device CRUD
│   ├── SettingsController.cs  # Settings management
│   └── UtilityController.cs   # Stats, cleanup, export
├── Services/
│   ├── CleanupService.cs      # Background cleanup
│   └── ExportService.cs       # CSV/JSON export
└── Utils/
    └── Validation.cs          # Input validation
```

## License

MIT License

## Author

TechniLux - https://github.com/elabx-org/technilux-apps
