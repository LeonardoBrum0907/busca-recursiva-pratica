#!/usr/bin/env bash
# Idempotent repository bootstrap for the busca-recursiva-pratica stack.
# Installs stable toolchains (.NET 9 SDK, PostgreSQL 16) when missing and
# refreshes project dependencies (NuGet + npm). Database seeding happens in
# start.sh, which needs a running server.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

echo "==> Ensuring .NET 9 SDK"
if ! "$DOTNET_ROOT/dotnet" --list-sdks 2>/dev/null | grep -q '^9\.'; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 9.0 --install-dir "$DOTNET_ROOT"
fi
# Expose dotnet on PATH for interactive shells and terminals.
sudo ln -sf "$DOTNET_ROOT/dotnet" /usr/local/bin/dotnet

echo "==> Ensuring PostgreSQL 16"
if ! command -v pg_ctlcluster >/dev/null 2>&1; then
  sudo apt-get update -qq
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq postgresql-16 postgresql-contrib-16
fi

echo "==> Restoring and building the .NET solution"
"$DOTNET_ROOT/dotnet" restore api/BuscaApi.sln
"$DOTNET_ROOT/dotnet" build api/BuscaApi.sln -c Debug --no-restore

echo "==> Installing frontend dependencies"
if [ -f web/package-lock.json ]; then
  npm --prefix web ci
else
  npm --prefix web install
fi

echo "==> install.sh completed"
