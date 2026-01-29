# Development Guide

## Project Structure

```
technilux-apps/
├── .github/
│   └── workflows/
│       └── release.yml           # Automated build and release
├── AdvancedBlockingPlus/         # Enhanced blocking with multi-group support
├── AdvancedForwardingPlus/       # Enhanced forwarding with multi-group support
├── AutoReverseDns/               # Automatic PTR record management
│   ├── App.cs                    # Main entry point (IDnsApplication)
│   ├── AutoReverseDns.csproj     # .NET 9.0 project file
│   ├── dnsApps.config            # App metadata for Technitium (REQUIRED)
│   ├── ui-schema.json            # TechniLux UI schema for visual config
│   ├── Models/
│   ├── Services/
│   ├── build.sh                  # Build and package script
│   └── README.md
├── NetworkHelper/                # Network client metadata storage
│   ├── App.cs                    # Main entry point (IDnsApplication)
│   ├── NetworkHelper.csproj      # .NET 9.0 project file
│   ├── dnsApps.config            # App metadata for Technitium (REQUIRED)
│   ├── Models/
│   ├── Storage/
│   ├── Controllers/
│   ├── Services/
│   ├── build.sh
│   └── README.md
├── schemas/                      # Example UI schemas for reference
├── appstore.json                 # TechniLux app store catalog
├── UI-SCHEMA-GUIDE.md            # Complete UI schema documentation
├── DEVELOPMENT.md                # This file
└── README.md                     # Repository overview
```

### Required Files for Each App

| File | Purpose |
|------|---------|
| `App.cs` | Main entry point implementing `IDnsApplication` |
| `*.csproj` | .NET project file with version info |
| `dnsApps.config` | **Required** - App metadata (name, version, description) |
| `build.sh` | Build and package script |
| `ui-schema.json` | Optional - TechniLux visual config schema |
| `README.md` | App documentation |

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

1. **Update version in all locations** (must match):

   | File | Field | Example |
   |------|-------|---------|
   | `*.csproj` | `<Version>` | `1.0.1` |
   | `*.csproj` | `<AssemblyVersion>` | `1.0.1.0` |
   | `*.csproj` | `<FileVersion>` | `1.0.1.0` |
   | `dnsApps.config` | `"version"` | `"1.0.1"` |
   | `appstore.json` | `"version"` | `"1.0.1"` |
   | `appstore.json` | `"downloadUrl"` | Update tag in URL |

2. Commit version bump:
   ```bash
   git commit -am "chore: bump AppName to v1.0.1"
   git push origin main
   ```

3. Create and push tag (use app-specific tag for single-app releases):
   ```bash
   # For single app release
   git tag v1.0.1-autoreversedns
   git push origin v1.0.1-autoreversedns

   # For full release of all apps
   git tag v1.0.1
   git push origin v1.0.1
   ```

4. GitHub Actions will automatically:
   - Build all apps
   - Create ZIP packages
   - Create GitHub release with all ZIPs attached

### Version Mismatch Issues

If the installed app shows a different version than expected (e.g., "1.0" instead of "1.0.1"):

1. **Check `dnsApps.config`** - Ensure it has the correct version
2. **Verify ZIP contents** - The ZIP must contain `dnsApps.config` with metadata
3. **Rebuild and reinstall** - Uninstall the app and install fresh from the store

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

## App Metadata File (dnsApps.config)

Every Technitium DNS app **must** include a `dnsApps.config` file in the ZIP package. This file contains the app metadata that Technitium uses to display app information.

### Required Structure

```json
{
  "name": "My App Name",
  "version": "1.0.0",
  "description": "What this app does",
  "author": "TechniLux",
  "url": "https://github.com/elabx-org/technilux-apps",
  "appRecordDataTemplate": null,
  "configTemplate": {
    // Default configuration values (optional)
  }
}
```

### Fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Display name shown in Technitium and TechniLux UI |
| `version` | Yes | Semantic version (e.g., "1.0.0") - **must match csproj** |
| `description` | Yes | Brief description of the app's functionality |
| `author` | No | Author name |
| `url` | No | Project URL |
| `appRecordDataTemplate` | No | Template for APP record data (null if not applicable) |
| `configTemplate` | No | Default configuration object applied on first install |

### Important Notes

1. **Version Consistency**: The version in `dnsApps.config` must match the version in your `.csproj` file:
   ```xml
   <Version>1.0.0</Version>
   <AssemblyVersion>1.0.0.0</AssemblyVersion>
   <FileVersion>1.0.0.0</FileVersion>
   ```

2. **File Naming**: Use `dnsApps.config` (plural), not `dnsApp.config`. The plural form is the standard Technitium format for app metadata.

3. **Build Script**: Ensure your build script copies `dnsApps.config` to the output:
   ```bash
   cp dnsApps.config "$OUTPUT_DIR/"
   ```

4. **csproj Reference**: Include the config file in your project:
   ```xml
   <ItemGroup>
     <None Include="dnsApps.config">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </None>
   </ItemGroup>
   ```

### Example: Auto Reverse DNS

```json
{
  "name": "Auto Reverse DNS",
  "version": "1.0.1",
  "description": "Automatically creates and maintains PTR records for A/AAAA records",
  "author": "TechniLux",
  "url": "https://github.com/elabx-org/technilux-apps",
  "appRecordDataTemplate": null,
  "configTemplate": {
    "enabled": true,
    "syncIntervalSeconds": 60,
    "createReverseZones": true
  }
}
```

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

## TechniLux UI Integration

Apps can provide a `ui-schema.json` file to enable a visual configuration interface in TechniLux UI. This eliminates the need for users to edit raw JSON.

**For complete documentation, see [`UI-SCHEMA-GUIDE.md`](./UI-SCHEMA-GUIDE.md)**

### Quick Start

1. Create `ui-schema.json` in your app directory
2. Add `schemaUrl` to `appstore.json`

### Two Approaches

| Approach | Use Case | Example |
|----------|----------|---------|
| **Dynamic Schema** | Most apps | Define fields in JSON |
| **Component Reference** | Complex UIs | Reference existing component |

### Example Schemas

| Schema | Complexity | Features Demonstrated |
|--------|------------|----------------------|
| [`schemas/no-data.json`](./schemas/no-data.json) | Simple | String list |
| [`schemas/drop-requests.json`](./schemas/drop-requests.json) | Medium | Object array, table columns, conditions |
| [`schemas/dns64.json`](./schemas/dns64.json) | Complex | Tabs, key-value maps, dynamic options |
| [`schemas/advanced-blocking-fork.json`](./schemas/advanced-blocking-fork.json) | Component | References existing component |
| [`AutoReverseDns/ui-schema.json`](./AutoReverseDns/ui-schema.json) | Full | Complete production example |

### Available Field Types

`switch` | `number` | `text` | `textarea` | `select` | `list` | `urlList` | `keyValue` | `objectArray` | `table` | `tabs` | `clientSelector` | `group`

### Available Components (for complex UIs)

`AdvancedBlockingConfig` | `AdvancedForwardingConfig` | `BlockPageConfig` | `Dns64Config` | `FailoverConfig` | `SplitHorizonConfig` | `ZoneAliasConfig` | `LogExporterConfig` | `QueryLogsMySqlConfig` | `QueryLogsSqliteConfig` | `DnsRebindingProtectionConfig` | `DropRequestsConfig` | `FilterAaaaConfig` | `MispConnectorConfig` | `NoDataConfig` | `NxDomainConfig` | `NxDomainOverrideConfig`

## Contributing

1. Fork the repository
2. Create feature branch
3. Make changes
4. Test thoroughly
5. Submit Pull Request

## License

MIT License - See LICENSE file
