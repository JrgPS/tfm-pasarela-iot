FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /source
COPY Directory.Build.props global.json ./
COPY src/ ./src/
RUN dotnet publish src/${PROJECT}/${PROJECT}.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROJECT
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out/ ./
ENV IOT_PROJECT=${PROJECT}
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet \"$IOT_PROJECT.dll\" \"$@\"", "--"]
