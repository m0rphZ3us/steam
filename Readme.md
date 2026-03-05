# SteamWatcher + SteamAchievementWorker

Dieses Projekt erkennt automatisch gestartete **Steam-Spiele** und synchronisiert **Achievements live** mit dem Backend.

Es besteht aus zwei Programmen:

* **SteamWatcher** → erkennt gestartete Spiele
* **SteamAchievementWorker** → liest Achievements über Steamworks aus

Logs werden in eine Datei geschrieben und können z.B. mit **Promtail + Loki** weiterverarbeitet werden.

---

# Voraussetzungen

Installiert sein müssen:

* **.NET 8 SDK**
* **Steam**
* **Steamworks.NET**
* **Promtail (optional für Logging)**

Prüfen:

```
dotnet --version
```

Sollte z.B. anzeigen:

```
8.0.x
```

---

# Repository klonen

```
git clone <repo-url>
cd steam
```

---

# Ordner für Logs erstellen

Die Anwendungen schreiben Logs nach:

```
C:\steam-logs\steam.log
```

Ordner erstellen:

```
mkdir C:\steam-logs
```

---

# Worker bauen

```
dotnet publish SteamAchievementWorker -c Release
```

Output:

```
SteamAchievementWorker/bin/Release/net8.0/publish/
```

Wichtig: In diesem Ordner muss auch liegen:

```
steam_api64.dll
```

---

# Watcher bauen

```
dotnet publish SteamWatcher -c Release
```

Output:

```
SteamWatcher/bin/Release/net8.0/publish/
```

Beim Publish wird automatisch der Worker kopiert:

```
SteamWatcher.exe
SteamAchievementWorker.exe
appsettings.json
```

Das passiert über diesen Build-Step:

```
<Target Name="CopyWorker" AfterTargets="Publish">
```

---

# Deploy-Verzeichnis

Empfohlen:

```
C:\steam\
```

Dateien kopieren:

```
SteamWatcher.exe
SteamAchievementWorker.exe
appsettings.json
steam_api64.dll
```

Endstruktur:

```
C:\steam
 ├ SteamWatcher.exe
 ├ SteamAchievementWorker.exe
 ├ steam_api64.dll
 └ appsettings.json
```

---

# Backend URL konfigurieren

Datei:

```
appsettings.json
```

Beispiel:

```
{
  "Watcher": {
    "StatusUrl": "http://192.168.1.163:8093/steam/status"
  }
}
```

---

# Test lokal starten

```
C:\steam\SteamWatcher.exe
```

Log prüfen:

```
C:\steam-logs\steam.log
```

Beispiel:

```
INFO 1 --- [steam] SteamWatcher : event=watcher_started
INFO 1 --- [steam] SteamWatcher : event=manifest_loaded count=87
```

Beim Start eines Spiels:

```
event=game_started appId=2393160
```

---

# Task Scheduler Setup

Damit der Watcher automatisch startet.

## Task Scheduler öffnen

```
Win + R
taskschd.msc
```

---

## Neuer Task

Name:

```
SteamWatcher
```

Optionen:

* Run whether user is logged on or not
* Run with highest privileges

---

## Trigger

```
At log on
```

oder

```
At startup
```

---

## Action

Program:

```
C:\steam\SteamWatcher.exe
```

Start in:

```
C:\steam
```

---

# Promtail Setup (optional)

Promtail liest die Logs aus:

```
C:\steam-logs\steam.log
```

Promtail starten:

```
C:\promtail\promtail-windows-amd64.exe -config.file=C:\promtail\config.yml
```

Auch hierfür kann ein eigener **Task Scheduler Task** erstellt werden.

---

# Logs

Alle Logs landen hier:

```
C:\steam-logs\steam.log
```

Beispiele:

```
event=watcher_started
event=manifest_loaded
event=game_started
event=achievement_unlocked
event=achievement_sync_sent
```

---

# Architektur

```
SteamWatcher
     │
     │ erkennt laufendes Spiel
     ▼
SteamAchievementWorker
     │
     │ liest Achievements über Steamworks
     ▼
Backend API
     │
     ▼
Frontend (Live Update via SSE)
```

---

# Troubleshooting

## Worker startet nicht

Prüfen ob vorhanden:

```
SteamAchievementWorker.exe
```

im gleichen Ordner wie

```
SteamWatcher.exe
```

---

## Steam API Fehler

Prüfen ob vorhanden:

```
steam_api64.dll
```

im selben Ordner wie:

```
SteamAchievementWorker.exe
```

---

## Keine Logs

Ordner prüfen:

```
C:\steam-logs
```

---

# Entwicklung

Watcher starten:

```
dotnet run --project SteamWatcher
```

Worker starten:

```
dotnet run --project SteamAchievementWorker 2393160
```

---

# Lizenz

Privates Projekt.
