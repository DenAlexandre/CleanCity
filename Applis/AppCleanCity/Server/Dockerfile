FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CortexiaAuth.Api.csproj .
RUN dotnet restore CortexiaAuth.Api.csproj
COPY . .
RUN dotnet publish CortexiaAuth.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CortexiaAuth.Api.dll"]
