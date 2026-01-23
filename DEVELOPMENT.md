# Development Guide

## Project Structure

```
technilux-apps/
├── .github/
│   └── workflows/
│       └── release.yml           # Automated build and release
├── NetworkHelper/
│   ├── App.cs                    # Main entry point (IDnsApplication)
│   ├── NetworkHelper.csproj      # .NET 8.0 project file
│   ├── dnsApps.config            # App metadata for Technitium
│   ├── Models/
│   │   ├── Device.cs             # Device data model
│   │   ├── AppSettings.cs        # Settings model
│   │   └── ApiResponse.cs        # Standard API response wrapper
│   ├── Storage/
│   │   └── DeviceStore.cs        # JSON file storage with backups
│   ├── Controllers/
│   │   ├── DevicesController.cs  # Device CRUD endpoints
│   │   ├── SettingsController.cs # Settings management
│   │   └── UtilityController.cs  # Stats, cleanup, export
│   ├── Services/
│   │   ├── CleanupService.cs     # Background cleanup task
│   │   └── ExportService.cs      # CSV/JSON export
│   ├── Utils/
│   │   └── Validation.cs         # Input validation helpers
│   ├── build.sh                  # Build and package script
│   ├── test-api.sh               # API testing script
│   └── README.md                 # App documentation
├── .gitignore
└── README.md                     # Repository overview
```

## Building Locally

### Prerequisites

- .NET 8.0 SDK
- Bash shell (Linux/macOS/WSL)
- zip utility

### Build Steps

```bash
cd NetworkHelper

# Restore dependencies
dotnet restore

# Build in release mode
dotnet build NetworkHelper.csproj -c Release

# Or use the build script
./build.sh
```

The build script will create:
- `NetworkHelper-1.0.0.zip` - Ready to install in Technitium

### Testing

1. Install the built ZIP in Technitium DNS Server
2. Configure your admin token:
   ```bash
   export TECHNITIUM_TOKEN=your-admin-token
   export TECHNITIUM_URL=http://localhost:5380
   ```
3. Run the test script:
   ```bash
   ./test-api.sh
   ```

## Development Workflow

### Adding New Features

1. Create feature branch:
   ```bash
   git checkout -b feature/new-feature
   ```

2. Make changes to relevant files:
   - Models: Update data structures
   - Controllers: Add/modify API endpoints
   - Services: Add background tasks or utilities
   - Storage: Modify storage logic

3. Test locally with Technitium

4. Commit and push:
   ```bash
   git add .
   git commit -m "feat: add new feature"
   git push origin feature/new-feature
   ```

5. Create Pull Request

### Release Process

1. Update version in:
   - `NetworkHelper.csproj` - `<Version>1.0.0</Version>`
   - `dnsApps.config` - `"version": "1.0.0"`
   - `build.sh` - `VERSION="1.0.0"`

2. Commit version bump:
   ```bash
   git commit -am "chore: bump version to 1.0.0"
   ```

3. Create and push tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

4. GitHub Actions will automatically:
   - Build the project
   - Create ZIP package
   - Create GitHub release with the ZIP attached

## API Endpoints Reference

### Base URL
```
http://your-technitium:5380/api/networkhelper
```

### Authentication
All endpoints require `?token=<admin-token>`

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/devices` | List all devices |
| GET | `/devices/get?ip=<ip>` | Get single device |
| POST | `/devices` | Create/update device |
| DELETE | `/devices?ip=<ip>` | Delete device |
| POST | `/devices/bulk` | Bulk import devices |
| GET | `/settings` | Get app settings |
| POST | `/settings` | Update app settings |
| GET | `/stats` | Get statistics |
| POST | `/cleanup` | Manually trigger cleanup |
| GET | `/export?format=json\|csv` | Export devices |

## Code Style

### C# Conventions

- Use C# 12 features (implicit usings, nullable reference types)
- Follow Microsoft naming conventions
- Use async/await for all I/O operations
- Validate all user input
- Handle errors gracefully

### Example Pattern

```csharp
public static async Task<string> MyEndpoint(DeviceStore store, string queryString)
{
    try
    {
        // 1. Parse and validate input
        var query = HttpUtility.ParseQueryString(queryString);
        var param = query["param"];

        if (string.IsNullOrWhiteSpace(param))
        {
            return JsonSerializer.Serialize(ApiResponse.Error("Parameter required"));
        }

        // 2. Perform operation
        var result = await store.DoSomethingAsync(param);

        // 3. Return success response
        return JsonSerializer.Serialize(ApiResponse.Success(result));
    }
    catch (Exception ex)
    {
        return JsonSerializer.Serialize(ApiResponse.Error($"Error: {ex.Message}"));
    }
}
```

## Storage Format

### devices.json
```json
[
  {
    "ip": "10.0.0.198",
    "hostname": "device.local",
    "customName": "My Device",
    "mac": "aa:bb:cc:dd:ee:ff",
    "vendor": "Samsung",
    "hostnameSource": "ptr",
    "notes": "Notes here",
    "tags": ["tag1", "tag2"],
    "group": "Living Room",
    "icon": "📱",
    "firstSeen": "2026-01-22T10:00:00Z",
    "lastSeen": "2026-01-22T16:00:00Z",
    "lastUpdated": "2026-01-22T16:00:00Z",
    "queryCount": 100,
    "favorite": false
  }
]
```

### settings.json
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

## Debugging

### Enable Console Logging

The app logs to Technitium's console output:

```bash
# View Technitium logs
docker logs -f technitium

# Or if running directly
journalctl -u dns-server -f
```

### Common Issues

**Build Errors:**
- Ensure .NET 8.0 SDK is installed: `dotnet --version`
- Restore packages: `dotnet restore`

**Runtime Errors:**
- Check Technitium version (requires v12.0+)
- Verify file permissions on data directory
- Check logs for stack traces

**API Errors:**
- Verify token is valid
- Check endpoint path matches exactly
- Ensure request body is valid JSON

## Testing

### Manual API Testing

Use the `test-api.sh` script or curl directly:

```bash
# Get all devices
curl -s "http://localhost:5380/api/networkhelper/devices?token=YOUR_TOKEN" | jq .

# Add device
curl -X POST "http://localhost:5380/api/networkhelper/devices?token=YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "ip": "10.0.0.100",
    "hostname": "test.local",
    "customName": "Test Device"
  }' | jq .
```

### Integration Testing

Test with the TechniLux UI:

1. Build and install Network Helper
2. Open TechniLux UI at http://localhost:5173
3. Navigate to Network Clients page
4. Click "Resolve Hostnames"
5. Verify devices appear with hostnames
6. Edit a device and add custom name
7. Refresh page - custom name should persist

## Contributing

1. Fork the repository
2. Create feature branch
3. Make changes
4. Test thoroughly
5. Submit Pull Request

## License

MIT License - See LICENSE file
