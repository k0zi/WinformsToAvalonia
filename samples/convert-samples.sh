#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

SAMPLES_DIR="$REPO_ROOT/samples"
WINFORMS_DIR="$SAMPLES_DIR/WinForms"
AVALONIA_DIR="$SAMPLES_DIR/Avalonia"
CLI_PROJECT="$REPO_ROOT/src/WinFormsToAvalonia.Cli/WinFormsToAvalonia.Cli.csproj"

if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet CLI is required but not found in PATH." >&2
    exit 1
fi

if [ ! -d "$WINFORMS_DIR" ]; then
    echo "Error: WinForms samples directory not found at $WINFORMS_DIR" >&2
    exit 1
fi

echo "==> Building WinFormsToAvalonia.Cli..."
dotnet build "$CLI_PROJECT" -c Release

CLI_DLL="$REPO_ROOT/src/WinFormsToAvalonia.Cli/bin/Release/net10.0/WinFormsToAvalonia.Cli.dll"
if [ ! -f "$CLI_DLL" ]; then
    CLI_DLL="$REPO_ROOT/src/WinFormsToAvalonia.Cli/bin/Debug/net10.0/WinFormsToAvalonia.Cli.dll"
fi

if [ ! -f "$CLI_DLL" ]; then
    echo "Error: WinformsToAvalonia.Cli binary not found after build." >&2
    exit 1
fi

echo "==> Preparing Avalonia directory and solution..."
mkdir -p "$AVALONIA_DIR"
dotnet new sln -n Avalonia -o "$AVALONIA_DIR" --force

SLN_FILE="$(find "$AVALONIA_DIR" -maxdepth 1 -name "Avalonia.sln*" | head -n 1)"
if [ -z "$SLN_FILE" ]; then
    SLN_FILE="$AVALONIA_DIR/Avalonia.slnx"
fi

echo "==> Converting WinForms projects to Avalonia..."
success_count=0
fail_count=0

while IFS= read -r proj_file; do
    [ -z "$proj_file" ] && continue
    proj_name="$(basename "$proj_file" .csproj)"
    output_dir="$AVALONIA_DIR/$proj_name"

    echo ""
    echo "------------------------------------------------------------"
    echo "Converting: $proj_name"
    echo "Source:     $proj_file"
    echo "Output:     $output_dir"
    echo "------------------------------------------------------------"

    if dotnet "$CLI_DLL" convert --source "$proj_file" --output "$output_dir" --force; then
        gen_csproj="$(find "$output_dir" -maxdepth 1 -name "*.csproj" | head -n 1 || true)"
        if [ -n "$gen_csproj" ]; then
            echo "Adding $gen_csproj to solution $SLN_FILE..."
            dotnet sln "$SLN_FILE" add "$gen_csproj"
            success_count=$((success_count + 1))
        else
            echo "Warning: No .csproj found in $output_dir to add to solution." >&2
        fi
    else
        echo "Conversion skipped or failed for $proj_name ($proj_file)." >&2
        fail_count=$((fail_count + 1))
    fi
done < <(find "$WINFORMS_DIR" -type f -name "*.csproj" | sort)

echo ""
echo "============================================================"
echo "==> Conversion completed!"
echo "Successfully converted and added projects: $success_count"
if [ "$fail_count" -gt 0 ]; then
    echo "Skipped / failed projects:                 $fail_count"
fi
echo "Avalonia solution: $SLN_FILE"
echo "Converted projects are in: $AVALONIA_DIR"
echo "============================================================"
