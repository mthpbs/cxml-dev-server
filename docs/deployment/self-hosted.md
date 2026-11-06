# Runbook: self-hosted Docker Compose

This runbook stands up the cXML Dev Server on a Linux host you control, with automatic HTTPS and scheduled
database backups. The stack lives in [`deploy/selfhost`](../../deploy/selfhost):

| Service | Image | Role |
|---|---|---|
| `app` | `ghcr.io/mthpbs/cxml-dev-server` (or built from this repository) | The cXML Dev Server. Listens on 8080 on a private network only. Data in the `cxml-data` volume. |
| `caddy` | `caddy:2` | Reverse proxy with automatic HTTPS (Let's Encrypt, or Caddy's own CA for a LAN). The only service with published ports: 80, 443 and 443/udp. Certificates in the `caddy_data` volume. |
| `backup` | built from `deploy/selfhost/backup` (Alpine with `sqlite3`) | Takes an online SQLite backup every `BACKUP_INTERVAL_HOURS` hours into `deploy/selfhost/backups`, checks it and prunes old ones. Runs as uid 1654 with **no network** (`network_mode: none`). |

The Compose project is named `cxml-dev-server`, so Docker names the volumes
`cxml-dev-server_cxml-data`, `cxml-dev-server_caddy_data` and `cxml-dev-server_caddy_config`, and the
network `cxml-dev-server_cxml`. Unlike the [Azure deployment](azure-container-apps.md), the database
**persists** across restarts and upgrades.

- [Host requirements](#host-requirements)
- [DNS and firewall](#dns-and-firewall)
- [Install Docker](#install-docker)
- [Get the files](#get-the-files)
- [Configure and start](#configure-and-start)
- [Verification](#verification)
- [LAN mode without a public domain](#lan-mode-without-a-public-domain)
- [Upgrade and rollback](#upgrade-and-rollback)
- [Backup and restore](#backup-and-restore)
- [Logs](#logs)
- [Certificate troubleshooting](#certificate-troubleshooting)
- [Other problems](#other-problems)
- [Teardown](#teardown)

All commands below run in `deploy/selfhost` (that is where Compose reads `.env` and the relative paths
`./Caddyfile` and `./backups`), unless stated otherwise.

## Host requirements

| Resource | Minimum | Comfortable |
|---|---|---|
| CPU | 1 vCPU | 2 vCPU |
| RAM | 1 GB | 2 GB (Blazor keeps one circuit per open browser tab in memory) |
| Disk | 10 GB | 20 GB (image about 125 MB compressed, a few hundred MB for the SQLite file in heavy use, plus 14 days of daily backups) |
| OS | Any Linux that runs Docker Engine on x86-64 or arm64 | Ubuntu LTS or Debian stable |
| Network | A public IPv4 address (and optionally IPv6) with inbound 80 and 443, for a public certificate | |

A small cloud VM, a Raspberry Pi 4 or an on-premises VM is enough. The application is a test tool: it handles
tens of requests per minute, not thousands.

## DNS and firewall

For a public certificate (the default, `TLS_MODE=acme`):

1. Pick a host name, for example `cxml.example.com`, and create an **A record** to the host's public IPv4
   address (and an **AAAA record** only if the host really is reachable over that IPv6 address; a wrong AAAA
   record is the most common cause of failed certificate issuance).
2. Open inbound **TCP 80** (certificate validation and the redirect to HTTPS), **TCP 443** (HTTPS) and
   **UDP 443** (HTTP/3, optional). Nothing else must be reachable: in particular never publish 8080.

   ```bash
   sudo ufw allow 80/tcp && sudo ufw allow 443/tcp && sudo ufw allow 443/udp
   ```

   (Use your own firewall or the cloud provider's security group if you do not use `ufw`.)
3. Check that the name resolves to the host:

   ```bash
   dig +short cxml.example.com
   ```

No public DNS name or no inbound internet access? Use [LAN mode](#lan-mode-without-a-public-domain).

## Install Docker

Install Docker Engine and the Compose plugin from Docker's repository, following the
[official instructions for your distribution](https://docs.docker.com/engine/install/). Then confirm that
both work (Compose v2.20 or later is recommended):

```bash
docker --version && docker compose version
```

Allow your user to run Docker without `sudo` (log out and in afterwards), or prefix every command with
`sudo`:

```bash
sudo usermod -aG docker "$USER"
```

Docker's default `json-file` log driver never rotates logs. Limit them in `/etc/docker/daemon.json`, then
restart Docker (`sudo systemctl restart docker`):

```json
{
  "log-driver": "json-file",
  "log-opts": { "max-size": "10m", "max-file": "3" }
}
```

## Get the files

Option A, clone the repository (needed if you want to build the image yourself, and the easiest way to
update the deployment files later):

```bash
git clone https://github.com/mthpbs/cxml-dev-server.git && cd cxml-dev-server/deploy/selfhost
```

Option B, copy only the `deploy/selfhost` folder (with `docker-compose.yml`, `Caddyfile`, `.env.example` and
`backup/`) to the host, for example into `/opt/cxml-dev-server`, and work there. This works as long as the
published image can be pulled; the `app` service's `build` fallback needs the whole repository
(`context: ../..`).

If the image on GHCR is private, sign in once so `docker compose` can pull it (use a personal access token
with `read:packages`):

```bash
docker login ghcr.io
```

## Configure and start

1. **Create `.env`** from the template and restrict it, because it holds the admin password hash:

   ```bash
   cp .env.example .env && chmod 600 .env
   ```

2. **Create the admin password hash** with the image you will run. `-it` lets the container prompt without
   echo:

   ```bash
   docker run --rm -it ghcr.io/mthpbs/cxml-dev-server:latest hash-password
   ```

   If the image is not published yet, build it first with `docker build -t cxml-dev-server ../..` and use
   `cxml-dev-server` instead of the registry name in this command and in `IMAGE`. Without Docker, run
   `dotnet run --project src/CxmlDevServer.Web --no-launch-profile -- hash-password` from a repository
   checkout. The command prints one long line (base64).

3. **Edit `.env`** (`nano .env`). Required values:

   | Variable | Example | Meaning |
   |---|---|---|
   | `DOMAIN` | `cxml.example.com` | Public host name. |
   | `ACME_EMAIL` | `ops@yourcompany.com` | Contact for the certificate authority. Required in every mode. Use a real address: Let's Encrypt rejects `example.com` addresses. |
   | `ADMIN_PASSWORD_HASH` | the hash from step 2 | Compose refuses to start while it is empty. |

   Optional values (the commented lines in `.env.example`):

   | Variable | Default | Meaning |
   |---|---|---|
   | `IMAGE` | `ghcr.io/mthpbs/cxml-dev-server:latest` | Image to run. **Pin a version tag** (`:v1.0.0`) for predictable upgrades and rollbacks. |
   | `ADMIN_USERNAME` | `admin` | Admin user name. |
   | `CXML_PUBLIC_BASE_URL` | `https://<DOMAIN>` | Externally visible URL (see [forwarded headers](configuration.md#forwarded-headers-and-publicbaseurl)). |
   | `CXML_ALLOW_UNKNOWN_PARTNERS` | `false` | `true`: process documents from unknown senders with a warning. |
   | `CXML_SEED_DEMO_PARTNERS` | `false` | `true`: create the demo partners with the well-known secrets `devsecret` and `abracadabra` in a **new** database. Leave it `false` on a shared host. |
   | `TLS_MODE` | `acme` | `acme` (public certificate) or `internal` (Caddy's own CA). |
   | `HTTP_PORT` / `HTTPS_PORT` | `80` / `443` | Host ports of Caddy; change only for testing. |
   | `BACKUP_INTERVAL_HOURS` / `BACKUP_RETENTION_DAYS` | `24` / `14` | Backup schedule and retention. |
   | `TZ` | `UTC` | Time zone of container logs (backup file names are always UTC). |

   A value containing a `$` must be wrapped in single quotes (`'...'`) or Compose treats it as a variable
   reference. The hash printed by `hash-password` is base64 and contains no `$`, so it can be pasted as it is.

4. **Check the result** before starting anything. It prints nothing on success and fails with a clear
   message when a required variable is missing (leave out `--quiet` to see the resolved configuration, but
   that output contains the hash, so do not paste it into a ticket):

   ```bash
   docker compose config --quiet && echo OK
   ```

5. **Start the stack.** The first start pulls `IMAGE` and `caddy:2` and builds the small backup image. If
   `IMAGE` cannot be pulled (not published, wrong name), Compose builds the application from the repository
   instead, which needs Option A above:

   ```bash
   docker compose up -d
   ```

   To always build from this repository (a fork, local changes) use:

   ```bash
   docker compose up -d --build
   ```

6. **Watch it come up.** `app` must become `healthy` (about 30 seconds); Caddy and the backup service start
   after that:

   ```bash
   docker compose ps
   ```

## Verification

Replace the host name with your `DOMAIN`.

1. **Containers.** All three services are `running`, and `app` is `healthy`:

   ```bash
   docker compose ps
   ```

2. **Health through Caddy** answers `OK` over HTTPS with a valid certificate:

   ```bash
   curl -fsS https://cxml.example.com/health
   ```

3. **cXML endpoint** answers `Status code="200"`:

   ```bash
   curl -fsS https://cxml.example.com/cxml
   ```

4. **HTTP is redirected** to HTTPS:

   ```bash
   curl -sI http://cxml.example.com/health | head -n 3
   ```

5. **The admin UI requires login**: expect `302` with `location: /login?returnUrl=%2F`:

   ```bash
   curl -sI https://cxml.example.com/ | head -n 3
   ```

6. **Sign in** in a browser at `https://cxml.example.com/` as `ADMIN_USERNAME` with the password behind your
   hash. The dashboard shows the cXML endpoint URL to give to buyers; it must start with `https://`.

7. **A backup works.** Take one now and look at it:

   ```bash
   docker compose run --rm backup once && ls -l backups
   ```

   The service also takes its first backup right after it starts, then every `BACKUP_INTERVAL_HOURS`.
   Files are named `cxml-dev-<UTC timestamp>.db` and are owned by uid/gid 1654 (the `app` user of the .NET
   image); use `sudo` if the host's permissions keep your user from reading or deleting them. If the `app`
   service is down, add `--no-deps` to the `docker compose run` command to skip starting it.

8. Continue with the [new-environment checklist](configuration.md#new-environment-checklist) to create the
   real trading partners.

## LAN mode without a public domain

For a host without a public DNS name or without inbound internet access (an office LAN, an air-gapped
network, a laptop), let Caddy issue certificates from its own local certificate authority:

```ini
DOMAIN=cxml.lan
ACME_EMAIL=ops@yourcompany.com
TLS_MODE=internal
ADMIN_PASSWORD_HASH=<your-hash>
```

- `DOMAIN` must resolve to the host for every client: a DNS entry in your network, or a line in each
  client's hosts file. `localhost` works for a test on the same machine.
- If you change `HTTP_PORT`/`HTTPS_PORT`, also set `CXML_PUBLIC_BASE_URL` to the full URL with the port,
  for example `https://cxml.lan:8443`.
- Browsers and buyer systems do not trust Caddy's CA, so they show certificate warnings or reject the
  connection until they trust its **root certificate**. Export it once the stack is running:

  ```bash
  docker compose exec caddy cat /data/caddy/pki/authorities/local/root.crt > caddy-root.crt
  ```

  Install `caddy-root.crt` as a trusted root on every client:

  | Client | Command or step |
  |---|---|
  | Debian or Ubuntu | `sudo cp caddy-root.crt /usr/local/share/ca-certificates/caddy-root.crt && sudo update-ca-certificates` |
  | macOS | `sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain caddy-root.crt` |
  | Windows (administrator prompt) | `certutil -addstore -f "ROOT" caddy-root.crt` |
  | Java based buyer systems | `keytool -importcert -alias caddy-local -file caddy-root.crt -keystore <truststore> -storepass <password>` |
  | One-off test with curl | `curl --cacert caddy-root.crt https://cxml.lan/health` (or `-k` to skip verification) |

- The Caddyfile sends an HSTS header. If the CA is not trusted on every client, remove the
  `Strict-Transport-Security` line from `Caddyfile` for a LAN deployment.
- Keep the `caddy_data` volume: it holds the CA's key. If it is deleted, a new root certificate is
  generated and all clients must trust it again.

Apply a change of `.env` or the Caddyfile with `docker compose up -d` (Compose recreates the services whose
configuration changed). For a Caddyfile edit alone:

```bash
docker compose restart caddy
```

## Upgrade and rollback

### Upgrade

Releases are published as image tags. Before upgrading, take a backup (database migrations run
automatically at start-up and only go forward):

```bash
docker compose run --rm backup once
```

If you cloned the repository, bring the deployment files up to date. Review changes to `docker-compose.yml`,
`Caddyfile` and `.env.example` and add new variables to your `.env`:

```bash
git pull
```

Set `IMAGE` in `.env` to the new tag (or leave `latest`), then pull the images and recreate the containers.
Name the services: a bare `docker compose pull` fails because the `backup` image is built locally and
cannot be pulled.

```bash
docker compose pull app caddy && docker compose up -d
```

If the `backup/` folder changed, rebuild its image:

```bash
docker compose build backup && docker compose up -d
```

Confirm with `docker compose ps` and the [verification](#verification) steps. Open sessions in the browser
reconnect after the short restart (the login cookie survives because the Data Protection keys are stored in
the data volume).

Remove old images from time to time:

```bash
docker image prune -f
```

### Rollback

Pin the previous tag in `.env`:

```ini
IMAGE=ghcr.io/mthpbs/cxml-dev-server:1.0.0
```

```bash
docker compose up -d
```

Database migrations are not reversible. If the newer version already migrated the database, the older
version may not start on it. In that case restore the backup that you took **before** the upgrade (see
below) together with the older image. Restoring a backup made by an *older* version under a *newer* image
normally works: the migrations are applied at start-up.

## Backup and restore

### What is backed up

The `backup` service copies `/app/data/cxml-dev.db` with SQLite's online backup API (`sqlite3 .backup`),
which is consistent even while the application writes (the database runs in WAL mode). Each copy is
checked with `PRAGMA integrity_check` before it gets its final name; copies older than
`BACKUP_RETENTION_DAYS` are removed, but the newest copy is always kept. The schedule is
`BACKUP_INTERVAL_HOURS` (default every 24 hours, and once when the service starts).

Not part of the backup: the Data Protection keys (`/app/data/keys`; losing them only signs people out),
the `.env` file (keep your own copy in a safe place, it holds the admin password hash) and the Caddy
certificates (`caddy_data`; they are re-issued, but beware of Let's Encrypt rate limits).

### Check the backups

```bash
docker compose logs --tail 20 backup
```

```bash
sudo ls -l backups
```

### Offsite copy

The backups sit on the same host as the data. Copy them elsewhere regularly, for example with a daily
`cron` job on the host (as root, because the files belong to uid 1654):

```bash
rsync -a /opt/cxml-dev-server/deploy/selfhost/backups/ backup-server:/srv/backups/cxml-dev-server/
```

Any tool works (rclone to object storage, restic, your existing backup agent); just include the `backups`
folder.

### Restore

This replaces the current database with a backup. Everything configured or received after the backup is
lost. Choose the file first:

```bash
sudo ls -l backups
```

1. **Stop the application and the backup service** (Caddy can keep running; it answers `502` meanwhile):

   ```bash
   docker compose stop app backup
   ```

2. **Optionally keep the current state** as a safety copy before overwriting it:

   ```bash
   docker run --rm -v cxml-dev-server_cxml-data:/data -v "$PWD/backups":/backups alpine:3.22 cp /data/cxml-dev.db /backups/before-restore.db
   ```

   The name `before-restore.db` does not match `cxml-dev-*.db`, so retention never deletes it.

3. **Copy the chosen snapshot into the volume, remove the old journal files and fix the ownership.** Replace
   the file name. The `-wal` and `-shm` files belong to the old database; if they stay, SQLite would apply
   them on top of the restored file and corrupt it. The application runs as uid/gid 1654 and must own the
   file.

   ```bash
   docker run --rm -v cxml-dev-server_cxml-data:/data -v "$PWD/backups":/backups alpine:3.22 sh -c 'cp /backups/cxml-dev-20261105T020000Z.db /data/cxml-dev.db && rm -f /data/cxml-dev.db-wal /data/cxml-dev.db-shm && chown 1654:1654 /data/cxml-dev.db && ls -l /data'
   ```

4. **Start everything again** and verify:

   ```bash
   docker compose up -d
   ```

   Then run the [verification](#verification) steps, and check in the admin UI that the trading partners and
   messages you expect are present.

If the stack is gone (new host, deleted volumes), install it again as in [Configure and start](#configure-and-start),
but run `docker compose create` instead of `up -d`, restore with the helper container above (Compose
creates the volume `cxml-dev-server_cxml-data` on `create`), then `docker compose up -d`:

```bash
docker compose create
```

## Logs

Follow the application and the proxy:

```bash
docker compose logs -f app caddy
```

Only the last hour, or one service:

```bash
docker compose logs --since 1h app
```

- `app`: application log (startup, seeding, errors). The cXML traffic itself is in the **Message log** of
  the admin UI, which survives restarts here.
- `caddy`: access log in console format and certificate events.
- `backup`: one line per backup (`ok: /backups/cxml-dev-...db`) or the failure reason.

Raise or lower verbosity by adding `Logging__LogLevel__Default` to the `environment` of the `app` service
in `docker-compose.yml` (see [configuration](configuration.md#logging)).

## Certificate troubleshooting

Check what Caddy says:

```bash
docker compose logs caddy | grep -i -E "error|obtain|certificate|acme"
```

| Symptom | Cause and fix |
|---|---|
| Browser shows a certificate error right after the first start | Issuance takes up to a minute; look at the Caddy log. |
| `no valid A records found`, `timeout during connect`, `Timeout during connect (likely firewall problem)` | The challenge cannot reach the host. Check DNS (`dig +short $DOMAIN` must return this host's public address), the AAAA record, the firewall and the provider's security group, and port forwarding when the host is behind NAT. Ports 80 and 443 must reach Caddy. |
| `too many certificates` / `rate limited` | Let's Encrypt limits repeated issuance for the same name. Wait; never delete the `caddy_data` volume casually. Use `TLS_MODE=internal` to keep working meanwhile. |
| `invalid contact domain` / `invalid email` | `ACME_EMAIL` is not a deliverable address (for example `@example.com`). Set a real one and `docker compose up -d`. |
| Another service already uses 80 or 443 | `bind: address already in use`. Stop it, or put a different proxy in front and run Caddy on other ports with `HTTP_PORT`/`HTTPS_PORT` (then public certificates through Caddy no longer work; use `TLS_MODE=internal` behind your own TLS proxy, or terminate TLS in that proxy and forward to Caddy). |
| Works with `curl -k` but not in the buyer system | `TLS_MODE=internal` without trusting the root CA: see [LAN mode](#lan-mode-without-a-public-domain). |
| Needs a clean slate | `docker compose down`, then remove only the Caddy volumes (`docker volume rm cxml-dev-server_caddy_data cxml-dev-server_caddy_config`) and `docker compose up -d`. This deletes the certificates, not your data. |

## Other problems

| Symptom | Fix |
|---|---|
| `docker compose` complains `Set ADMIN_PASSWORD_HASH in .env`, `Set DOMAIN in .env` or `Set ACME_EMAIL in .env` | The variable is empty or missing. Compose reads `.env` from the folder of `docker-compose.yml`. |
| `app` is `unhealthy` or restarts | `docker compose logs app`. A hash that is damaged or not made by `hash-password` stops the app at startup with `Admin:PasswordHash is not a valid ASP.NET Core Identity password hash`. |
| `StartPage` URLs wrong | See [forwarded headers and PublicBaseUrl](configuration.md#forwarded-headers-and-publicbaseurl). Behind an additional proxy or a non-standard port set `CXML_PUBLIC_BASE_URL`. |
| `401` from `/cxml` for a buyer | No matching trading partner or a wrong shared secret; open the **Message log** to see the reason. |
| Login blocked with `429` | More than 5 attempts per minute from one address. Wait a minute. |
| Backup service exits or logs `FAILED` | `docker compose logs backup`. `database ... does not exist (yet)` means the app has not created it; a failed integrity check keeps no file. Run `docker compose run --rm backup once` to see the message. |
| Cannot delete or change files in `backups/` on the host | The service chowns the folder to 1654:1654 at start and creates the files as that user; do not chown it back, use `sudo` for housekeeping. |

## Teardown

Stop and remove the containers, keeping the data and certificates (volumes) and the backups:

```bash
docker compose down
```

Remove **everything including the database and certificates**:

```bash
docker compose down --volumes
```

Delete the local backups (and the safety copy) once you no longer need them:

```bash
sudo rm -rf backups
```

Remove the images if you want the disk space back:

```bash
docker image rm ghcr.io/mthpbs/cxml-dev-server:latest cxml-dev-server-backup:local caddy:2
```
