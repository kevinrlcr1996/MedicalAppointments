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