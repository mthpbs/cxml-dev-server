# Backup sidecar for the self-hosted stack: sqlite3 online backups of the cXML Dev Server database.
# Built by deploy/selfhost/docker-compose.yml (service "backup"); no need to build it by hand.
FROM alpine:3.22

# sqlite: the sqlite3 CLI (.backup uses the online-backup API, safe while the app writes in WAL mode)
# su-exec: drop from root to uid 1654 after fixing the ownership of the bind-mounted backup folder
# tzdata: lets TZ=Europe/Berlin etc. work for the log timestamps
RUN apk add --no-cache sqlite su-exec tzdata

COPY backup.sh /usr/local/bin/backup.sh
RUN chmod 0755 /usr/local/bin/backup.sh

# The script starts as root only to chown /backups, then runs as 1654:1654 - the uid/gid of the "app" user in the
# .NET image - because SQLite in WAL mode needs to open the -wal/-shm files that user created.
ENTRYPOINT ["/usr/local/bin/backup.sh"]
CMD ["loop"]
