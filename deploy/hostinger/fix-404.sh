#!/usr/bin/env bash
# Run on Hostinger VPS (hPanel -> VPS -> Browser terminal OR SSH as root)
# Fixes nginx 404 by installing .NET, deploying app folder, nginx proxy, systemd service.

set -euo pipefail

APP_DIR="/var/www/sahulatghartak"
SERVICE_NAME="sahulatghartak"
APP_PORT=5000
DOMAIN="sahulatghartak.com"

echo "========== SahulatGharTak fix script =========="

echo "[1/7] Installing packages..."
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq

if ! command -v dotnet >/dev/null 2>&1; then
  wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update -qq
  apt-get install -y aspnetcore-runtime-8.0
fi

apt-get install -y nginx unzip curl

echo "[2/7] Preparing app directory..."
mkdir -p "$APP_DIR/wwwroot/uploads/providers"
mkdir -p "$APP_DIR/wwwroot/uploads/documents"

if [ ! -f "$APP_DIR/HomeServicesPortal.dll" ]; then
  echo ""
  echo "WARNING: $APP_DIR/HomeServicesPortal.dll NOT FOUND."
  echo "Upload your publish folder from Windows first:"
  echo "  scp -r publish/* USER@93.127.199.220:$APP_DIR/"
  echo "Or zip publish on PC, upload to server, then:"
  echo "  unzip -o sahulatghartak.zip -d $APP_DIR"
  echo ""
fi

if [ -f "$APP_DIR/appsettings.json" ]; then
  echo "appsettings.json found."
else
  echo "WARNING: appsettings.json missing in $APP_DIR"
fi

chown -R www-data:www-data "$APP_DIR" 2>/dev/null || true

echo "[3/7] Creating systemd service..."
cat >/etc/systemd/system/${SERVICE_NAME}.service <<EOF
[Unit]
Description=SahulatGharTak Home Services Portal
After=network.target

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=/usr/bin/dotnet ${APP_DIR}/HomeServicesPortal.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable ${SERVICE_NAME}

echo "[4/7] Configuring nginx..."
cat >/etc/nginx/sites-available/${SERVICE_NAME} <<EOF
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name ${DOMAIN} www.${DOMAIN} _;

    client_max_body_size 10M;

    location / {
        proxy_pass http://127.0.0.1:${APP_PORT};
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
}
EOF

ln -sf /etc/nginx/sites-available/${SERVICE_NAME} /etc/nginx/sites-enabled/${SERVICE_NAME}
rm -f /etc/nginx/sites-enabled/default

nginx -t
systemctl restart nginx

echo "[5/7] Starting app..."
if [ -f "$APP_DIR/HomeServicesPortal.dll" ]; then
  systemctl restart ${SERVICE_NAME}
  sleep 2
else
  echo "Skipping app start — DLL missing."
fi

echo "[6/7] Status checks..."
echo "--- nginx ---"
systemctl is-active nginx || true
echo "--- ${SERVICE_NAME} ---"
systemctl is-active ${SERVICE_NAME} || true
systemctl status ${SERVICE_NAME} --no-pager -l | head -15 || true

echo "[7/7] HTTP probe..."
curl -s -o /dev/null -w "localhost:${APP_PORT} => %{http_code}\n" "http://127.0.0.1:${APP_PORT}/" || echo "App not responding on port ${APP_PORT}"

echo ""
echo "========== Done =========="
if [ -f "$APP_DIR/HomeServicesPortal.dll" ]; then
  echo "Open: http://${DOMAIN}/"
  echo "If still 404/502, run: journalctl -u ${SERVICE_NAME} -n 50 --no-pager"
else
  echo "NEXT: Upload publish files to ${APP_DIR}, then run:"
  echo "  systemctl restart ${SERVICE_NAME}"
fi
