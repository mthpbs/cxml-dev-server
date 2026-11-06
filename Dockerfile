# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# cXML Dev Server
#
#   docker build -t cxml-dev-server .
#   docker run --rm -p 8080:8080 -v cxml-data:/app/data cxml-dev-server
#
# The build context must be the repository root: Cxml.Protocol embeds the cXML DTDs
# from ../../cXML-spec/cXML_DTDs (see src/Cxml.Protocol/Cxml.Protocol.csproj), so that
# folder has to be inside the context (.dockerignore keeps it and drops the rest of cXML-spec).
# ---------------------------------------------------------------------------

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Solution-wide build settings and package versions.
COPY Directory.Build.props Directory.Packages.props .editorconfig ./

# Restore first, with only the project files, so this layer is cached until a package changes.
COPY src/Cxml.Protocol/Cxml.Protocol.csproj src/Cxml.Protocol/
COPY src/CxmlDevServer.Core/CxmlDevServer.Core.csproj src/CxmlDevServer.Core/
COPY src/CxmlDevServer.Infrastructure/CxmlDevServer.Infrastructure.csproj src/CxmlDevServer.Infrastructure/
COPY src/CxmlDevServer.Web/CxmlDevServer.Web.csproj src/CxmlDevServer.Web/
RUN dotnet restore src/CxmlDevServer.Web/CxmlDevServer.Web.csproj

# Sources and the DTDs that Cxml.Protocol embeds as resources.
COPY src/ src/
COPY cXML-spec/cXML_DTDs/ cXML-spec/cXML_DTDs/

RUN dotnet publish src/CxmlDevServer.Web/CxmlDevServer.Web.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# Without the Blazor script the UI loads but is not interactive; fail the build instead of shipping that
# (see RequiresAspNetWebAssets in src/CxmlDevServer.Web/CxmlDevServer.Web.csproj).
RUN test -f /app/publish/wwwroot/_framework/blazor.web.js \
    || { echo "blazor.web.js is missing from the publish output" >&2; exit 1; }

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# Passed at build time, e.g. docker build --build-arg SOURCE_URL=https://github.com/<owner>/<repo> .
# The repository has no license file yet: set LICENSES to an SPDX identifier once it has one.
ARG SOURCE_URL=https://github.com/mthpbs/cxml-dev-server
ARG LICENSES=NOASSERTION
LABEL org.opencontainers.image.title="cXML Dev Server" \
      org.opencontainers.image.description="A cXML 1.2.071 supplier simulator for testing PunchOut and OrderRequest integrations." \
      org.opencontainers.image.source="${SOURCE_URL}" \
      org.opencontainers.image.licenses="${LICENSES}"
WORKDIR /app
COPY --from=build /app/publish ./

# SQLite lives in /app/data; mount a volume there to keep messages, orders and configuration.
# The base image's non-root user "app" must be able to write to it.
RUN mkdir -p /app/data && chown -R app:app /app/data
VOLUME /app/data

# ASPNETCORE_FORWARDEDHEADERS_ENABLED makes the default host honour X-Forwarded-For/-Proto from any proxy, so a
# TLS-terminating reverse proxy in front (Azure Container Apps ingress, Caddy, nginx) yields https:// StartPage URLs.
# Only run this image behind such a proxy (or not exposed directly), or the headers can be spoofed by clients.
# DataProtection__KeysPath keeps the login cookie and antiforgery keys in the data volume across restarts.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ConnectionStrings__Default="Data Source=/app/data/cxml-dev.db" \
    DataProtection__KeysPath=/app/data/keys
# Optional, set at `docker run` time:
#   -e Cxml__PublicBaseUrl=https://dev.example.com   externally visible URL (StartPage and Profile URLs)
#   -e Cxml__AllowUnknownPartners=true
#   -e Cxml__SeedDemoPartners=false                  no demo trading partners (shared environments)
#   -e Admin__Enabled=true -e Admin__PasswordHash=... admin login (create the hash: docker run -it --rm <image> hash-password)
EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "CxmlDevServer.Web.dll"]
