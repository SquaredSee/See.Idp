# Configuration Reference

All runtime configuration is supplied via environment variables. The app reads them through
the standard ASP.NET Core configuration hierarchy
(`appsettings.json` → `appsettings.{Environment}.json` → environment variables).

Environment variables use the `__` double-underscore separator for nested keys
(e.g. `ConnectionStrings__DefaultConnection`).

---

## Required in production

| Variable | Example | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=db;Port=5432;Database=seeidp;Username=seeidp;Password=…` | PostgreSQL connection string |
| `OpenIddict__SigningKey` | `<RSAKeyValue>…</RSAKeyValue>` | RSA-2048 signing key in XML format (see below) |
| `OpenIddict__EncryptionKey` | `<RSAKeyValue>…</RSAKeyValue>` | RSA-2048 encryption key in XML format (see below) |
| `Email__ApiKey` | `re_…` | [Resend](https://resend.com) API key |
| `Email__FromAddress` | `noreply@example.com` | Sender address for outbound email |

## Optional

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__Redis` | *(empty — in-memory fallback)* | Redis connection string for Data Protection key persistence |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://localhost:4317` | OpenTelemetry OTLP collector endpoint |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment (`Development` disables signing key requirement and uses dev certs) |

---

## Generating RSA keys for production

Run once and store the output in a Kubernetes Secret or equivalent secrets manager.
**Never commit these values to source control.**

```bash
# Signing key
dotnet run --project tools/KeyGen -- signing   # or use the snippet below

# One-liner with openssl + PowerShell/bash:
openssl genrsa 2048 | openssl pkcs8 -topk8 -nocrypt -outform PEM
```

Alternatively, generate the XML-formatted keys programmatically:

```csharp
using System.Security.Cryptography;
using var rsa = RSA.Create(2048);
Console.WriteLine(rsa.ToXmlString(includePrivateParameters: true));
```

Run this snippet twice — once for the signing key, once for the encryption key — and set
each output as the corresponding environment variable.

---

## Local development (docker-compose)

`docker-compose.yml` runs the IDP in `Development` mode. No signing or encryption keys are
required — OpenIddict generates ephemeral development certificates automatically.

Seeding (initial users, roles, clients) is driven by `appsettings.Development.json` via the
`Initialization` section.

See [docker-compose.yml](../docker-compose.yml) for the full service definition.
