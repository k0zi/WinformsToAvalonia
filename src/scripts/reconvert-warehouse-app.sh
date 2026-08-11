#!/usr/bin/env bash
# Rebuilds the converter, (re)installs it as a sandboxed dotnet tool, and reconverts the
# WarehouseApp sample from scratch. Mirrors the packaged-tool workflow documented in
# CLAUDE.md's "Packaging" section - --tool-path (not --global) keeps this fully sandboxed.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

CONVERTED_DIR="$SRC_DIR/SampleWinFormsApp/WarehouseAvaloniaApp"
WAREHOUSE_DIR="$SRC_DIR/SampleWinFormsApp/WarehouseApp"
NUPKG_DIR="/tmp/wf2av-nupkg"
TOOL_PATH="/tmp/wf2av-tool"
PACKAGE_ID="WinformsToAvalonia.Converter"
TOOL_COMMAND="winforms2avalonia"

cd "$SRC_DIR"

echo "==> Removing existing WarehouseAvaloniaApp project"
rm -rf "$CONVERTED_DIR"

echo "==> Building Converter.Cli (Release)"
dotnet build Converter.Cli/Converter.Cli.csproj -c Release

echo "==> Packing Converter.Cli as a dotnet tool"
rm -rf "$NUPKG_DIR"
dotnet pack Converter.Cli/Converter.Cli.csproj -c Release -o "$NUPKG_DIR"

if [ -d "$TOOL_PATH" ]; then
    echo "==> Uninstalling previous tool install at $TOOL_PATH"
    dotnet tool uninstall --tool-path "$TOOL_PATH" "$PACKAGE_ID" || true
fi
rm -rf "$TOOL_PATH"

echo "==> Installing freshly built tool to $TOOL_PATH"
dotnet tool install --tool-path "$TOOL_PATH" --add-source "$NUPKG_DIR" "$PACKAGE_ID"

echo "==> Converting WarehouseApp -> WarehouseAvaloniaApp"
"$TOOL_PATH/$TOOL_COMMAND" convert -i "$WAREHOUSE_DIR" -o "$CONVERTED_DIR" --no-interactive --no-git

echo "==> Done. Output at $CONVERTED_DIR"
