# API

Base route:

- /api/v1

Authentication:

- JWT bearer.

Phase 1 endpoints:

- POST /api/v1/dev/token
- POST /api/v1/foundation/realtime/test
- GET /health/live
- GET /health/ready

Conventions:

- ProblemDetails for errors.
- Pagination contract for collections.
- DTOs separate from domain entities.
- Idempotency-Key header planned for critical operations.