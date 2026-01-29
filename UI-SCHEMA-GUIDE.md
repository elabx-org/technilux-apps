# TechniLux UI Schema Guide

This guide explains how to create configuration UIs for DNS apps using the TechniLux UI schema system.

## Overview

Apps can provide a `ui-schema.json` file that defines their configuration interface. TechniLux renders this schema as a visual editor, eliminating the need for users to edit raw JSON.

There are two approaches:

1. **Dynamic Schema** - Define fields, sections, and layout in JSON (recommended for most apps)
2. **Component Reference** - Reference an existing TechniLux component for complex UIs

## Quick Start

Create a `ui-schema.json` file in your app directory:

```json
{
  "version": "1.0",
  "appName": "My App",
  "description": "Brief description of what this app does",
  "sections": [
    {
      "id": "settings",
      "title": "Settings",
      "fields": [
        {
          "id": "enabled",
          "path": "enabled",
          "type": "switch",
          "label": "Enable App",
          "default": true
        }
      ]
    }
  ]
}
```

Then add the schema URL to `appstore.json`:

```json
{
  "apps": [
    {
      "name": "My App",
      "version": "1.0.0",
      "downloadUrl": "https://github.com/.../MyApp.zip",
      "schemaUrl": "https://raw.githubusercontent.com/.../ui-schema.json"
    }
  ]
}
```

## Schema Structure

### Root Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `version` | string | Yes | Schema version (use "1.0") |
| `appName` | string | Yes | Display name for the app |
| `description` | string | No | Brief description shown at the top |
| `sections` | array | No* | Array of form sections |
| `component` | string | No* | Reference to built-in component |
| `actions` | array | No | Action buttons (API calls) |
| `options` | object | No | Global options |

*Either `sections` or `component` must be provided.

### Sections

Sections group related fields together:

```json
{
  "id": "general",
  "title": "General Settings",
  "description": "Basic configuration options",
  "collapsible": true,
  "collapsed": false,
  "fields": [ ... ]
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `id` | string | Required | Unique section identifier |
| `title` | string | Required | Section heading |
| `description` | string | - | Explanatory text below heading |
| `collapsible` | boolean | true | Allow section to collapse |
| `collapsed` | boolean | false | Start collapsed |
| `showIf` | condition | - | Conditional visibility |
| `fields` | array | Required | Array of field definitions |

## Field Types

### Basic Fields

#### switch
Boolean toggle for on/off settings.

```json
{
  "id": "enabled",
  "path": "enabled",
  "type": "switch",
  "label": "Enable Feature",
  "description": "Turn this feature on or off",
  "default": true
}
```

#### number
Numeric input with optional constraints.

```json
{
  "id": "timeout",
  "path": "timeoutSeconds",
  "type": "number",
  "label": "Timeout",
  "description": "How long to wait before timing out",
  "default": 30,
  "min": 1,
  "max": 300,
  "step": 1,
  "suffix": "seconds"
}
```

#### text
Single-line text input.

```json
{
  "id": "serverUrl",
  "path": "server.url",
  "type": "text",
  "label": "Server URL",
  "placeholder": "https://example.com",
  "maxLength": 255
}
```

#### textarea
Multi-line text input.

```json
{
  "id": "customHtml",
  "path": "blockPage.html",
  "type": "textarea",
  "label": "Custom HTML",
  "placeholder": "<html>...</html>",
  "rows": 10
}
```

#### select
Dropdown with predefined options.

```json
{
  "id": "protocol",
  "path": "protocol",
  "type": "select",
  "label": "Protocol",
  "default": "tcp",
  "options": [
    { "value": "tcp", "label": "TCP", "description": "Standard TCP" },
    { "value": "udp", "label": "UDP", "description": "Faster but unreliable" },
    { "value": "tls", "label": "TLS", "description": "Encrypted" }
  ]
}
```

**Dynamic Options:** Pull options from another field in the config:

```json
{
  "id": "selectedGroup",
  "path": "selectedGroup",
  "type": "select",
  "label": "Group",
  "optionsFrom": "groups[*].name"
}
```

### Array Fields

#### list
Simple string array with add/remove.

```json
{
  "id": "blockedDomains",
  "path": "blockedDomains",
  "type": "list",
  "label": "Blocked Domains",
  "description": "Domains to block",
  "default": ["ads.example.com"],
  "itemPlaceholder": "Enter domain..."
}
```

#### urlList
URL list with optional per-item settings.

```json
{
  "id": "blockLists",
  "path": "blockListUrls",
  "type": "urlList",
  "label": "Block Lists",
  "itemSchema": {
    "fields": [
      {
        "id": "enabled",
        "path": "enabled",
        "type": "switch",
        "label": "Enabled",
        "default": true
      }
    ]
  }
}
```

### Complex Fields

#### keyValue
Key-value map with nested configuration per key.

```json
{
  "id": "zones",
  "path": "zones",
  "type": "keyValue",
  "label": "Zone Configuration",
  "keyLabel": "Zone Name",
  "keyPlaceholder": "example.com",
  "valueSchema": {
    "fields": [
      {
        "id": "enabled",
        "path": "enabled",
        "type": "switch",
        "label": "Enabled",
        "default": true
      }
    ]
  }
}
```

#### tabs
Tabbed interface for managing arrays like "groups".

```json
{
  "id": "groups",
  "path": "groups",
  "type": "tabs",
  "label": "Blocking Groups",
  "addLabel": "Group",
  "tabsOptions": {
    "nameField": "name",
    "defaultItem": {
      "name": "New Group",
      "enabled": true,
      "allowedDomains": [],
      "blockedDomains": []
    },
    "allowAdd": true,
    "allowDelete": true,
    "allowRename": true,
    "allowCopy": true,
    "minTabs": 1,
    "maxTabs": 50,
    "itemSchema": {
      "fields": [
        {
          "id": "name",
          "path": "name",
          "type": "text",
          "label": "Group Name"
        },
        {
          "id": "enabled",
          "path": "enabled",
          "type": "switch",
          "label": "Enabled"
        }
      ]
    }
  }
}
```

#### objectArray / table
Array of complex objects displayed as a table.

```json
{
  "id": "records",
  "path": "records",
  "type": "objectArray",
  "label": "DNS Records",
  "addLabel": "Record",
  "columns": [
    { "id": "name", "label": "Name", "path": "name", "width": "30%" },
    { "id": "type", "label": "Type", "path": "type", "type": "badge" },
    { "id": "value", "label": "Value", "path": "value" }
  ],
  "rowActions": [
    { "id": "edit", "icon": "edit", "label": "Edit" },
    { "id": "delete", "icon": "delete", "label": "Delete", "variant": "destructive" }
  ],
  "itemSchema": {
    "fields": [
      { "id": "name", "path": "name", "type": "text", "label": "Name" },
      { "id": "type", "path": "type", "type": "select", "label": "Type", "options": [...] },
      { "id": "value", "path": "value", "type": "text", "label": "Value" }
    ]
  }
}
```

#### clientSelector
Network device/IP selector with Network Helper integration.

```json
{
  "id": "clients",
  "path": "allowedClients",
  "type": "clientSelector",
  "label": "Allowed Clients",
  "clientSelectorOptions": {
    "multiple": true,
    "allowManualEntry": true,
    "showDevicePicker": true,
    "showHostnames": true
  }
}
```

#### group
Nested group of fields (visual grouping only).

```json
{
  "id": "advanced",
  "path": "advanced",
  "type": "group",
  "label": "Advanced Options",
  "groupFields": [
    { "id": "opt1", "path": "option1", "type": "switch", "label": "Option 1" },
    { "id": "opt2", "path": "option2", "type": "number", "label": "Option 2" }
  ]
}
```

## Conditional Visibility

Show or hide fields based on other field values:

```json
{
  "id": "customEndpoint",
  "path": "customEndpoint",
  "type": "text",
  "label": "Custom Endpoint",
  "showIf": {
    "field": "useCustomEndpoint",
    "operator": "eq",
    "value": true
  }
}
```

### Operators

| Operator | Description |
|----------|-------------|
| `eq` | Equal to value |
| `neq` | Not equal to value |
| `contains` | Array contains value |
| `notEmpty` | Value is not null/undefined/empty |
| `empty` | Value is null/undefined/empty |

### Examples

```json
// Show when toggle is enabled
"showIf": { "field": "advancedMode", "operator": "eq", "value": true }

// Hide when field is empty
"hideIf": { "field": "serverUrl", "operator": "empty" }

// Show when array contains value
"showIf": { "field": "features", "operator": "contains", "value": "logging" }
```

## Actions

Add buttons that call app API endpoints:

```json
{
  "actions": [
    {
      "id": "sync",
      "label": "Sync Now",
      "description": "Trigger manual sync",
      "method": "POST",
      "endpoint": "/sync",
      "variant": "default"
    },
    {
      "id": "clear",
      "label": "Clear Cache",
      "method": "POST",
      "endpoint": "/cache/clear",
      "variant": "destructive",
      "confirmMessage": "Are you sure you want to clear the cache?"
    }
  ]
}
```

## Component Reference

For complex UIs that can't be expressed in schema, reference a built-in component:

```json
{
  "version": "1.0",
  "appName": "My Advanced Blocking Fork",
  "description": "Fork of Advanced Blocking with custom features",
  "component": "AdvancedBlockingConfig"
}
```

### Available Components

| Component | Use Case |
|-----------|----------|
| `AdvancedBlockingConfig` | Group-based blocking with client targeting |
| `AdvancedForwardingConfig` | Group-based forwarding with domains/networks |
| `BlockPageConfig` | Custom block page HTML/settings |
| `Dns64Config` | DNS64 prefix and exclusions |
| `DnsRebindingProtectionConfig` | Rebinding protection with exclusions |
| `DropRequestsConfig` | Drop requests for domains/networks |
| `FailoverConfig` | Failover zones and health checks |
| `FilterAaaaConfig` | Filter AAAA records |
| `LogExporterConfig` | Syslog/file log export |
| `MispConnectorConfig` | MISP threat intel integration |
| `NoDataConfig` | Return NO DATA for record types |
| `NxDomainConfig` | Return NXDOMAIN for domains |
| `NxDomainOverrideConfig` | Override NXDOMAIN responses |
| `QueryLogsMySqlConfig` | MySQL query logging |
| `QueryLogsSqliteConfig` | SQLite query logging |
| `SplitHorizonConfig` | Split-horizon DNS |
| `ZoneAliasConfig` | Zone aliases |

## Best Practices

### 1. Use Descriptive Labels and Descriptions

```json
{
  "label": "Cache TTL",
  "description": "How long to cache DNS responses (0 = use record TTL)"
}
```

### 2. Provide Sensible Defaults

```json
{
  "default": 300,
  "min": 0,
  "max": 86400
}
```

### 3. Group Related Fields

```json
{
  "sections": [
    { "id": "general", "title": "General", "fields": [...] },
    { "id": "advanced", "title": "Advanced", "collapsed": true, "fields": [...] }
  ]
}
```

### 4. Use Conditional Visibility

Hide advanced options unless needed:

```json
{
  "id": "advancedMode",
  "type": "switch",
  "label": "Show Advanced Options"
},
{
  "id": "advancedSetting",
  "type": "text",
  "showIf": { "field": "advancedMode", "operator": "eq", "value": true }
}
```

### 5. Validate Input

```json
{
  "type": "text",
  "required": true,
  "pattern": "^https?://",
  "patternMessage": "Must be a valid URL starting with http:// or https://"
}
```

## Complete Example

See [`AutoReverseDns/ui-schema.json`](./AutoReverseDns/ui-schema.json) for a complete example featuring:
- Multiple sections
- Various field types
- Conditional visibility
- Key-value maps
- Actions

## Testing Your Schema

1. Install your app in TechniLux
2. Add your schema URL to `appstore.json`
3. Open the app's Config dialog
4. The visual editor should appear automatically
5. Use the "JSON Editor" toggle to verify data structure

## Troubleshooting

### Schema not loading
- Verify the `schemaUrl` is accessible (raw GitHub URL)
- Check browser console for fetch errors
- Ensure JSON is valid (use a JSON validator)

### Fields not appearing
- Check field `path` matches your config structure
- Verify `showIf` conditions are met
- Check section `showIf` conditions

### Values not saving
- Verify field `path` uses correct dot-notation
- Check console for validation errors
- Ensure config structure matches schema

## Migration from Custom Components

If you have an existing custom component and want to migrate to schema:

1. Identify all fields in your component
2. Map each to a schema field type
3. Preserve field paths exactly
4. Test with existing configs

For complex components that can't be fully migrated, use the hybrid approach:
- Create a minimal schema with `"component": "ExistingComponent"`
- The component handles the complex UI
- New apps can fork and customize the schema
