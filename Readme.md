# WebhookHub

WebhookHub is a learning-focused webhook platform built with ASP.NET Core. It consists of two services that together demonstrate a complete webhook publish/deliver/receive pipeline.

| Service | Responsibility |
|---|---|
| **WebhookHub** | Publishes events, manages subscriptions, signs webhook deliveries, retries failures, and tracks delivery history. |
| **WebhookReceiver** | Receives signed webhook calls, verifies HMAC signatures, validates payloads, and handles idempotency. |

## Architecture

```
Client
  -> WebhookHub API
       -> Create subscription
       -> Publish event
       -> Queue delivery
       -> Background worker sends webhook
            -> WebhookReceiver
                 -> Verify signature
                 -> Check idempotency
                 -> Dispatch to handler
```

## Features

### WebhookHub
- API key authentication
- Subscription lifecycle management
- Event publishing
- HMAC SHA-256 webhook signatures
- Background delivery worker
- Retry policy with exponential backoff
- Delivery attempt history
- Dead-letter handling
- Dead-letter replay
- SQLite persistence
- EF Core migrations
- Serilog logging
- Correlation IDs
- Health checks
- Rate limiting
- Scalar/OpenAPI documentation
- Docker Compose support

### WebhookReceiver
- Webhook signature verification
- SQLite-backed idempotency
- Strongly typed payload handlers
- Strategy pattern event dispatching
- Validation
- Serilog logging
- Correlation IDs
- Health checks
- Scalar/OpenAPI documentation

## Technology Stack

- ASP.NET Core
- Entity Framework Core
- SQLite
- Serilog
- Scalar
- Docker Compose
- xUnit

## Project Structure

```
src/
├── Hub/
│   └── WebhookHub/
├── Receiver/
│   └── WebhookReceiver/
├── tests/
│   ├── WebhookHub.Tests/
│   └── WebhookReceiver.Tests/
├── docker-compose.yml
└── README.md
```

## Running Locally

### Run WebhookReceiver

```bash
cd src/Receiver/WebhookReceiver
dotnet restore
dotnet ef database update
dotnet run
```

Open Scalar: `http://localhost:<receiver-port>/scalar/v1`

### Run WebhookHub

```bash
cd src/Hub/WebhookHub
dotnet restore
dotnet ef database update
dotnet run
```

Open Scalar: `http://localhost:<hub-port>/scalar/v1`

## Configuration

### WebhookHub — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=webhookhub.db"
  },
  "ApiKey": {
    "HeaderName": "X-Api-Key",
    "Key": "dev-api-key-123"
  }
}
```

### WebhookReceiver — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=webhookreceiver.db"
  },
  "WebhookReceiver": {
    "EventSecrets": {
      "order.created": "super-secret-value",
      "order.cancelled": "cancel-secret-value"
    }
  }
}
```

## Database Migrations

### WebhookHub

```bash
cd src/Hub/WebhookHub
dotnet ef database update
```

### WebhookReceiver

```bash
cd src/Receiver/WebhookReceiver
dotnet ef database update
```

## Running with Docker Compose

From the solution root:

```bash
docker compose up --build
```

**Services:**
- WebhookHub: `http://localhost:5001`
- WebhookReceiver: `http://localhost:5002`

**Scalar:**
- WebhookHub: `http://localhost:5001/scalar/v1`
- WebhookReceiver: `http://localhost:5002/scalar/v1`

**Health checks:**
- WebhookHub: `http://localhost:5001/health/ready`
- WebhookReceiver: `http://localhost:5002/health/ready`

### Docker Callback URL

Inside Docker, use the service name instead of `localhost`.

Use this callback URL when creating subscriptions:

```
http://webhook-receiver:8080/webhooks/orders
```

Do **not** use:

```
http://localhost:5002/webhooks/orders
```

> Inside a container, `localhost` refers to the current container, not the host or sibling containers.

## Testing the Webhook Flow

### 1. Create Subscription

**Docker:**

```bash
curl -X POST http://localhost:5001/api/subscriptions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-api-key-123" \
  -d '{
    "eventType": "order.created",
    "callbackUrl": "http://webhook-receiver:8080/webhooks/orders",
    "secret": "super-secret-value"
  }'
```

**Local development** (request body):

```json
{
  "eventType": "order.created",
  "callbackUrl": "http://localhost:<receiver-port>/webhooks/orders",
  "secret": "super-secret-value"
}
```

### 2. Publish Event

```bash
curl -X POST http://localhost:5001/api/events \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-api-key-123" \
  -d '{
    "eventType": "order.created",
    "payload": {
      "orderId": "ORD-9001",
      "customerId": "CUST-DOCKER",
      "total": 199.99
    }
  }'
```

### 3. View Deliveries

```bash
curl http://localhost:5001/api/deliveries \
  -H "X-Api-Key: dev-api-key-123"
```

### 4. View Dead-Letter Deliveries

```bash
curl http://localhost:5001/api/deliveries/dead-letter \
  -H "X-Api-Key: dev-api-key-123"
```

### 5. Replay Dead-Letter Delivery

```bash
curl -X POST http://localhost:5001/api/deliveries/{deliveryId}/replay \
  -H "X-Api-Key: dev-api-key-123"
```

## Subscription Lifecycle

### List Subscriptions

```bash
curl http://localhost:5001/api/subscriptions \
  -H "X-Api-Key: dev-api-key-123"
```

### Get Subscription

```bash
curl http://localhost:5001/api/subscriptions/{subscriptionId} \
  -H "X-Api-Key: dev-api-key-123"
```

### Update Subscription

```bash
curl -X PUT http://localhost:5001/api/subscriptions/{subscriptionId} \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-api-key-123" \
  -d '{
    "eventType": "order.created",
    "callbackUrl": "http://webhook-receiver:8080/webhooks/orders"
  }'
```

### Pause Subscription

```bash
curl -X POST http://localhost:5001/api/subscriptions/{subscriptionId}/pause \
  -H "X-Api-Key: dev-api-key-123"
```

### Resume Subscription

```bash
curl -X POST http://localhost:5001/api/subscriptions/{subscriptionId}/resume \
  -H "X-Api-Key: dev-api-key-123"
```

### Rotate Secret

```bash
curl -X POST http://localhost:5001/api/subscriptions/{subscriptionId}/rotate-secret \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-api-key-123" \
  -d '{
    "newSecret": "new-super-secret-value"
  }'
```

> After rotating the subscription secret, update the receiver configuration with the matching secret.

### Delete Subscription

This operation deactivates the subscription.

```bash
curl -X DELETE http://localhost:5001/api/subscriptions/{subscriptionId} \
  -H "X-Api-Key: dev-api-key-123"
```

## Webhook Headers

WebhookHub sends the following headers with each delivery:

| Header | Description |
|---|---|
| `X-Webhook-Event` | The event type being delivered |
| `X-Webhook-Delivery-Id` | Unique ID for this delivery attempt |
| `X-Webhook-Signature` | HMAC SHA-256 signature of the payload |
| `X-Correlation-Id` | Correlation ID for tracing across services |

## Signature Verification

WebhookHub signs the raw JSON payload using **HMAC SHA-256**. WebhookReceiver verifies the signature using the configured event secret.

## Idempotency

WebhookReceiver stores processed delivery IDs in SQLite. If the same `X-Webhook-Delivery-Id` is received more than once, duplicate deliveries are ignored.

## Running Tests

```bash
dotnet test
```

## Learning Objectives

This project demonstrates:

- Webhook architecture
- Background processing
- HMAC signing
- Idempotency
- Retry strategies
- Dead-letter queues
- API security
- Rate limiting
- Health checks
- OpenAPI documentation
- Integration testing
- Containerization
- SOLID principles
- Repository pattern
- Strategy pattern
- Dependency Injection

## Future Enhancements

- Multi-tenant API keys
- Webhook verification handshake
- Delivery dashboards
- Metrics and tracing
- Outbox pattern
- Message broker integration
- Secret versioning
- Subscription filtering
- Webhook batching
- PostgreSQL support
- Kubernetes deployment