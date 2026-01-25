# Advanced Forwarding Plus

A TechniLux-enhanced DNS app for Technitium DNS Server that extends the original Advanced Forwarding app to support **multiple groups per client IP address**.

## Features

All features from the original Advanced Forwarding app, **PLUS**:

| Feature | Advanced Forwarding | Advanced Forwarding Plus |
|---------|---------------------|--------------------------|
| Groups per client | **ONE** (longest prefix match) | **MULTIPLE** (all matching) |
| Forwarder aggregation | N/A | Forwarders from ALL groups are combined |
| Backward compatible | N/A | Yes - single string values still work |

## Configuration

The `networkGroupMap` now accepts arrays:

```json
{
  "networkGroupMap": {
    "10.0.0.174": ["group1", "group2"],
    "192.168.1.0/24": ["everyone", "special"],
    "10.0.0.1": "single-group"
  }
}
```

**Backward compatible**: Single string values still work for clients that only need one group.

## Forwarding Logic

1. **Client lookup**: Find all groups assigned to the client IP (longest prefix match, with same-length prefixes merged)
2. **Domain matching**: Check if the queried domain matches ANY forwarding rule in ANY of the client's groups
3. **Forwarder aggregation**: Combine forwarders from all matching groups (duplicates removed)
4. **First match priority**: More specific domain matches take priority (exact > wildcard)

## Example Use Cases

### Use Case 1: Device needs rules from multiple groups

You have a device at `10.0.0.174` that needs forwarding rules from both:
- `local-network` (forwards `*.local`, `*.lan` to router)
- `special-domains` (forwards `*.internal.company.com` to corporate DNS)

**With Advanced Forwarding**: Can only assign ONE group, must duplicate rules manually
**With Advanced Forwarding Plus**: Assign BOTH groups, manage each independently

```json
{
  "networkGroupMap": {
    "10.0.0.174": ["local-network", "special-domains"]
  }
}
```

### Use Case 2: Selective forwarding per device

Forward certain devices' queries for specific domains to your router while using regular DNS for everything else:

```json
{
  "forwarders": [
    {
      "name": "router",
      "dnssecValidation": false,
      "forwarderProtocol": "Udp",
      "forwarderAddresses": ["192.168.1.1"]
    }
  ],
  "networkGroupMap": {
    "10.0.0.100": ["forward-to-router"],
    "10.0.0.101": ["forward-to-router"],
    "0.0.0.0/0": "default"
  },
  "groups": [
    {
      "name": "default",
      "enableForwarding": false,
      "forwardings": []
    },
    {
      "name": "forward-to-router",
      "enableForwarding": true,
      "forwardings": [
        {
          "forwarders": ["router"],
          "domains": ["*.mydomain.com", "mydomain.com"]
        }
      ]
    }
  ]
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
   bin/Release/net9.0/AdvancedForwardingPlus.dll
   ```

## Installation

### Method 1: Via Technitium GUI

1. Open Technitium DNS Server web UI
2. Go to Apps section
3. Click "Install" and upload the `AdvancedForwardingPlus.zip` file
4. The app will be installed to `/etc/dns/apps/AdvancedForwardingPlus/`

### Method 2: Manual Installation

```bash
mkdir -p /etc/dns/apps/AdvancedForwardingPlus
cp bin/Release/net9.0/AdvancedForwardingPlus.dll /etc/dns/apps/AdvancedForwardingPlus/
cp dnsApp.config /etc/dns/apps/AdvancedForwardingPlus/
# Restart Technitium DNS Server
```

## Configuration Reference

```json
{
  "enableForwarding": true,
  "appPreference": 200,
  "proxyServers": [
    {
      "name": "my-proxy",
      "type": "Http",
      "proxyAddress": "proxy.example.com",
      "proxyPort": 8080,
      "proxyUsername": null,
      "proxyPassword": null
    }
  ],
  "forwarders": [
    {
      "name": "cloudflare-doh",
      "proxy": null,
      "dnssecValidation": true,
      "forwarderProtocol": "Https",
      "forwarderAddresses": [
        "https://cloudflare-dns.com/dns-query"
      ]
    },
    {
      "name": "router",
      "dnssecValidation": false,
      "forwarderProtocol": "Udp",
      "forwarderAddresses": [
        "192.168.1.1"
      ]
    }
  ],
  "networkGroupMap": {
    "10.0.0.174": ["group1", "group2"],
    "192.168.0.0/16": ["everyone"],
    "0.0.0.0/0": ["default"]
  },
  "groups": [
    {
      "name": "group1",
      "enableForwarding": true,
      "forwardings": [
        {
          "forwarders": ["router"],
          "domains": ["*.local", "*.lan"]
        }
      ],
      "adguardUpstreams": [
        {
          "configFile": "upstreams.txt",
          "proxy": null,
          "dnssecValidation": true
        }
      ]
    }
  ]
}
```

### Forwarder Protocol Options

- `Udp` - Standard DNS over UDP (port 53)
- `Tcp` - DNS over TCP (port 53)
- `Tls` - DNS over TLS (DoT, port 853)
- `Https` - DNS over HTTPS (DoH)
- `Quic` - DNS over QUIC (DoQ)

### AdGuard Upstreams

You can also use AdGuard-style upstream configuration files:

```
# upstreams.txt
# Default upstream
1.1.1.1

# Domain-specific
[/local/lan/]192.168.1.1
[/example.com/]8.8.8.8
```

## Migrating from Advanced Forwarding

1. Export your Advanced Forwarding configuration
2. Update `networkGroupMap` entries to use arrays where you want multiple groups
3. Import into Advanced Forwarding Plus

Your existing single-group configs will work unchanged.

## Comparison with Advanced Blocking Plus

| Feature | Advanced Blocking Plus | Advanced Forwarding Plus |
|---------|------------------------|--------------------------|
| Purpose | Multi-group blocking rules | Multi-group forwarding rules |
| Rule priority | Allow rules from ANY group take priority | All forwarders from matching groups aggregated |
| Domain matching | Block/allow domain lists | Forward domain lists |

## License

Based on Technitium Advanced Forwarding App by Shreyas Zare.
Licensed under GNU General Public License v3.0.

---

**Made by TechniLux**
