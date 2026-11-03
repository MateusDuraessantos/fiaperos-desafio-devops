FROM mcr.microsoft.com/dotnet/sdk:8.0
RUN apt-get update && apt-get install -y --no-install-recommends python3 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /workspace
COPY . .
RUN dotnet restore Greenhouse.sln && dotnet build Greenhouse.sln -c Release --no-restore
ENTRYPOINT ["python3", "scripts/test_bdd.py", "--no-build"]
