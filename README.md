# Advanced Blocking Plus

A TechniLux-enhanced DNS app for Technitium DNS Server that extends the original Advanced Blocking app to support **multiple groups per client IP address**.

## Features

All features from the original Advanced Blocking app, **PLUS**:

| Feature | Advanced Blocking | Advanced Blocking Plus |
|---------|------------------|------------------------|
| Groups per client | **ONE** (longest prefix match) | **MULTIPLE** (all matching) |
| Rule aggregation | N/A | Allow rules from ANY group take priority |
| Backward compatible | N/A | Yes - single string values still work |

## Configuration

The `networkGroupMap` now accepts arrays:

```json
{
  "networkGroupMap": {
    "10.0.0.174": ["group1", "group2"],
    "192.168.1.0/24": ["everyone", "kids"],
    "10.0.0.1": "single-group"
  }
}
```

**Backward compatible**: Single string values still work for clients that only need one group.

## Blocking Logic

1. **Allow rules take priority**: If a domain is allowed in ANY of the client's groups, it's allowed
2. **Block rules aggregate**: If a domain is blocked in ANY group (and not allowed in any), it's blocked
3. **First match wins for response**: The first group that blocks determines the response format (NXDOMAIN vs 0.0.0.0)

## Example Use Case

You have a device at `10.0.0.174` that needs rules from both:
- `block-apple-signing-main` (blocks mesu.apple.com, gdmf.apple.com)
- `block-apple-signing-switch` (blocks appldnld.apple.com, gg.apple.com)

**With Advanced Blocking**: Can only assign ONE group, must merge rules manually
**With Advanced Blocking Plus**: Assign BOTH groups, toggle each independently

```json
{
  "networkGroupMap": {
    "10.0.0.174": ["block-apple-signing-main", "block-apple-signing-switch"]
  }
}
```

## Building the App

### Prerequisites

1. .NET 9.0 SDK
2. Docker (for extracting Technitium DLLs)

### Quick Build

```bash
./build.sh
```

### Manual Build

1. Extract DLLs from Technitium:
   ```bash
   mkdir -p lib
   docker pull technitium/dns-server:latest
   CONTAINER_ID=$(docker create technitium/dns-server:latest)
   docker cp "$CONTAINER_ID:/opt/technitium/dns/TechnitiumLibrary.dll" lib/
   docker cp "$CONTAINER_ID:/opt/technitium/dns/TechnitiumLibrary.Net.dll" lib/
   docker cp "$CONTAINER_ID:/opt/technitium/dns/DnsServerCore.ApplicationCommon.dll" lib/
   docker rm "$CONTAINER_ID"
   ```

2. Build:
   ```bash
   dotnet build -c Release
   ```

3. Output:
   ```
   bin/Release/net9.0/AdvancedBlockingPlus.dll
   ```

## Installation

### Method 1: Via Technitium GUI

1. Open Technitium DNS Server web UI
2. Go to Apps section
3. Click "Install" and upload the `AdvancedBlockingPlus.dll` file
4. The app will be installed to `/etc/dns/apps/AdvancedBlockingPlus/`

### Method 2: Manual Installation

```bash
mkdir -p /etc/dns/apps/AdvancedBlockingPlus
cp bin/Release/net9.0/AdvancedBlockingPlus.dll /etc/dns/apps/AdvancedBlockingPlus/
cp dnsApp.config /etc/dns/apps/AdvancedBlockingPlus/
# Restart Technitium DNS Server
```

## Configuration Reference

```json
{
  "enableBlocking": true,
  "blockingAnswerTtl": 30,
  "blockListUrlUpdateIntervalHours": 24,
  "blockListUrlUpdateIntervalMinutes": 0,
  "localEndPointGroupMap": {
    "dns.example.com": ["group1"],
    "192.168.1.1:53": ["bypass"]
  },
  "networkGroupMap": {
    "10.0.0.174": ["group1", "group2"],
    "192.168.0.0/16": ["everyone"],
    "0.0.0.0/0": ["default"]
  },
  "groups": [
    {
      "name": "group1",
      "enableBlocking": true,
      "allowTxtBlockingReport": true,
      "blockAsNxDomain": false,
      "blockingAddresses": ["0.0.0.0", "::"],
      "allowed": ["safe.example.com"],
      "blocked": ["ads.example.com"],
      "allowListUrls": [],
      "blockListUrls": ["https://example.com/blocklist.txt"],
      "allowedRegex": [],
      "blockedRegex": ["^tracking\\."],
      "regexAllowListUrls": [],
      "regexBlockListUrls": [],
      "adblockListUrls": []
    }
  ]
}
```

## Migrating from Advanced Blocking

1. Export your Advanced Blocking configuration
2. Update `networkGroupMap` entries to use arrays where you want multiple groups
3. Import into Advanced Blocking Plus

Your existing single-group configs will work unchanged.

## License

Based on Technitium Advanced Blocking App by Shreyas Zare.
Licensed under GNU General Public License v3.0.

---

**Made by TechniLux**
