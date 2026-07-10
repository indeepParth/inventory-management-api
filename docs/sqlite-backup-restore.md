# SQLite Backup and Restore

This API uses SQLite for the production database. Use SQLite's online backup
mechanism for backups; do not copy the live database file while the API may be
writing to it.

## Verified Runtime Paths

The current committed configuration uses these database paths:

| Runtime | Configuration | Runtime database file |
| --- | --- | --- |
| Local Development | `InventoryManagement.API/appsettings.Development.json` has `Data Source=../Data/inventory.db` | `C:\_Projects\DotNET_Projects\InventoryManagement\Data\inventory.db` when run from `InventoryManagement.API` |
| Docker Compose | `docker-compose.yml` has `ConnectionStrings__DefaultConnection=Data Source=Data/inventory.db` and `./Data:/app/Data` | `/app/Data/inventory.db` in the container, persisted as `C:\_Projects\DotNET_Projects\InventoryManagement\Data\inventory.db` on the host |

Production deployments may override `ConnectionStrings__DefaultConnection`.
Confirm the production value before running backup or restore commands.

## Backup

Run backups from the host where the database volume is mounted. Store backups
outside the live `Data` directory.

PowerShell:

```powershell
$DbPath = "C:\_Projects\DotNET_Projects\InventoryManagement\Data\inventory.db"
$BackupDir = "C:\_Projects\DotNET_Projects\InventoryManagement\Backups"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupPath = Join-Path $BackupDir "inventory-$Timestamp.db"

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
sqlite3 $DbPath ".backup '$BackupPath'"
sqlite3 $BackupPath "PRAGMA integrity_check;"
```

The integrity check must return:

```text
ok
```

Docker Compose host example:

```powershell
$DbPath = ".\Data\inventory.db"
$BackupDir = ".\Backups"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupPath = Join-Path $BackupDir "inventory-$Timestamp.db"

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
sqlite3 $DbPath ".backup '$BackupPath'"
sqlite3 $BackupPath "PRAGMA integrity_check;"
```

## Retention

Keep enough backups to cover operational mistakes and delayed detection of
data issues. A conservative starting policy is:

- Daily backups for 14 days.
- Weekly backups for 8 weeks.
- Monthly backups for 12 months.

Store a copy outside the application host or Docker volume. Periodically test a
restore into a non-production database.

## Restore

Restores must be deliberate and must not overwrite the live database while the
API is running.

1. Stop the API.

   ```powershell
   docker compose stop inventory-api
   ```

2. Verify the backup before using it.

   ```powershell
   $BackupPath = ".\Backups\inventory-YYYYMMDD-HHMMSS.db"
   sqlite3 $BackupPath "PRAGMA integrity_check;"
   ```

   Continue only if the result is `ok`.

3. Restore into a temporary database file using SQLite's restore command.

   ```powershell
   $RestorePath = ".\Backups\restore-candidate.db"
   sqlite3 $RestorePath ".restore '$BackupPath'"
   sqlite3 $RestorePath "PRAGMA integrity_check;"
   ```

   Continue only if the result is `ok`.

4. Explicit operator step: replace the live database with the verified restore
   candidate.

   ```powershell
   $DbPath = ".\Data\inventory.db"
   Move-Item -LiteralPath $DbPath -Destination ".\Backups\inventory-before-restore.db"
   Move-Item -LiteralPath $RestorePath -Destination $DbPath
   ```

5. Start the API.

   ```powershell
   docker compose up -d inventory-api
   ```

6. Check readiness.

   ```powershell
   Invoke-WebRequest http://localhost:8080/health/ready
   ```

