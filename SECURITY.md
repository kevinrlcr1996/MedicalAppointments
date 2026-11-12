# Security

Implemented controls:

- JWT bearer validation checks issuer, audience, signature, and lifetime. Hub access tokens are read from the query string only on `/hubs` routes.
- Refresh tokens are opaque, stored by hash, rotated on use, revocable, and protected by sliding and absolute lifetimes. Reuse triggers revocation handling.
- Passwords are hashed with PBKDF2; login failures use account lockout.
- Authorization policies and permission claims are used at API and application/service boundaries, including resource-based appointment and clinical-record checks.
- Authentication endpoints have per-IP rate limits; CORS uses configured origins with credentials.
- Security headers include `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `Permissions-Policy`. HSTS and HTTPS redirection run outside Development.
- API errors use sanitized ProblemDetails and correlation IDs. Audit entries are used for sensitive operations and avoid storing clinical note content.
- `.env`, local settings, backups, build output, and uploads are excluded from version control and the API Docker build context.
- CI checks .NET and production npm dependencies for known vulnerabilities, runs backend tests and frontend lint/typecheck/build, and builds the API image. Findings should be reviewed before release; CI does not deploy.

Operational controls still required for any deployment:

- Put signing keys, database credentials, SMTP credentials, and storage credentials in a secret manager or protected deployment environment.
- Terminate HTTPS correctly and configure trusted forwarded headers only for known reverse proxies.
- Apply a jurisdiction-appropriate data retention schedule, encrypted backups, and regularly tested restores; see `DEPLOYMENT.md`.
- Keep the ClamAV scanner enabled and reachable for uploaded clinical attachments. Scanner unavailability fails closed.
- Run dependency/container scans and review findings; this project does not yet publish a software bill of materials or attest image provenance.
- Do not use real patient information in development or test environments.
