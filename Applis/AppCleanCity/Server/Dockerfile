FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CortexiaAuth.Api.csproj .
RUN dotnet restore CortexiaAuth.Api.csproj
COPY . .
RUN dotnet publish CortexiaAuth.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Npgsql charge libgssapi_krb5 pour la negociation GSSAPI/SSPI ; absent de l'image aspnet minimale.
# postgresql-client-18 fournit pg_dump/psql (utilises par ExportController pour sauvegarder/restaurer
# directement sur Neon, sans dependre d'un conteneur Postgres local) : le paquet postgresql-client
# d'Ubuntu 24.04 n'installe que la version 16, alors que pg_dump refuse de dumper un serveur plus
# recent que lui (Neon tourne en Postgres 18) - d'ou le depot officiel PGDG pour la bonne version.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl gnupg ca-certificates \
    && update-ca-certificates \
    && curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc -o /usr/share/keyrings/postgresql.asc \
    && gpg --dearmor -o /usr/share/keyrings/postgresql.gpg /usr/share/keyrings/postgresql.asc \
    && echo "deb [signed-by=/usr/share/keyrings/postgresql.gpg] http://apt.postgresql.org/pub/repos/apt noble-pgdg main" > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-18 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CortexiaAuth.Api.dll"]
