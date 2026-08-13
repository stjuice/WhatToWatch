# Frontend build
FROM node:20-bookworm-slim AS frontend
WORKDIR /src/ClientApp
COPY WhatToWatch/ClientApp/package.json WhatToWatch/ClientApp/package-lock.json ./
RUN npm ci
COPY WhatToWatch/ClientApp/ ./
RUN npm run build

# .NET restore + publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY WhatToWatch/WhatToWatch.csproj WhatToWatch/
COPY ImdbWatchlists/ImdbWatchlists.csproj ImdbWatchlists/
RUN dotnet restore WhatToWatch/WhatToWatch.csproj
COPY WhatToWatch/ WhatToWatch/
COPY ImdbWatchlists/ ImdbWatchlists/
COPY --from=frontend /src/ClientApp/dist/ WhatToWatch/wwwroot/
RUN dotnet publish WhatToWatch/WhatToWatch.csproj \
    -c Release \
    -o /app/publish \
    /p:SkipSpaBuild=true \
    --no-restore

# Runtime with Playwright Chromium + Linux dependencies
# SDK image is used so `playwright install --with-deps` can install browsers and OS packages.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

COPY --from=build /app/publish .

# playwright.ps1 ships with the Microsoft.Playwright package, so the browser build
# always matches the referenced library version.
RUN pwsh ./playwright.ps1 install --with-deps chromium

EXPOSE 8080
ENTRYPOINT ["dotnet", "WhatToWatch.dll"]
