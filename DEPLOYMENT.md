# Deployment

Local:

- Docker Compose.
- SQL Server.
- Mailpit.
- API.
- Frontend.

Production notes:

- Use HTTPS/TLS termination.
- Configure reverse proxy WebSocket support for /hubs.
- Use managed SQL Server or hardened self-hosted SQL Server.
- Store secrets outside Git.
- Enable health checks.
- Configure backups and restore tests.
- Evaluate SignalR backplane only if multiple API instances are required.

### Idempotency retention worker

The API removes expired idempotency keys at startup and once per polling interval. The worker deletes at most one ordered batch per run, using `ExpiresAt` as the cutoff. Configure `IdempotencyCleanup:PollingIntervalSeconds` and `IdempotencyCleanup:BatchSize` for the expected request volume. This retention applies only to idempotency response records; it is not a clinical-record or audit-log retention policy.

## SQL Server backup and restore verification

The Compose SQL Server stores its database files in the `sql-data` named volume. A Docker volume is not an off-host backup. Create a native `.bak` with SQL Server, copy it out of the container, and store it encrypted somewhere separate from the machine running Compose.

Example from a Bash shell after loading `SA_PASSWORD` from the local, untracked `.env` file:

```bash
set -a
. ./.env
set +a
mkdir -p backups
backup_name="MedicalAppointments_$(date -u +%Y%m%dT%H%M%SZ).bak"
docker exec -e SQLCMDPASSWORD="$SA_PASSWORD" medical-sql-server \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C \
  -Q "BACKUP DATABASE [MedicalAppointments] TO DISK = N'/var/opt/mssql/data/$backup_name' WITH COPY_ONLY, CHECKSUM, INIT, STATS = 10"
docker cp "medical-sql-server:/var/opt/mssql/data/$backup_name" "backups/$backup_name"
```

Keep backup files out of source control and restrict access because they contain personal and clinical data. The example keeps a local copy only; configure encrypted off-host storage and retention to meet the applicable legal and operational requirements. No automatic deletion policy is configured until that retention period is selected.

At least once before relying on a backup, copy it to an isolated SQL Server instance and:

1. Run `RESTORE VERIFYONLY ... WITH CHECKSUM` against the copied file.
2. Run `RESTORE FILELISTONLY` and use its returned logical file names to restore under a separate test database name and separate data/log file paths.
3. Connect to the restored database and verify migrations, representative records, and application reads.
4. Record the backup timestamp, restore duration, verification result, and any data-loss window. A successful backup command alone does not prove recoverability.

Never test by restoring over the active database. Document the chosen retention period and restore-test cadence with the deployment owner before enabling any automated backup purge.
