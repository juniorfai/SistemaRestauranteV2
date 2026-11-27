#!/bin/sh
# Copia de seguridad completa de la base de datos a ./backups (en el servidor, junto a docker-compose.yml).
# Programarla a diario, por ejemplo con cron:  0 4 * * * cd /ruta/al/proyecto && ./scripts/backup.sh
# Conviene copiar después la carpeta backups/ fuera del servidor (otro disco, S3, etc.).
set -eu
cd "$(dirname "$0")/.."

DB_NAME="${DB_NAME:-RestauranteMaster}"
STAMP="$(date +%Y%m%d-%H%M%S)"
NAME="${DB_NAME}-${STAMP}.bak"
TMP="/var/opt/mssql/backup/${NAME}"

docker compose exec -T db sh -c "mkdir -p /var/opt/mssql/backup && /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C -b \
  -Q \"BACKUP DATABASE [${DB_NAME}] TO DISK = N'${TMP}' WITH INIT, CHECKSUM\""

mkdir -p backups
docker compose cp "db:${TMP}" "backups/${NAME}"
docker compose exec -T db rm -f "${TMP}"
echo "Copia creada: backups/${NAME}"

# Borra las copias de más de 14 días.
find backups -name "${DB_NAME}-*.bak" -mtime +14 -delete
