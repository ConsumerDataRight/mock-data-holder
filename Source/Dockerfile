
###############################################################################
# Build base layer
###############################################################################
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

# Install nodejs, npm and live-server, which are needed by the auth UI
RUN apt-get update
RUN apt-get install -y nodejs
RUN apt-get install -y npm
RUN apt-get install -y nginx

WORKDIR /app
EXPOSE 8001
EXPOSE 8002
EXPOSE 3000
ENV ASPNETCORE_ENVIRONMENT=Release

###############################################################################
# Build CdrAuthServer UI layer
###############################################################################
FROM node:14-alpine AS ui-build
WORKDIR /app

ARG target_environment=production
ENV PATH /app/node_modules/.bin:$PATH

RUN npm install react-scripts@latest -g 
COPY ./cdr-auth-server/Source/CdrAuthServer.UI/package.json package.json
COPY ./cdr-auth-server/Source/CdrAuthServer.UI/package-lock.json package-lock.json
COPY ./cdr-auth-server/Source/CdrAuthServer.UI/craco.config.js craco.config.js
COPY ./cdr-auth-server/Source/CdrAuthServer.UI/.env.${target_environment} .env.local
RUN npm ci

COPY ./cdr-auth-server/Source/CdrAuthServer.UI/. .
RUN npm run build

# Copy nginx config to container.
COPY ./cdr-auth-server/Source/CdrAuthServer.UI/nginx/nginx.conf /etc/nginx/conf.d/default.conf
COPY ./cdr-auth-server/Source/CdrAuthServer.UI/nginx/nginx.gzip.conf /etc/nginx/conf.d/gzip.conf

# Copy the TLS certificates to the container.
COPY ./cdr-auth-server/Source/CdrAuthServer.UI/nginx/ssl/authserver-ui.crt /etc/nginx/ssl/authserver-ui.crt
COPY ./cdr-auth-server/Source/CdrAuthServer.UI/nginx/ssl/authserver-ui.key /etc/nginx/ssl/authserver-ui.key

###############################################################################
# Build CdrAuthServer API layer
###############################################################################
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY . ./

FROM build AS publish

# cdr-auth-server code
COPY ./cdr-auth-server/Source/CdrAuthServer.Domain/. /app/CdrAuthServer.Domain
COPY ./cdr-auth-server/Source/CdrAuthServer.Repository/. /app/CdrAuthServer.Repository
COPY ./cdr-auth-server/Source/CdrAuthServer/. /app/CdrAuthServer
COPY ./cdr-auth-server/Source/CdrAuthServer.Infrastructure/. /app/CdrAuthServer.Infrastructure
COPY ./cdr-auth-server/Source/CdrAuthServer.API.Logger/. /app/CdrAuthServer.API.Logger

COPY ./CDR.DataHolder.API.Infrastructure/. /app/CDR.DataHolder.API.Infrastructure
COPY ./CDR.DataHolder.Repository/. /app/CDR.DataHolder.Repository
COPY ./CDR.DataHolder.Domain/. /app/CDR.DataHolder.Domain
COPY ./CDR.DataHolder.Admin.API/. /app/CDR.DataHolder.Admin.API
COPY ./CDR.DataHolder.Manage.API/. /app/CDR.DataHolder.Manage.API
COPY ./CDR.DataHolder.Public.API/. /app/CDR.DataHolder.Public.API
COPY ./CDR.DataHolder.Resource.API/. /app/CDR.DataHolder.Resource.API
COPY ./CDR.DataHolder.API.Gateway.mTLS/. /app/CDR.DataHolder.API.Gateway.mTLS
COPY ./CDR.DataHolder.API.Logger/. /app/CDR.DataHolder.API.Logger

WORKDIR /app/CdrAuthServer
RUN dotnet publish -c Release -o /app/publish/cdrauthserver

WORKDIR /app/CDR.DataHolder.Admin.API
RUN dotnet publish -c Release -o /app/publish/admin
WORKDIR /app/CDR.DataHolder.Manage.API
RUN dotnet publish -c Release -o /app/publish/manage
WORKDIR /app/CDR.DataHolder.Public.API
RUN dotnet publish -c Release -o /app/publish/public
WORKDIR /app/CDR.DataHolder.Resource.API
RUN dotnet publish -c Release -o /app/publish/resource
WORKDIR /app/CDR.DataHolder.API.Gateway.mTLS
RUN dotnet publish -c Release -o /app/publish/gateway-mtls

COPY supervisord.conf /app/publish/supervisord.conf

COPY wait-until-manage-healthy-then-start.sh /app/publish/wait-until-manage-healthy-then-start.sh

###############################################################################
# Build final layer
###############################################################################
FROM base AS final
WORKDIR /app

# Auth UI #####################################################################
COPY --from=ui-build /app/build ./cdrauthserver-ui

# Copy build files to nginx output folder.
COPY --from=ui-build /app/build /usr/share/nginx/html/ui
COPY --from=ui-build /etc/nginx/conf.d/default.conf /etc/nginx/conf.d/default.conf
COPY --from=ui-build /etc/nginx/conf.d/gzip.conf /etc/nginx/conf.d/gzip.conf
COPY --from=ui-build /etc/nginx/ssl/authserver-ui.crt /etc/nginx/ssl/authserver-ui.crt
COPY --from=ui-build /etc/nginx/ssl/authserver-ui.key /etc/nginx/ssl/authserver-ui.key

# Auth API ####################################################################
COPY --from=publish /app/publish/supervisord.conf .
COPY --from=publish /app/publish/wait-until-manage-healthy-then-start.sh .
COPY --from=publish /app/publish/cdrauthserver ./cdrauthserver
COPY --from=publish /app/publish/resource ./resource
COPY --from=publish /app/publish/admin ./admin
COPY --from=publish /app/publish/manage ./manage
COPY --from=publish /app/publish/gateway-mtls ./gateway-mtls
COPY --from=publish /app/publish/public ./public

RUN apt-get update && apt-get install -y supervisor

RUN apt-get update && apt-get install -y sudo

# Install wget for use in health checks
RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*

RUN sudo cp ./gateway-mtls/Certificates/ca.crt /usr/local/share/ca-certificates/ca.crt

RUN sudo update-ca-certificates

USER 0
RUN setcap cap_net_bind_service=+ep /usr/sbin/nginx

RUN addgroup --group appgroup --gid 2000 \
&& adduser \
    --uid 1000 \
    --gid 2000 \
    "appuser"
RUN chown -R appuser:appgroup /app
RUN chown -R appuser:appgroup /usr/bin
RUN chown -R appuser:appgroup /usr/local
RUN chown -R appuser:appgroup /tmp
RUN chown -R appuser:appgroup /var/log/nginx
RUN chown -R appuser:appgroup /var/lib/nginx
RUN touch /run/nginx.pid \
 && chown -R appuser:appgroup /run/nginx.pid
RUN touch /usr/share/nginx/html/ui/env.js \
 && chown -R appuser:appgroup /usr/share/nginx/html/ui/env.js

USER appuser:appgroup 

ENV ASPNETCORE_URLS=https://+:8001;https://+:8002

# CdrAuthServer is embedded in MDH so override settings 
ENV CdrAuthServer__Issuer=https://mock-data-holder:8001
ENV CdrAuthServer__BaseUri=https://mock-data-holder:8001
ENV CdrAuthServer__SecureBaseUri=https://mock-data-holder:8002
ENV CdrAuthServer__ScopesProfile=banking
ENV AccessTokenIntrospectionEndpoint=https://mock-data-holder:8001/connect/introspect-internal
ENV CdrAuthServer__HttpsPort=8001
ENV CdrAuthServer__HttpPort=8080
ENV CdrAuthServer__AuthUiBaseUri=https://mock-data-holder:3000
ENV ConnectionStrings__CDR_Auth_Server_RW="Server=mssql;Database=cdr-mdh-auth-server;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__CDR_Auth_Server_Migrations_DBO="Server=mssql;Database=cdr-mdh-auth-server;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__CDR_Auth_Server_Logging_DBO="Server=mssql;Database=cdr-mdh-auth-server;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__CDR_Auth_Server_RequestResponse_Logging_DBO="Server=mssql;Database=cdr-mdh-auth-server;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV CdrAuthServer__HeadlessMode=false
ENV REACT_APP_DATA_FILE_NAME=customer-seed-data.json
ENV REACT_APP_CLUSTER_DATA_FILE_NAME=cluster-seed-data.json
ENV REACT_APP_DEFAULT_USER_NAME_TEXT="jwilson, ksmith"
ENV REACT_APP_CDR_POLICY_LINK=https://cdr-policy
ENV REACT_APP_JWKS_URI=https://mock-data-holder:8001/.well-known/openid-configuration/jwks
ENV REACT_APP_CDR_FAQ_LINK=https://consumerdataright.atlassian.net/wiki/spaces/PT/pages/4948421/CDR+Sandbox

# Entry point #################################################################
ENTRYPOINT ["/usr/bin/supervisord", "-c", "/app/supervisord.conf", "-u", "1000"]
