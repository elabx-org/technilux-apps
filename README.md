# TechniLux Apps

DNS Server applications for Technitium DNS Server, designed for the TechniLux UI.

## Installation via App Store

**Recommended Method:**

1. Open Technitium DNS Server → Apps
2. Click "Store URLs" → Add:
   ```
   https://raw.githubusercontent.com/elabx-org/technilux-apps/main/appstore.json
   ```
3. Browse TechniLux apps and click "Install"

## Available Apps

### Advanced Blocking Plus

Enhanced version of Advanced Blocking with support for MULTIPLE groups per client IP address.

**Features:**
- All original Advanced Blocking features
- Assign multiple groups to each client IP/subnet
- Rule aggregation from different groups
- Allow rules from ANY group take priority
- Backward compatible with single-group configs

[Read more →](./AdvancedBlockingPlus/README.md)

**Download:** [AdvancedBlockingPlus.zip](https://github.com/elabx-org/technilux-apps/releases/latest/download/AdvancedBlockingPlus.zip) (21 KB)

---

### Network Helper

Persistent network client information and hostname mappings.

**Features:**
- Store PTR-resolved hostnames with timestamps
- Device metadata (custom names, notes, tags, groups, icons)
- Source tracking (PTR, DHCP, ARP, manual)
- Auto-cleanup of stale entries
- Bulk import/export (CSV, JSON)
- RESTful API for CRUD operations

[Read more →](./NetworkHelper/README.md)

**Download:** [NetworkHelper.zip](https://github.com/elabx-org/technilux-apps/releases/latest/download/NetworkHelper.zip) (50 KB)

---

## Building from Source

### Requirements
- .NET 8.0 SDK
- Technitium DNS Server v12.0+ (for extracting DLLs)
- Docker (optional, for DLL extraction)

### Build Advanced Blocking Plus

```bash
cd AdvancedBlockingPlus
./build.sh
```

### Build Network Helper

```bash
cd NetworkHelper
./build.sh
```

## Manual Installation

1. Download ZIP files from [releases](https://github.com/elabx-org/technilux-apps/releases)
2. Navigate to Technitium DNS Server → Apps
3. Click "Install" and upload the ZIP file
4. Configure app settings as needed

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## Compatibility

- **Technitium DNS**: v12.0+
- **.NET**: 8.0+
- **Platform**: Linux (amd64, arm64), Windows

## License

- **Advanced Blocking Plus**: GNU General Public License v3.0 (based on Technitium Advanced Blocking)
- **Network Helper**: MIT License

## Author

TechniLux - Part of the elabx-org ecosystem

**Made for:** [TechniLux UI](https://github.com/elabx-org/technitium-ui)
