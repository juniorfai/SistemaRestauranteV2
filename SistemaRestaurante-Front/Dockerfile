# Frontend de Restaurante Master servido por Caddy, que además hace de proxy hacia la API
# (/api/*) y obtiene el certificado HTTPS automáticamente cuando SITE_ADDRESS es un dominio.
#   docker build -t restaurante-web SistemaRestaurante-Front

# ---------- Compilación de Angular ----------
FROM node:24-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
RUN npx ng build --configuration production

# ---------- Servidor web ----------
FROM caddy:2.8-alpine AS final
COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /src/dist/SistemaRestaurante-Front/browser /srv

# Valores por defecto para probar en local; en producción se cambian en .env.
ENV SITE_ADDRESS=":80" \
    API_UPSTREAM="api:8080"

EXPOSE 80 443
