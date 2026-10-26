# syntax=docker/dockerfile:1
# Imagem do frontend. A mesma imagem roda no compose local e no Azure Container Apps (Cap. 13).

# 1. Dependências: só invalida o cache quando o lockfile muda. O cache mount guarda o
#    cache do npm entre builds mesmo quando o lockfile muda.
FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json .npmrc ./
RUN --mount=type=cache,id=npm,target=/root/.npm \
    npm ci --no-audit --no-fund

# 2. Build: typecheck (tsc -b) + bundle. Erro de tipo quebra a imagem, como no backend.
FROM deps AS build
COPY . .
RUN npm run build

# 3. Runtime: nginx sem root (UID 101, porta 8080), sem Node na imagem final.
FROM nginxinc/nginx-unprivileged:stable-alpine AS runtime
ARG SOURCE_REVISION=local
LABEL org.opencontainers.image.revision="${SOURCE_REVISION}" \
      org.opencontainers.image.title="taskmanager-web"
# A base demora dias para sair com os patches do Alpine: o Trivy do pipeline barra CVE
# HIGH com correção disponível. apk upgrade traz as correções; o curl sai porque o
# runtime não usa (menos binário = menos CVE e menos ferramenta para um invasor).
USER root
RUN apk upgrade --no-cache && apk del --no-cache curl
USER 101
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY nginx/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /app/dist /usr/share/nginx/html
# A Api por trás do mesmo host: o cookie de refresh (SameSite=Strict) funciona sem CORS.
# Default = compose. Nome em TLD reservado (.internal): nunca resolve fora da rede do Docker.
# No Azure: API_HOST=<fqdn interno da Api>, API_PORT=443, API_SCHEME=https (Bicep).
ENV API_HOST=api.taskmanager.internal \
    API_PORT=8080 \
    API_SCHEME=http \
    NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
EXPOSE 8080
