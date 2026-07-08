#!/usr/bin/env bash
# One-time GCP VM setup for api.sahulatghartak.com (Ubuntu 22.04).
# Run on the server: sudo bash setup-server.sh

set -euo pipefail

APP_DIR="/var/www/sahulatghartak-api"
APP_USER="coditiumsolutions"
SERVICE_NAME="sahulatghartak-api"
APP_PORT=5300
DOMAIN="api.sahulatghartak.com"

echo "==> Ensuring ASP.NET Core 8 runtime..."
if ! dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App 8"; then
  wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update
  apt-get install -y aspnetcore-runtime-8.0
fi

echo "==> Ensuring nginx and certbot..."
apt-get install -y nginx certbot python3-certbot-nginx

echo "==> Creating app directory..."
mkdir -p "$APP_DIR/wwwroot/uploads/providers"
mkdir -p "$APP_DIR/wwwroot/uploads/documents"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"

echo "==> Installing systemd service..."
cat >/etc/systemd/system/${SERVICE_NAME}.service <<EOF
[Unit]
Description=Sahulat Ghar Tak API (api.sahulatghartak.com)
After=network.target

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=/usr/bin/dotnet ${APP_DIR}/HomeServicesPortal.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=${SERVICE_NAME}
User=${APP_USER}
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

echo "==> Installing nginx site..."
cat >/etc/nginx/sites-available/${SERVICE_NAME} <<EOF
server {
    listen 80;
    server_name ${DOMAIN};

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
nginx -t
systemctl daemon-reload
systemctl enable ${SERVICE_NAME}
systemctl restart nginx

echo "==> Requesting SSL certificate (if DNS points to this server)..."
if certbot --nginx -d "${DOMAIN}" --non-interactive --agree-tos -m admin@sahulatghartak.com --redirect 2>/dev/null; then
  echo "SSL certificate installed."
else
  echo "Certbot skipped or failed — run manually after DNS propagates:"
  echo "  sudo certbot --nginx -d ${DOMAIN}"
fi

echo "==> Done. Deploy app files to ${APP_DIR}, then:"
echo "    sudo systemctl restart ${SERVICE_NAME}"
