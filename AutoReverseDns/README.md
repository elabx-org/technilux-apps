# Auto Reverse DNS

Automatically creates and maintains PTR (reverse DNS) records for A/AAAA records in configured zones.

## Features

- **Automatic PTR Creation**: When you add an A or AAAA record, the corresponding PTR record is automatically created in the appropriate reverse zone
- **Per-Zone Control**: Enable/disable auto-reverse for specific zones
- **Duplicate Prevention**: Checks for existing PTR records before creating new ones
- **Reverse Zone Auto-Creation**: Optionally creates reverse zones (e.g., `0.0.10.in-addr.arpa`) if they don't exist
- **Cluster Support**: Option to only run on the primary node in cluster mode
- **Configurable Sync Interval**: Set how often the sync runs (default: 60 seconds)
- **IPv4 and IPv6 Support**: Handles both A records (in-addr.arpa) and AAAA records (ip6.arpa)

## Installation

1. Download `AutoReverseDns.zip` from the [releases page](https://github.com/elabx-org/technilux-apps/releases)
2. In Technitium DNS Server, go to **Apps** → **Install**
3. Upload the zip file

Or install via TechniLux UI:
1. Go to **Apps** → **App Store**
2. Find "Auto Reverse DNS" and click **Install**

## Configuration

### Global Settings

```json
{
  "enabled": true,
  "syncIntervalSeconds": 60,
  "createReverseZones": true,
  "overwriteExisting": false,
  "deleteOrphanedPtrs": false,
  "ptrTtl": 0,
  "clusterPrimaryOnly": true,
  "defaultZoneEnabled": false,
  "zones": {},
  "excludePatterns": [
    "localhost",
    "*.in-addr.arpa",
    "*.ip6.arpa"
  ]
}
```

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `enabled` | boolean | `true` | Global enable/disable switch |
| `syncIntervalSeconds` | integer | `60` | How often to scan for new records (seconds) |
| `createReverseZones` | boolean | `true` | Create reverse zones if they don't exist |
| `overwriteExisting` | boolean | `false` | Overwrite existing PTR records with different values |
| `deleteOrphanedPtrs` | boolean | `false` | Delete PTR records when A/AAAA records are removed |
| `ptrTtl` | integer | `0` | TTL for created PTR records (0 = use zone default) |
| `clusterPrimaryOnly` | boolean | `true` | In cluster mode, only run sync on primary node |
| `defaultZoneEnabled` | boolean | `false` | Enable auto-reverse for zones not explicitly configured |
| `zones` | object | `{}` | Per-zone configuration (see below) |
| `excludePatterns` | array | `[...]` | Zone patterns to exclude (supports wildcards) |

### Per-Zone Configuration

To enable auto-reverse for specific zones:

```json
{
  "enabled": true,
  "defaultZoneEnabled": false,
  "zones": {
    "home.lab": {
      "enabled": true
    },
    "example.com": {
      "enabled": true,
      "reverseZone": "168.192.in-addr.arpa",
      "createReverseZone": true,
      "overwriteExisting": false
    },
    "internal.local": {
      "enabled": false
    }
  }
}
```

| Zone Setting | Type | Default | Description |
|--------------|------|---------|-------------|
| `enabled` | boolean | `true` | Enable auto-reverse for this zone |
| `reverseZone` | string | (auto) | Override the reverse zone to use |
| `createReverseZone` | boolean | (global) | Override global createReverseZones setting |
| `overwriteExisting` | boolean | (global) | Override global overwriteExisting setting |

## How It Works

1. **Scanning**: The app periodically scans all enabled forward zones for A and AAAA records
2. **Reverse Zone Detection**: For each IP address, it calculates the appropriate reverse zone:
   - IPv4: `10.0.0.15` → zone `0.0.10.in-addr.arpa`, PTR `15.0.0.10.in-addr.arpa`
   - IPv6: Uses /64 boundary for zone, full address for PTR
3. **Duplicate Check**: Before creating a PTR, it checks if one already exists
4. **PTR Creation**: Creates the PTR record pointing to the hostname

## Example

Given this A record in `home.lab`:
```
test.home.lab    A    10.0.0.50
```

The app will automatically create:
```
50.0.0.10.in-addr.arpa    PTR    test.home.lab
```

In the reverse zone `0.0.10.in-addr.arpa` (creating it if necessary).

## Cluster Mode

When running in a Technitium cluster:

- Set `clusterPrimaryOnly: true` (default) to only run sync on the primary node
- This prevents duplicate operations and potential conflicts
- The primary node will create PTR records that replicate to secondary nodes

## API Endpoints

The app provides HTTP endpoints for management:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/autoreversedns/sync` | POST | Trigger manual sync |
| `/api/autoreversedns/stats` | GET | Get sync statistics |
| `/api/autoreversedns/zones` | GET | List zones with status |
| `/api/autoreversedns/zones/toggle` | POST | Enable/disable a zone |

### Trigger Manual Sync

```bash
curl -X POST "http://localhost:5380/api/apps/autoreversedns/sync?token=YOUR_TOKEN"
```

### Get Statistics

```bash
curl "http://localhost:5380/api/apps/autoreversedns/stats?token=YOUR_TOKEN"
```

Response:
```json
{
  "success": true,
  "stats": {
    "lastSyncResult": {
      "timestamp": "2024-01-28T12:00:00Z",
      "zonesProcessed": 3,
      "recordsScanned": 45,
      "ptrsCreated": 5,
      "ptrsUpdated": 0,
      "ptrsSkipped": 40,
      "errors": []
    },
    "totalSyncs": 100,
    "totalPtrsCreated": 50,
    "isRunning": false,
    "nextSyncTime": "2024-01-28T12:01:00Z"
  }
}
```

### Toggle Zone

```bash
curl -X POST "http://localhost:5380/api/apps/autoreversedns/zones/toggle?token=YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"zone": "home.lab", "enabled": true}'
```

## Troubleshooting

### PTRs Not Being Created

1. Check the app is enabled: `"enabled": true`
2. Check the zone is enabled in config or `defaultZoneEnabled` is true
3. Check the reverse zone exists or `createReverseZones` is true
4. Check Technitium logs for errors: `docker logs technitium`

### Duplicate PTR Records

The app checks for existing PTRs before creating. If you're seeing duplicates:
1. Existing PTRs may have been created manually
2. Set `overwriteExisting: true` to replace them

### Cluster Conflicts

If running in cluster mode and seeing issues:
1. Ensure `clusterPrimaryOnly: true` is set
2. Only the primary node should run sync
3. PTR records will replicate via zone transfer

## Building from Source

```bash
cd AutoReverseDns
./build.sh
```

Requires:
- .NET 9.0 SDK
- Docker (for extracting Technitium DLLs)

## License

MIT
