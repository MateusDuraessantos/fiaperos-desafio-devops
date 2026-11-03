FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /workspace
COPY . .
RUN dotnet restore Greenhouse.sln && dotnet build Greenhouse.sln -c Release --no-restore
ENTRYPOINT ["pwsh", "-NoLogo", "-NoProfile", "-File", "scripts/Testar-Bdd.ps1", "-SemBuild"]
