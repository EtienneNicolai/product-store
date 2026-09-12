# Render has no native .NET runtime (unlike Node/Python/Ruby/Go) - Docker is
# required. Build context is backend/Store.Api (see Render's Root Directory
# setting), so paths below are relative to that, not the repo root.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Store.Api.csproj .
RUN dotnet restore Store.Api.csproj

COPY . .
RUN dotnet publish Store.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render assigns $PORT at runtime (not build time), and ASPNETCORE_URLS needs
# it - shell-form CMD is required here so the variable actually expands;
# exec-form ("dotnet", "Store.Api.dll") would pass "$PORT" through literally.
CMD ASPNETCORE_URLS=http://+:$PORT dotnet Store.Api.dll
