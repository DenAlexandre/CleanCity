FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CortexiaAuth.Api.csproj .
RUN dotnet restore CortexiaAuth.Api.csproj
COPY . .
RUN dotnet publish CortexiaAuth.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Npgsql charge libgssapi_krb5 pour la negociation GSSAPI/SSPI ; absent de l'image aspnet minimale.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CortexiaAuth.Api.dll"]
