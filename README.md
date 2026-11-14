# 🛡️ Sraosha (`jellyfin-plugin-sraosha`)

> *"In Zoroastrian myth, Sraosha is the divine, sleepless sentinel who never rests—patrolling creation with his staff raised, guarding against negligence, chaos, and stalled quests while the world sleeps."*

**Sraosha** is a native Jellyfin plugin that acts as an automated guardian for your media pipeline. It inspects **Jellyseerr** / **Overseerr** for requests that are stalled in processing (e.g. downloads manually purged from qBittorrent, stuck indexers, or forgotten items), verifies whether files are actually missing in **Radarr** and **Sonarr**, and automatically resurrects the download queue.

---

## ⚡ The Problem It Solves

When running an automated *arr media stack:
1. A user requests a movie or series in **Jellyseerr**.
2. A download is started, but later gets manually removed from your download client (e.g. qBittorrent / SABnzbd), corrupted, or rejected by indexers.
3. Radarr and Sonarr lose track of the search or leave the item unmonitored.
4. Jellyseerr remains indefinitely in the **"Processing"** state—it never retries by itself, and the media is never delivered.

**Sraosha solves this silently and automatically.**

---

## 🌟 Key Features

- 🕒 **Native Scheduled Task (`IScheduledTask`)**: Integrates directly into Jellyfin's task scheduler (default: runs every 6 hours; customizable schedule via Jellyfin Dashboard).
- 🎬 **Multi-Instance Radarr & Sonarr**: Fully supports multiple instances (e.g., standard 1080p, 4K HDR, and Anime instances).
- ⏱️ **Configurable Stale Threshold**: Avoids false triggers by ignoring freshly submitted requests (default: 2 hours).
- 🧭 **Trakt Watch History Guard**: If you already watched a movie or series and intentionally deleted it to save space, Sraosha skips re-downloading it.
- 🧪 **Dry-Run Mode**: Safely test and preview exactly what would be searched without sending command calls to your downloaders.
- 🔔 **Discord & Webhook Alerts**: Receive clean notifications with item titles whenever downloads are revived.
- 🎛️ **Integrated Dashboard UI**: Embedded configuration page with connection test buttons for every instance and an immediate **"Run Scan Now"** trigger.

---

## 🔄 How It Works

```
┌──────────────────────────────────────────────────────────────┐
│                    Jellyfin Server (Sraosha)                 │
│                                                              │
│  Scheduled Task (Every 6h) or Manual "Run Scan Now"          │
└───────────────┬───────────────────────────────┬──────────────┘
                │ 1. Query Processing Requests   │ 2. (Optional) Check Watch History
                ▼                               ▼
       ┌─────────────────┐             ┌─────────────────┐
       │   Jellyseerr    │             │      Trakt      │
       └────────┬────────┘             └─────────────────┘
                │
                │ Filter by Age (e.g. > 2 hours)
                ▼
        ┌───────────────┴───────────────┐
        │                               │
        ▼ Movies                        ▼ TV Series
┌──────────────────────┐        ┌──────────────────────┐
│   Radarr Instance(s) │        │   Sonarr Instance(s) │
├──────────────────────┤        ├──────────────────────┤
│ 1. Match TMDb ID     │        │ 1. Match TVDb ID     │
│ 2. Check HasFile     │        │ 2. Find missing eps  │
│ 3. Ensure Monitored  │        │ 3. Filter watched    │
│ 4. Trigger Search    │        │ 4. Batch Search      │
└──────────────────────┘        └──────────────────────┘
```

---

## 📦 Installation

### Method A: Plugin Repository (Recommended)

1. In Jellyfin, go to **Dashboard** → **Plugins** → **Repositories** tab.
2. Click **+ Add** and paste the manifest URL:
   ```
   https://raw.githubusercontent.com/ghahramani/jellyfin-plugin-sraosha/main/manifest.json
   ```
3. Set the name to **Sraosha** and save.
4. Switch to the **Catalog** tab, locate **Sraosha**, and click **Install**.
5. Restart your Jellyfin server.

---

### Method B: Manual Download

1. Download the latest `Jellyfin.Plugin.Sraosha_x.x.x.x.zip` from the [Releases](https://github.com/ghahramani/jellyfin-plugin-sraosha/releases) page.
2. Extract the archive into your Jellyfin plugins directory:
   ```bash
   # Linux / Docker example:
   mkdir -p /config/plugins/Sraosha_1.0.0.0
   unzip Jellyfin.Plugin.Sraosha_1.0.0.0.zip -d /config/plugins/Sraosha_1.0.0.0/
   ```
3. Restart your Jellyfin container or service.

---

### Method C: Local Build

```bash
git clone https://github.com/ghahramani/jellyfin-plugin-sraosha.git
cd jellyfin-plugin-sraosha

# Build and generate ZIP in dist/
./build.sh

# Or build and automatically install into local Jellyfin test instance:
./build.sh --install
```

---

## ⚙️ Configuration

Navigate to **Jellyfin Dashboard** → **Plugins** → **Sraosha**:

| Setting | Description | Default |
|---|---|---|
| **Stale Threshold (Hours)** | Minimum age before a processing request is considered stalled | `2` |
| **Dry Run Mode** | Logs what would be searched without triggering real downloads | `false` |
| **Jellyseerr URL & API Key** | URL (`http://192.168.1.10:5055`) and API Key from Settings → General | *Required* |
| **Radarr Instances** | Add one or more Radarr URLs and API keys | *At least one* |
| **Sonarr Instances** | Add one or more Sonarr URLs and API keys | *At least one* |
| **Trakt Integration** | Optional Trakt Client ID and username to skip already-watched media | `Disabled` |
| **Webhook URL** | Optional Discord/ntfy/Slack webhook for summary alerts | `Disabled` |

---

## 🛠️ Building & Compatibility

| Component | Version |
|---|---|
| **Jellyfin Target ABI** | `10.9.x`, `10.10.x`, `10.11.x`, `12.0.x` |
| **Target Framework** | .NET 8.0 (standard) / .NET 10.0 (Jellyfin 12 preview) |

```bash
# Standard .NET 8 build
dotnet build Jellyfin.Plugin.Sraosha -c Release

# Preview .NET 10 build (Jellyfin 12 preview)
docker run --rm -v $(pwd):/src -w /src/Jellyfin.Plugin.Sraosha mcr.microsoft.com/dotnet/sdk:10.0-preview dotnet build -c Release -p:NET10=true
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
