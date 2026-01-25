# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**Repository Info**: Default branch is `main` (not `master`).

## Project Overview

**TechniLux Apps** - Custom DNS apps for Technitium DNS Server that extend the official apps with additional features, primarily multi-group support for client IP assignments.

## Quick Start

```bash
# Build an app locally (requires Docker for extracting Technitium DLLs)
cd AdvancedForwardingPlus
./build.sh

# Or trigger a release build via GitHub Actions
git tag v1.0.2-appname
git push origin v1.0.2-appname
```

## Repository Structure

```
technilux-apps/
├── .github/workflows/
│   └── release.yml              # Automated build and release
├── AdvancedBlockingPlus/        # Multi-group blocking app
│   ├── App.cs                   # Main app implementation
│   ├── AdvancedBlockingPlus.csproj
│   ├── dnsApp.config            # Default runtime configuration
│   └── build.sh                 # Local build script
├── AdvancedForwardingPlus/      # Multi-group forwarding app
│   ├── App.cs
│   ├── AdvancedForwardingPlus.csproj
│   ├── dnsApp.config
│   └── build.sh
├── NetworkHelper/               # Network client metadata app
│   ├── App.cs
│   ├── NetworkHelper.csproj
│   ├── dnsApps.config           # App metadata config
│   └── build.sh
└── appstore.json                # App catalog for TechniLux UI
```

## Building Apps

### Prerequisites

- .NET 9.0 SDK
- Docker (for extracting Technitium reference DLLs)

### Local Build

Each app has a `build.sh` script that:
1. Extracts required DLLs from `technitium/dns-server:latest` Docker image
2. Builds the app with `dotnet build`
3. Creates a distribution zip

```bash
cd AdvancedForwardingPlus
./build.sh
# Output: AdvancedForwardingPlus.zip
```

### GitHub Actions Release

Push a tag to trigger automated builds:

```bash
git tag v1.0.2-advancedforwardingplus
git push origin v1.0.2-advancedforwardingplus
```

The workflow will:
1. Extract Technitium DLLs from Docker
2. Build all apps
3. Create/update the GitHub release with zip artifacts

## App Development

### Required References

Apps reference these DLLs from Technitium (extracted via Docker):
- `TechnitiumLibrary.dll`
- `TechnitiumLibrary.Net.dll`
- `DnsServerCore.ApplicationCommon.dll`

These are set as `<Private>false</Private>` in csproj to avoid bundling them.

### App Interfaces

Apps implement interfaces from `DnsServerCore.ApplicationCommon`:
- `IDnsApplication` - Required for all apps
- `IDnsRequestBlockingHandler` - For blocking apps
- `IDnsAuthoritativeRequestHandler` - For forwarding apps
- `IDnsApplicationPreference` - For setting app priority

### Version Configuration

Set version in the `.csproj` file:

```xml
<PropertyGroup>
  <Version>1.0.1</Version>
  <AssemblyVersion>1.0.1.0</AssemblyVersion>
  <FileVersion>1.0.1.0</FileVersion>
  <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
</PropertyGroup>
```

**IMPORTANT**: Also update `appstore.json` when changing versions.

## Critical: App Version Detection

Technitium reads app versions through a specific mechanism that requires careful attention.

### How Technitium Loads Apps

1. Technitium scans the app folder for `*.deps.json` files
2. For each deps.json, it loads the corresponding DLL
3. It looks for classes implementing `IDnsApplication`
4. Version is read from `Assembly.GetName().Version`

### Required Files in App Zip

Every app zip **MUST** include:
- `AppName.dll` - The compiled app assembly
- `AppName.deps.json` - **CRITICAL** - Required for Technitium to load the DLL
- `dnsApp.config` or `dnsApps.config` - Default configuration

### Common Version Issues

#### Issue: App shows "v0.0"

**Cause**: Missing `.deps.json` file in the zip.

**Solution**: Ensure the workflow/build script copies the deps.json:
```yaml
cp bin/Release/net9.0/AppName.deps.json "$OUTPUT_DIR/"
```

#### Issue: App shows "v1.0" instead of actual version

**Cause**: The deps.json is present but version reading failed. This can happen with version `1.0.0.0` specifically.

**Solution**: Use a version like `1.0.1` instead of `1.0.0`. The fallback version `1.0` is used when:
- `dnsApplications.Count > 0` (app loaded successfully)
- But `Assembly.GetName().Version` returned null

#### Debugging Version Issues

1. Check zip contents include deps.json:
```bash
python3 -c "import zipfile; z = zipfile.ZipFile('App.zip'); print([i.filename for i in z.infolist()])"
```

2. Verify DLL has correct version embedded:
```bash
strings AppName.dll | grep -E "^[0-9]+\.[0-9]+\.[0-9]+"
```

3. Test loading the assembly:
```csharp
var asm = Assembly.LoadFile("AppName.dll");
Console.WriteLine(asm.GetName().Version); // Should print version
```

## Workflow Configuration

The `.github/workflows/release.yml` builds all apps:

```yaml
# Key steps for each app:
- name: Build App
  run: |
    dotnet restore
    dotnet build AppName.csproj -c Release --no-restore

    OUTPUT_DIR="./build"
    mkdir -p "$OUTPUT_DIR"
    cp bin/Release/net9.0/AppName.dll "$OUTPUT_DIR/"
    cp bin/Release/net9.0/AppName.deps.json "$OUTPUT_DIR/"  # CRITICAL!
    cp dnsApp.config "$OUTPUT_DIR/"

    cd "$OUTPUT_DIR"
    zip -j ../AppName.zip ./*
```

## App Store Integration

The `appstore.json` file is fetched by TechniLux UI to display available apps:

```json
{
  "name": "TechniLux Apps",
  "apps": [
    {
      "name": "Advanced Forwarding Plus",
      "version": "1.0.1",
      "description": "...",
      "downloadUrl": "https://github.com/elabx-org/technilux-apps/releases/download/v1.0.1-advancedforwardingplus/AdvancedForwardingPlus.zip"
    }
  ]
}
```

**Important**: Update `downloadUrl` when creating new release tags.

## Testing Apps

### Install via Technitium API

```bash
TOKEN="your-admin-token"
curl -X POST "http://localhost:5380/api/apps/install?token=$TOKEN&name=App%20Name" \
  -F "fileApp=@AppName.zip"
```

### Verify Installation

```bash
curl "http://localhost:5380/api/apps/list?token=$TOKEN" | jq '.response.apps[] | {name, version}'
```

### Check App Config

```bash
curl "http://localhost:5380/api/apps/config/get?token=$TOKEN&name=App%20Name"
```

## Multi-Group Feature

The "Plus" apps (Advanced Blocking Plus, Advanced Forwarding Plus) extend the originals with multi-group support:

### Original Behavior
- Each client IP maps to **ONE** group (longest prefix match)
- Rules from only that group apply

### Plus Behavior
- Each client IP can map to **MULTIPLE** groups
- Rules from ALL matching groups are aggregated
- Backward compatible with single-string group assignments

### Configuration Example

```json
{
  "networkGroupMap": {
    "10.0.0.174": ["group1", "group2"],
    "192.168.1.0/24": ["everyone", "special"],
    "10.0.0.1": "single-group"
  }
}
```

## Troubleshooting

### Build Fails: Missing DLLs

Ensure Docker is running and can pull the Technitium image:
```bash
docker pull technitium/dns-server:latest
```

### App Not Loading

Check Technitium logs for errors. Common issues:
- Missing deps.json (app won't load at all)
- Interface mismatch (wrong Technitium version)
- Configuration parse errors

### Version Shows Incorrectly

See "Critical: App Version Detection" section above.
