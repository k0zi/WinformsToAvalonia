#!/usr/bin/env bash
# Converts every entry of corpus.txt and builds the result. Exits non-zero if any conversion
# throws, any generated project fails to build, or any build prints a warning - the generated
# project has to build warning-free (see CLAUDE.md), and the corpus is where that meets code
# nobody in this repo wrote.
#
#   ./samples/corpus/run.sh            # everything
#   ./samples/corpus/run.sh Matching   # only entries whose source path contains "Matching"
#   KEEP=1 ./samples/corpus/run.sh     # keep every generated project, not only the failing ones
#                                      # (each built Avalonia project is ~0.5 GB)
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CACHE_DIR="$SCRIPT_DIR/.cache"
OUT_DIR="$CACHE_DIR/out"
FILTER="${1:-}"

echo "==> Building WinFormsToAvalonia.Cli..."
dotnet build "$REPO_ROOT/src/WinFormsToAvalonia.Cli/WinFormsToAvalonia.Cli.csproj" -c Release -nodeReuse:false -v q -nologo \
    || { echo "CLI build failed" >&2; exit 2; }
CLI_DLL="$REPO_ROOT/src/WinFormsToAvalonia.Cli/bin/Release/net10.0/WinFormsToAvalonia.Cli.dll"

# One sparse, pinned checkout per (repo, commit), holding just the folders the entries need.
checkout() {
    local repo="$1" commit="$2" path="$3"
    local dir="$CACHE_DIR/src/${repo//\//_}@${commit:0:12}"
    if [ ! -d "$dir/.git" ]; then
        git clone -q --filter=blob:none --no-checkout "https://github.com/$repo.git" "$dir" >&2 || return 1
        git -C "$dir" sparse-checkout init --no-cone >&2
    fi
    local folder
    folder="$(dirname "$path")"
    # The parent folder too, so a sibling project, a linked file or a shared project comes along.
    git -C "$dir" sparse-checkout add "/$(dirname "$folder")/" >&2 || return 1
    git -C "$dir" -c advice.detachedHead=false checkout -q "$commit" >&2 || return 1
    echo "$dir"
}

failures=0
total=0
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

while read -r repo commit source mappings; do
    [[ -z "$repo" || "$repo" == \#* ]] && continue
    [[ -n "$FILTER" && "$source" != *"$FILTER"* ]] && continue
    total=$((total + 1))
    name="$(basename "${source%.*}")"
    out="$OUT_DIR/$name"

    if ! dir="$(checkout "$repo" "$commit" "$source")"; then
        echo "FAIL  $name: could not check out $repo@$commit"
        failures=$((failures + 1))
        continue
    fi

    args=(convert --source "$dir/$source" --output "$out")
    [ -n "${mappings:-}" ] && args+=(--mappings "$SCRIPT_DIR/$mappings")
    if ! conversion="$(dotnet "$CLI_DLL" "${args[@]}" 2>&1)"; then
        echo "FAIL  $name: conversion failed"
        echo "$conversion" | tail -5 | sed 's/^/      /'
        failures=$((failures + 1))
        continue
    fi

    target="$(find "$out" -maxdepth 1 -name '*.slnx' | head -1)"
    [ -z "$target" ] && target="$out"
    build="$(cd "$out" && dotnet build "$target" -nodeReuse:false 2>&1)"
    status=$?
    warnings="$(echo "$build" | grep -E ': warning ' | sort -u)"
    errors="$(echo "$build" | grep -E ': error ' | sort -u)"
    if [ $status -ne 0 ] || [ -n "$warnings" ]; then
        echo "FAIL  $name: build exit $status, $(echo -n "$errors" | grep -c .) error(s), $(echo -n "$warnings" | grep -c .) warning(s)"
        { echo "$errors"; echo "$warnings"; } | grep . | head -10 | sed "s|$out/||; s/^/      /"
        failures=$((failures + 1))
    else
        echo "ok    $name"
        [ -z "${KEEP:-}" ] && rm -rf "$out"
    fi
done < "$SCRIPT_DIR/corpus.txt"

echo "==> $((total - failures))/$total passed (failing output kept in $OUT_DIR)"
[ $failures -eq 0 ]
