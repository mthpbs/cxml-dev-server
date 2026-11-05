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

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# SQLite lives in /app/data; mount a volume there to keep messages, orders and configuration.
# The base image's non-root user "app" must be able to write to it.
RUN mkdir -p /app/data && chown -R app:app /app/data
VOLUME /app/data

ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/app/data/cxml-dev.db"
# Optional, set at `docker run` time:
#   -e Cxml__PublicBaseUrl=https://dev.example.com   externally visible URL (StartPage and Profile URLs)
#   -e Cxml__AllowUnknownPartners=true
EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "CxmlDevServer.Web.dll"]
