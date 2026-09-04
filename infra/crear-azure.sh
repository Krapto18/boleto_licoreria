#!/usr/bin/env bash
# ══════════════════════════════════════════════════════════════
# Infraestructura mínima de Boleto Licorería.
# Costo verificado en el calculador (East US 2):
#   App Service B1 $12.41 + Azure SQL Basic $4.90 + Storage (centavos)
# Los precios cambian: vuelve a verificar antes de correrlo.
# ══════════════════════════════════════════════════════════════
set -euo pipefail

RG="rg-boleto"
LOC="eastus2"                 # más barato que Brazil South; ~70 ms a Lima
PLAN="plan-boleto"
APP="boleto-licoreria"        # debe ser único en azurewebsites.net
SQLSRV="sql-boleto-$RANDOM"
SQLDB="boleto"
SQLUSER="boleto_admin"
STORAGE="stboleto$RANDOM"     # solo minúsculas y números, único global

read -rsp "Contraseña para el admin de SQL: " SQLPASS; echo
read -rp  "Correo del dueño (login del panel): " ADMIN_EMAIL
read -rsp "Contraseña del panel (mínimo 12): " ADMIN_PASS; echo

az group create -n "$RG" -l "$LOC" -o none

# B1 es el piso real. F1 no tiene Always On: la app se dormiría de madrugada,
# justo cuando el 24/7 es todo el argumento del negocio.
az appservice plan create -g "$RG" -n "$PLAN" --sku B1 --is-linux -o none
az webapp create -g "$RG" -p "$PLAN" -n "$APP" --runtime "DOTNETCORE:10.0" -o none

az sql server create -g "$RG" -n "$SQLSRV" -l "$LOC" \
  --admin-user "$SQLUSER" --admin-password "$SQLPASS" -o none

# Basic = 5 DTU, 2 GB. Alcanza de sobra: la web pública sirve desde caché en
# memoria y solo el panel escribe.
# Basic = 5 DTU, 2 GB, $4.90/mes. Sin Long Term Retention: el
# Point-in-Time Restore ya viene incluido sin costo extra.
az sql db create -g "$RG" -s "$SQLSRV" -n "$SQLDB" \
  --service-objective Basic --backup-storage-redundancy Local -o none

# Solo servicios de Azure, no todo internet.
az sql server firewall-rule create -g "$RG" -s "$SQLSRV" \
  -n AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none

# ── Storage para las fotos ───────────────────────────────────────
# Sin esto, la app cae al almacén en disco y cada despliegue borra las
# fotos que subió el dueño. Program.cs se niega a arrancar en Producción
# si esta cadena no está, así que el olvido no pasa desapercibido.
#
# Acceso público de solo lectura a nivel blob: son fotos de catálogo que
# ya se sirven en la web abierta. Escribir sigue exigiendo la clave.
az storage account create -g "$RG" -n "$STORAGE" -l "$LOC" \
  --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 \
  --allow-blob-public-access true -o none

BLOBCS=$(az storage account show-connection-string \
  -g "$RG" -n "$STORAGE" --query connectionString -o tsv)

CS="Server=tcp:${SQLSRV}.database.windows.net,1433;Database=${SQLDB};User ID=${SQLUSER};Password=${SQLPASS};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set -g "$RG" -n "$APP" \
  --connection-string-type SQLAzure --settings Sql="$CS" -o none

# Tipo Custom: Blob no es una de las categorías con nombre de App Service.
az webapp config connection-string set -g "$RG" -n "$APP" \
  --connection-string-type Custom --settings Blob="$BLOBCS" -o none

az webapp config appsettings set -g "$RG" -n "$APP" --settings \
  Admin__Email="$ADMIN_EMAIL" \
  Admin__Password="$ADMIN_PASS" \
  ASPNETCORE_ENVIRONMENT=Production -o none

# Always On: sin esto App Service descarga la app tras 20 min sin tráfico y el
# primer visitante de la madrugada se come el arranque en frío.
az webapp config set -g "$RG" -n "$APP" \
  --always-on true --http20-enabled true --min-tls-version 1.2 \
  --health-check-path "/health" -o none

az webapp update -g "$RG" -n "$APP" --https-only true -o none

echo
echo "Listo:  https://${APP}.azurewebsites.net"
echo "Panel:  https://${APP}.azurewebsites.net/panel"
echo "Fotos:  cuenta de storage ${STORAGE}, contenedor 'media'"
echo
echo "Pendiente:"
echo "  1. Borrar Admin__Password de App Settings tras el primer arranque."
echo "  2. Evaluar Access Restrictions por IP para /panel."
