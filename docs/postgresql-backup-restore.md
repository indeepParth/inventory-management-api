# PostgreSQL Backup and Restore

This API uses PostgreSQL for local, test, Docker, and production persistence.
Use PostgreSQL-native tools for backups and restores.

## Runtime Targets

| Runtime | Connection |
| --- | --- |
| Local development | `Host=localhost;Port=5432;Database=inventorydatabase_dev;Username=postgres` |
| Local Docker Compose | API uses `Host=postgres;Port=5432`; host access is `localhost:5433` |
| Tests | `INVENTORY_TEST_POSTGRES_ADMIN_CONNECTION`, defaulting to local `postgres` admin database |
| Production Docker Compose | API uses `Host=postgres;Port=5432` with `POSTGRES_DB`, `POSTGRES_USER`, and `POSTGRES_PASSWORD` |

Confirm the active connection string before running backup or restore commands.

## Backup

Back up from a machine that can reach the PostgreSQL server. Store backups
outside Docker volumes and outside the application directory when possible.

Local PostgreSQL 18 on Windows:

```powershell
$env:PGPASSWORD = "admin123"
$BackupDir = ".\Backups"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupPath = Join-Path $BackupDir "inventorydatabase_dev-$Timestamp.dump"

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
& "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe" `
  -h localhost `
  -p 5432 `
  -U postgres `
  -d inventorydatabase_dev `
  -Fc `
  -f $BackupPath
```

Docker Compose PostgreSQL:

```powershell
$BackupDir = ".\Backups"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupPath = Join-Path $BackupDir "inventory-$Timestamp.dump"

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
docker compose exec -T postgres pg_dump `
  -U $env:POSTGRES_USER `
  -d $env:POSTGRES_DB `
  -Fc > $BackupPath
```

## Verify A Backup

List the dump contents:

```powershell
& "C:\Program Files\PostgreSQL\18\bin\pg_restore.exe" -l $BackupPath | Select-Object -First 20
```

For higher confidence, restore into a temporary database and run application
smoke checks against it.

## Retention

A conservative starting policy:

- Daily backups for 14 days.
- Weekly backups for 8 weeks.
- Monthly backups for 12 months.

Keep at least one copy outside the application host. Periodically test restore
into a non-production database.

## Restore

Restores are destructive when targeting an existing database. Stop the API
before replacing production data.

1. Stop the API.

   ```powershell
   docker compose stop inventory-api
   ```

2. Create a restore candidate database.

   ```powershell
   $env:PGPASSWORD = "admin123"
   & "C:\Program Files\PostgreSQL\18\bin\createdb.exe" `
     -h localhost `
     -p 5432 `
     -U postgres `
     inventory_restore_candidate
   ```

3. Restore into the candidate.

   ```powershell
   & "C:\Program Files\PostgreSQL\18\bin\pg_restore.exe" `
     -h localhost `
     -p 5432 `
     -U postgres `
     -d inventory_restore_candidate `
     --clean `
     --if-exists `
     $BackupPath
   ```

4. Point a non-production app instance at the candidate database and verify core
   workflows.

5. For production replacement, take a fresh backup of the current database,
   terminate active sessions, drop/recreate the target database, and restore the
   verified dump.

6. Start the API.

   ```powershell
   docker compose up -d inventory-api
   ```

7. Check readiness.

   ```powershell
   Invoke-WebRequest http://localhost:8080/health/ready
   ```
