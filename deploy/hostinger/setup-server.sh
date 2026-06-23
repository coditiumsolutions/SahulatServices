#!/usr/bin/env bash
# One-time server setup for Hostinger VPS (Ubuntu/Debian).
# Run on the server as root: bash setup-server.sh

set -euo pipefail

APP_DIR="/var/www/sahulatghartak"
APP_USER="www-data"
SERVICE_NAME="sahulatghartak"
APP_PORT=5000
DOMAIN="sahulatghartak.com"

echo "==> Installing .NET 8 ASP.NET runtime..."
if ! command -v dotnet >/dev/null 2>&1; then
  wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update
  apt-get install -y aspnetcore-runtime-8.0
fi

echo "==> Installing nginx..."
apt-get install -y nginx

echo "==> Creating app directory..."
mkdir -p "$APP_DIR"
mkdir -p "$APP_DIR/wwwroot/uploads/providers"
mkdir -p "$APP_DIR/wwwroot/uploads/documents"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"

echo "==> Installing systemd service..."
cat >/etc/systemd/system/${SERVICE_NAME}.service <<EOF
[Unit]
Description=SahulatGharTak Home Services Portal
After=network.target

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=/usr/bin/dotnet ${APP_DIR}/HomeServicesPortal.dll
Restart=always
RestartSec=10
User=${APP_USER}
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

echo "==> Installing nginx site..."
cat >/etc/nginx/sites-available/${SERVICE_NAME} <<'EOF'
server {
    listen 80;
    server_name sahulatghartak.com www.sahulatghartak.com;

    client_max_body_size 10M;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
EOF

ln -sf /etc/nginx/sites-available/${SERVICE_NAME} /etc/nginx/sites-enabled/${SERVICE_NAME}
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl enable ${SERVICE_NAME}
systemctl restart nginx

echo "==> Done. Deploy files to ${APP_DIR}, then run:"
echo "    systemctl restart ${SERVICE_NAME}"
echo "Optional SSL: certbot --nginx -d ${DOMAIN} -d www.${DOMAIN}"
