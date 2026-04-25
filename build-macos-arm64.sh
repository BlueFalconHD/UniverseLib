#!/usr/bin/env bash
set -euo pipefail

# Simple cross-platform build helper for UniverseLib on macOS arm64.
# Builds the configurations that are relevant for modern IL2CPP/CoreCLR BepInEx setups,
# but can be run on any platform with a recent .NET SDK installed.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

CONFIGS=(
  Release_IL2CPP_Interop_BIE
  Release_IL2CPP_Interop_ML
  Release_IL2CPP_Unhollower
  Release_Mono
)

for cfg in "${CONFIGS[@]}"; do
  echo "[UniverseLib] Building configuration '$cfg'..."
  dotnet build ./src/UniverseLib.sln -c "$cfg"
done

echo "[UniverseLib] Build completed. Artifacts are under 'Release/'."
