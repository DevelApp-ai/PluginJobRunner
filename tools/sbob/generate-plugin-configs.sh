#!/usr/bin/env bash
# Deterministic sbobforge config generator for the PluginJobRunner plugin
# packages (DevelApp-Repo-Global #54).
#
# One config per COMPLETE plugin package (csproj + test corpus):
#   sbobforge/<PkgDir>.json
#     -> sbob/<package-name>.linux.sbob.yaml  (via sbobforge generate)
#
# The package csprojs are the single source of truth for package id and
# version; this script derives the configs from them so nothing is
# hand-maintained. Drift is fixed by re-running this generator, never by
# hand-editing (org decision #26). CI drift-checks the committed configs +
# profiles in .github/workflows/sbob-regenerate.yml.
#
# Fleet scope (DevelApp-Repo-Global #54): the nupkg-shipping plugin
# ecosystem packages of this repo. The fleet is an explicit list below —
# adding a package is a reviewed change, like the pinned generator ref.
# JobRunner is the consuming HOST (Web SDK, never packed) and is NOT a
# plugin package.
#
# PluginJobRunner's layout differs from Golem/LaunchMobile: packages live at
# the REPO ROOT (JobExecutor/, PluginInterface/, JobExecutorModel/) and one
# SHARED test project (JobExecutorTest) covers both the host and the plugin
# packages. Derivation:
#   - SUT   = <PkgDir>/**/*.cs
#   - tests = every test project (Microsoft.NET.Test.Sdk) whose ProjectReference
#     closure (transitive — a package referenced through another package still
#     counts: JobExecutorTest references PluginInterface only through
#     JobExecutor) contains the package csproj. There is no dedicated
#     <PkgDir>.Tests project, so every contributing test project is SHARED:
#     it contributes exactly the files that use the package root namespace
#     (`using <PkgDir>;` or `using <PkgDir>.Sub;`). JobExecutorTest also tests
#     the host's JobExecutorFactory (RPCF plugin loading with temp-path IO),
#     and attributing host behavior to a package profile would both over-claim
#     the package and strand the profile in section-shadowed quarantine
#     (TDS 8.4): the factory's Path.Combine(Path.GetTempPath(), ...) opens are
#     the host's behavior, and its expression-argument candidates are
#     non-liftable by any declared policy. File-level attribution keeps the
#     corpus honest. A new test file that starts using the package changes
#     the derived config: re-run this generator and commit the result (the
#     drift-check failure message says the same).
#   - The namespace match is ANCHORED (using <PkgDir>[;.]) so sibling
#     namespaces sharing a prefix (JobExecutor vs JobExecutorModel) never
#     cross-attribute.
#
# Skips with a WARNING (incomplete packages are a normal mid-build state):
#   - fleet dirs with no <PkgDir>/<PkgDir>.csproj (package gone/renamed)
#   - packages with no test corpus (a behavior profile lifts behaviors from
#     the test corpus; a missing corpus means the package is not done yet —
#     its profile waits for the PR that adds its tests). This is NOT a
#     deferral: JobExecutorModel currently has no test using its namespace.
#   - packages listed in tools/sbob/deferred-plugins.txt: sbobforge cannot
#     represent their behavior yet (section-shadowed quarantine, TDS 8.4).
#     A generator-capability gap, not a thin corpus; evidence per deferred
#     package in DevelApp-Repo-Global #54. The deferral list is committed and
#     reviewed like code: removing a line unblocks the package on the next
#     lane run.
#
# Declared dynamic-destination policies (SBOB-Forge #49, TDS 7.3 tier P): a
# package whose SUT builds request URLs / file paths from identifiers carries
# its per-package policy declarations in tools/sbob/plugin-policies.tsv (same
# committed-and-reviewed contract as the deferral list). The policies flow
# into the derived config here — configs stay derived, never hand-edited (#26).
set -euo pipefail

OUT_DIR="sbobforge"
DEFER_FILE="tools/sbob/deferred-plugins.txt"
POLICY_FILE="tools/sbob/plugin-policies.tsv"
# Fleet: the plugin ecosystem packages of this repo (DevelApp-Repo-Global #54).
FLEET="JobExecutor PluginInterface JobExecutorModel"
mkdir -p "$OUT_DIR"

# Fail closed on a missing deferral list: without it the deferred packages
# would fail every lane run, so its absence is a wiring defect, not a valid state.
if [ ! -f "$DEFER_FILE" ]; then
  echo "::error file=$DEFER_FILE::deferral list missing — restore it or remove deferral support deliberately (fail-closed)"
  exit 1
fi
DEFERRED="$(grep -Ev '^[[:space:]]*(#|$)' "$DEFER_FILE" || true)"
is_deferred() { printf '%s\n' "$DEFERRED" | grep -qxF -- "$1"; }

# Declared dynamic-destination policies: a missing table is a warning (packages
# without dynamic destinations legitimately need no policies), but a malformed
# row is fail-closed — a half-read policy would silently strand a package in
# quarantine.
declare -a P_KIND=() P_ID=() P_MATCH=() P_EMIT=()
if [ -f "$POLICY_FILE" ]; then
  while IFS=$'\t' read -r p_pkg p_kind p_id p_match p_emit; do
    [ -z "${p_pkg:-}" ] && continue
    case "$p_pkg" in '#'*) continue ;; esac
    if [ -z "${p_kind:-}" ] || [ -z "${p_id:-}" ] || [ -z "${p_match:-}" ] || [ -z "${p_emit:-}" ]; then
      echo "::error file=$POLICY_FILE::malformed policy row (package='$p_pkg' needs kind, id, match, emit — tab-separated) (fail-closed)"
      exit 1
    fi
    if [ "$p_kind" != "net" ] && [ "$p_kind" != "path" ]; then
      echo "::error file=$POLICY_FILE::policy '$p_id' has unknown kind '$p_kind' (net|path) (fail-closed)"
      exit 1
    fi
    if [ "$p_kind" = "net" ]; then
      case "$p_emit" in
        *'/'*|*':'*|*' '*)
          echo "::error file=$POLICY_FILE::net policy '$p_id' emit '$p_emit' is not a DNS-name pattern (no scheme, path or whitespace) (fail-closed)"
          exit 1 ;;
      esac
    else
      case "$p_emit" in
        /*) : ;;
        *)
          echo "::error file=$POLICY_FILE::path policy '$p_id' emit '$p_emit' is not an absolute path pattern (fail-closed)"
          exit 1 ;;
      esac
    fi
    P_KIND+=("$p_pkg:$p_kind")
    P_ID+=("$p_pkg:$p_id")
    P_MATCH+=("$p_pkg:$p_match")
    P_EMIT+=("$p_pkg:$p_emit")
  done < <(grep -Ev '^[[:space:]]*(#|$)' "$POLICY_FILE" || true)
else
  echo "::warning file=$POLICY_FILE::no declared-policy table — packages with dynamic destinations (request URLs / dynamic open paths) stay in quarantine without one (SBOB-Forge #49)"
fi

policy_json_array() { # <PkgDir> <kind> -> prints the JSON array body (no brackets)
  local pkg="$1" kind="$2" first=1
  for i in "${!P_KIND[@]}"; do
    [ "${P_KIND[$i]}" != "$pkg:$kind" ] && continue
    [ "$first" -eq 0 ] && printf ',\n'
    first=0
    printf '    { "id": "%s", "match": "%s", "emit": "%s" }' \
      "${P_ID[$i]#*:}" "${P_MATCH[$i]#*:}" "${P_EMIT[$i]#*:}"
  done
  return 0
}

csproj_tag() { # <csproj> <tag> -> prints the tag's value (first real one)
  # Only match "<Tag>" immediately followed by a value character: csprojs
  # often mention tags inside XML comments BEFORE the real element — those
  # lines are skipped.
  awk -F"<$2>" -v tag="$2" '$0 ~ "<"tag">[0-9A-Za-z]" { split($2, a, "<"); gsub(/^[[:space:]]+|[[:space:]]+$/, "", a[1]); print a[1]; exit }' "$1"
}

# Repo-relative ProjectReference closure (transitive): a package referenced
# only through another package still counts as tested by the project.
ref_closure() { # <test-csproj> -> prints every repo csproj reachable via ProjectReference (including itself)
  local start="$1"
  declare -a queue=("$start") seen=()
  local cur dir rel p s
  while [ "${#queue[@]}" -gt 0 ]; do
    cur="${queue[0]}"; queue=("${queue[@]:1}")
    local skip=0
    for s in "${seen[@]:-}"; do [ "$s" = "$cur" ] && skip=1; done
    [ "$skip" -eq 1 ] && continue
    seen+=("$cur")
    dir="$(dirname "$cur")"
    while IFS= read -r rel; do
      [ -z "$rel" ] && continue
      rel="${rel//\\//}" # csproj Include paths use Windows separators
      p="$(realpath -m --relative-to=. "$dir/$rel")"
      [ -f "$p" ] && queue+=("$p")
    done < <(grep -o 'ProjectReference Include="[^"]*"' "$cur" 2>/dev/null | sed 's/^ProjectReference Include="//; s/"$//' || true)
  done
  printf '%s\n' "${seen[@]}"
}

emit_config() { # <pkg_dir> <pkg_id> <version> <test_glob>...
  local pkg_dir="$1" pkg_id="$2" version="$3"
  shift 3
  local net_body path_body tline="" first=1 g
  net_body="$(policy_json_array "$pkg_dir" net)"
  path_body="$(policy_json_array "$pkg_dir" path)"
  for g in "$@"; do
    if [ "$first" -eq 1 ]; then tline="\"$g\""; first=0
    else tline="$tline, \"$g\""
    fi
  done
  {
    cat <<EOF
{
  "packageType": "nuget",
  "packageName": "$pkg_id",
  "packageVersion": "$version",
  "namespace": "develapp",
  "targetOs": "linux",
  "architectures": [ "amd64" ],
  "sutGlobs": [ "$pkg_dir/**/*.cs" ],
  "testGlobs": [ $tline ],
EOF
    # Byte-identity contract: a package with no declared policies must derive
    # a config in exactly this shape — clean configs never drift on a
    # policy-table change (netPolicies omitted when empty, pathPolicies []).
    if [ -n "$path_body" ]; then
      printf '  "pathPolicies": [\n%s\n  ],\n' "$path_body"
    else
      printf '  "pathPolicies": [],\n'
    fi
    if [ -n "$net_body" ]; then
      printf '  "netPolicies": [\n%s\n  ],\n' "$net_body"
    fi
    cat <<EOF
  "execPathPolicy": {},
  "allowParseError": []
}
EOF
  } > "$OUT_DIR/$pkg_dir.json"
}

for pkg_dir in $FLEET; do
  proj="$pkg_dir/$pkg_dir.csproj"
  if [ ! -f "$proj" ]; then
    echo "::warning file=$proj::fleet package missing — update the FLEET list in $0 when a package is added, renamed or removed (fail-closed)"
    continue
  fi
  if [ -z "$(find "$pkg_dir" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null)" ]; then
    echo "::warning file=$proj::no sources — profile deferred until the package is complete"
    continue
  fi
  # Test corpus: every test project (Test SDK) whose ProjectReference closure
  # contains this package's csproj.
  declare -a test_globs=()
  for tproj in */*.csproj; do
    [ -f "$tproj" ] || continue
    grep -q 'Microsoft.NET.Test.Sdk' "$tproj" || continue
    ref_closure "$tproj" | grep -qxF -- "$proj" || continue
    tdir="$(dirname "$tproj")"
    if [ "$(basename "$tdir")" = "$pkg_dir.Tests" ]; then
      # Dedicated test project: the whole tree is the package's corpus.
      test_globs+=("$tdir/**/*.cs")
    else
      # Shared test project: only the files that use the package root
      # namespace belong to this package's corpus (anchored match — see
      # header).
      while IFS= read -r f; do
        grep -Eq "using $pkg_dir[;.]" "$f" && test_globs+=("$f")
      done < <(find "$tdir" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' | sort)
    fi
  done
  if [ "${#test_globs[@]}" -eq 0 ]; then
    echo "::warning file=$proj::no test corpus — a behavior profile needs a test corpus (add tests that use the $pkg_dir namespace); profile deferred until then"
    continue
  fi
  version="$(csproj_tag "$proj" Version)"
  if ! printf '%s' "$version" | grep -Eq '^[0-9A-Za-z.+-]+$'; then
    echo "::warning file=$proj::no usable <Version> (got '$version') — its profile is deferred until the package is versioned (purl binding needs a version, #39/#88)"
    continue
  fi
  if is_deferred "$pkg_dir"; then
    echo "::warning file=$proj::profile deferred by $DEFER_FILE — sbobforge cannot represent its behavior yet (section-shadowed quarantine, TDS 8.4); evidence: DevelApp-Repo-Global #54"
    continue
  fi
  pkg_id="$(csproj_tag "$proj" PackageId)"
  if ! printf '%s' "$pkg_id" | grep -Eq '^[0-9A-Za-z.+-]+$'; then
    echo "::warning file=$proj::no usable <PackageId> (got '$pkg_id') — its profile is deferred until the package has a package id"
    continue
  fi
  emit_config "$pkg_dir" "$pkg_id" "$version" "${test_globs[@]}"
  echo "config: $OUT_DIR/$pkg_dir.json ($pkg_id@$version)"
done

# Prune configs whose package no longer exists (deleted/renamed packages,
# fleet-list changes) or is on the deferral list (a config must never linger
# for a deferred package — the lane and drift-check loop over the derived
# configs).
for cfg in "$OUT_DIR"/*.json; do
  [ -e "$cfg" ] || continue
  p="$(basename "$cfg" .json)"
  keep=0
  for pkg_dir in $FLEET; do
    [ "$p" = "$pkg_dir" ] && keep=1
  done
  if [ "$keep" -eq 0 ] || [ ! -f "$p/$p.csproj" ]; then
    rm "$cfg"
    echo "pruned stale config $cfg (package gone or off the fleet list)"
  elif is_deferred "$p"; then
    rm "$cfg"
    echo "pruned deferred config $cfg (package on the deferral list — remove its committed config/profile in the same PR that adds the line)"
  fi
done
