
###############################################################################
# Build base layer
###############################################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

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
FROM node:20-alpine AS ui-build
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
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . ./

FROM build AS publish

# cdr-auth-server code
COPY ./cdr-auth-server/Source/CdrAuthServer.Domain/. /app/CdrAuthServer.Domain
COPY ./cdr-auth-server/Source/CdrAuthServer.Repository/. /app/CdrAuthServer.Repository
COPY ./cdr-auth-server/Source/CdrAuthServer/. /app/CdrAuthServer
COPY ./cdr-auth-server/Source/CdrAuthServer.Infrastructure/. /app/CdrAuthServer.Infrastructure
COPY ./cdr-auth-server/Source/CdrAuthServer.API.Logger/. /app/CdrAuthServer.API.Logger

COPY ./Directory.Build.props /app/Directory.Build.props
COPY ./Shared/CDR.DataHolder.Admin.API/. /app/Shared/CDR.DataHolder.Admin.API
COPY ./Shared/CDR.DataHolder.Shared.API.Gateway.mTLS/. /app/Shared/CDR.DataHolder.Shared.API.Gateway.mTLS
COPY ./Shared/CDR.DataHolder.Shared.API.Infrastructure/. /app/Shared/CDR.DataHolder.Shared.API.Infrastructure
COPY ./Shared/CDR.DataHolder.Shared.API.Logger/. /app/Shared/CDR.DataHolder.Shared.API.Logger
COPY ./Shared/CDR.DataHolder.Manage.API/. /app/Shared/CDR.DataHolder.Manage.API
COPY ./Shared/CDR.DataHolder.Public.API/. /app/Shared/CDR.DataHolder.Public.API
COPY ./Shared/CDR.DataHolder.Shared.Domain/. /app/Shared/CDR.DataHolder.Shared.Domain
COPY ./Shared/CDR.DataHolder.Shared.Repository/. /app/Shared/CDR.DataHolder.Shared.Repository
COPY ./Shared/CDR.DataHolder.Shared.Business/. /app/Shared/CDR.DataHolder.Shared.Business

COPY ./Common/CDR.DataHolder.Common.API/. /app/Common/CDR.DataHolder.Common.API

COPY ./Banking/CDR.DataHolder.Banking.Domain/. /app/Banking/CDR.DataHolder.Banking.Domain
COPY ./Banking/CDR.DataHolder.Banking.Repository/. /app/Banking/CDR.DataHolder.Banking.Repository
COPY ./Banking/CDR.DataHolder.Banking.Resource.API/. /app/Banking/CDR.DataHolder.Banking.Resource.API
COPY ./Banking/CDR.DataHolder.Banking.Resource.API.UnitTests/. /app/Banking/CDR.DataHolder.Banking.Resource.API.UnitTests

COPY ./Energy/CDR.DataHolder.Energy.Domain/. /app/Energy/CDR.DataHolder.Energy.Domain
COPY ./Energy/CDR.DataHolder.Energy.Repository/. /app/Energy/CDR.DataHolder.Energy.Repository
COPY ./Energy/CDR.DataHolder.Energy.Resource.API/. /app/Energy/CDR.DataHolder.Energy.Resource.API
COPY ./Energy/CDR.DataHolder.Energy.Resource.API.UnitTests/. /app/Energy/CDR.DataHolder.Energy.Resource.API.UnitTests


WORKDIR /app/CdrAuthServer
RUN dotnet publish -c Release -o /app/publish/cdrauthserver

WORKDIR /app/Shared/CDR.DataHolder.Admin.API
RUN dotnet publish -c Release -o /app/publish/admin
WORKDIR /app/Shared/CDR.DataHolder.Manage.API
RUN dotnet publish -c Release -o /app/publish/manage
WORKDIR /app/Shared/CDR.DataHolder.Public.API
RUN dotnet publish -c Release -o /app/publish/public
WORKDIR /app/Common/CDR.DataHolder.Common.API
RUN dotnet publish -c Release -o /app/publish/common
WORKDIR /app/Banking/CDR.DataHolder.Banking.Resource.API
RUN dotnet publish -c Release -o /app/publish/bankingresource
WORKDIR /app/Energy/CDR.DataHolder.Energy.Resource.API
RUN dotnet publish -c Release -o /app/publish/energyresource
WORKDIR /app/Shared/CDR.DataHolder.Shared.API.Gateway.mTLS
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
COPY --from=publish /app/publish/bankingresource ./bankingresource
COPY --from=publish /app/publish/energyresource ./energyresource
COPY --from=publish /app/publish/admin ./admin
COPY --from=publish /app/publish/manage ./manage
COPY --from=publish /app/publish/gateway-mtls ./gateway-mtls
COPY --from=publish /app/publish/public ./public
COPY --from=publish /app/publish/common ./common

# Install wget for use in health checks
RUN apt-get update \
    && apt-get install -y supervisor \
    && apt-get install -y sudo \
    && apt-get install -y wget \
    && rm -rf /var/lib/apt/lists/*

RUN sudo cp ./gateway-mtls/Certificates/ca.crt /usr/local/share/ca-certificates/ca.crt \
    && sudo update-ca-certificates

USER 0
RUN setcap cap_net_bind_service=+ep /usr/sbin/nginx

RUN addgroup --group appgroup --gid 2000 \
    && adduser --uid 1000 --gid 2000 "appuser" \
    && chown -R appuser:appgroup /app \
    && chown -R appuser:appgroup /usr/bin \
    && chown -R appuser:appgroup /usr/local \
    && chown -R appuser:appgroup /tmp \
	&& chown -R appuser:appgroup /var/log/nginx \
	&& chown -R appuser:appgroup /var/lib/nginx

RUN touch /run/nginx.pid \
 && chown -R appuser:appgroup /run/nginx.pid
RUN touch /usr/share/nginx/html/ui/env.js \
 && chown -R appuser:appgroup /usr/share/nginx/html/ui/env.js

USER appuser:appgroup 

ENV ASPNETCORE_URLS=https://+:8001;https://+:8002

FROM final AS Energy

# CdrAuthServer is embedded in MDHE so override settings 
ENV CdrAuthServer__Issuer=https://mock-data-holder-energy:8101
ENV CdrAuthServer__BaseUri=https://mock-data-holder-energy:8101
ENV CdrAuthServer__SecureBaseUri=https://mock-data-holder-energy:8102
ENV CdrAuthServer__ScopesProfile=energy
ENV AccessTokenIntrospectionEndpoint=https://mock-data-holder-energy:8101/connect/introspect-internal
ENV CdrAuthServer__HttpsPort=8101
ENV CdrAuthServer__HttpPort=8180
ENV CdrAuthServer__AuthUiBaseUri=https://mock-data-holder-energy:3100
ENV CdrAuthServer__SeedData__FilePath="Data/customer-seed-data-energy.json"
ENV ConnectionStrings__CDR_Auth_Server_RW="Server=mssql;Database=cdr-mdhe-auth-server;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__CDR_Auth_Server_Migrations_DBO="Server=mssql;Database=cdr-mdhe-auth-server;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__CDR_Auth_Server_Logging_DBO="Server=mssql;Database=cdr-mdhe-auth-server;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__CDR_Auth_Server_RequestResponse_Logging_DBO="Server=mssql;Database=cdr-mdhe-auth-server;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV CdrAuthServer__AutoFillCustomerId="mmoss"
ENV CdrAuthServer__BrandName="Mock Data Holder Energy"
ENV CdrAuthServer__HeadlessMode=false
ENV REACT_APP_DATA_FILE_NAME=customer-seed-data-energy.json
ENV REACT_APP_CLUSTER_DATA_FILE_NAME=cluster-seed-data.json
ENV REACT_APP_DEFAULT_USER_NAME_TEXT="mmoss, hhare"
ENV REACT_APP_CDR_POLICY_LINK=https://cdr-policy
ENV REACT_APP_JWKS_URI=https://mock-data-holder-energy:8101/.well-known/openid-configuration/jwks
ENV REACT_APP_CDR_FAQ_LINK=https://consumerdataright.atlassian.net/wiki/spaces/PT/pages/4948421/CDR+Sandbox

#Energy APIs Overrides
ENV Industry=Energy
ENV ConnectionStrings__DataHolder_DB="Server=mssql;Database=cdr-mdhe;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__DataHolder_Logging_DB=Server="mssql;Database=cdr-mdhe;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__DataHolder_RequestResponse_Logging_DB="Server=mssql;Database=cdr-mdhe;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__DataHolder_Migrations_DB="Server=mssql;Database=cdr-mdhe;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"

 #Manage API
ENV SeedData__FilePath="Data/seed-data.energy.json"

 #Admin API
ENV AdminBaseUri=https://mock-data-holder-energy:8102
ENV DataHolderJwksUri=https://mock-data-holder-energy:8101/.well-known/openid-configuration/jwks
ENV DataHolderIssuer=https://mock-data-holder-energy:8101
ENV RegisterJwksUri=https://mock-register:7000/cdr-register/v1/jwks

#PUBLIC API
ENV Domain=mock-data-holder-energy:8100

#Resource API and Common API
ENV IdentityServerIssuerUri=https://mock-data-holder-energy:8101
ENV IdentityServerUrl=https://mock-data-holder-energy:8101
ENV ResourceBaseUri=https://mock-data-holder-energy:8102


# Entry point #################################################################
ENTRYPOINT ["/usr/bin/supervisord", "-c", "/app/supervisord.conf", "-u", "1000"]

FROM final AS Banking

############ CdrAuthServer is embedded in MDH so override settings 
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
####END Of AuthServer

######Banking overrides for DH################################################
ENV Industry=Banking
ENV ConnectionStrings__DataHolder_DB="Server=mssql;Database=cdr-mdh;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__DataHolder_Logging_DB="Server=mssql;Database=cdr-mdh;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__DataHolder_RequestResponse_Logging_DB="Server=mssql;Database=cdr-mdh;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
ENV ConnectionStrings__DataHolder_Migrations_DB="Server=mssql;Database=cdr-mdh;User Id='SA';Password='Pa{}w0rd2019';MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"

#Manage API
ENV SeedData__FilePath="Data/seed-data.banking.json"

#Admin API
ENV AdminBaseUri=https://mock-data-holder:8002
ENV DataHolderJwksUri=https://mock-data-holder:8001/.well-known/openid-configuration/jwks
ENV DataHolderIssuer=https://mock-data-holder:8001
ENV RegisterJwksUri=https://mock-register:7000/cdr-register/v1/jwks

#PUBLIC API
ENV Domain=mock-data-holder:8000

#Resource API and Common API
ENV IdentityServerIssuerUri=https://mock-data-holder:8001
ENV IdentityServerUrl=https://mock-data-holder:8001
ENV ResourceBaseUri=https://mock-data-holder:8002

# Entry point #################################################################
ENTRYPOINT ["/usr/bin/supervisord", "-c", "/app/supervisord.conf", "-u", "1000"]

FROM final AS NoIndustry

# Entry point #################################################################
ENTRYPOINT ["/usr/bin/supervisord", "-c", "/app/supervisord.conf", "-u", "1000"]