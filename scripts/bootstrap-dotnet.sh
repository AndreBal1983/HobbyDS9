#!/usr/bin/env bash
set -euo pipefail

# Cloud-only SDK preparation. Repository files are not rewritten.
sdk_version=10.0.401
sdk_root=/workspace/.tools/dotnet
sdk_cache=/workspace/.cache/hobbyds9
export DOTNET_CLI_HOME="$sdk_cache/dotnet-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

mkdir -p "$sdk_root" "$sdk_cache"
if [[ -x "$sdk_root/dotnet" && -d "$sdk_root/sdk/$sdk_version" ]]; then
  "$sdk_root/dotnet" --version
  exit 0
fi

sdk_archive="$sdk_cache/dotnet-sdk-$sdk_version-linux-x64.tar.gz"
curl --fail --silent --show-error --location --retry 2 \
  --output "$sdk_archive" \
  "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdk_version/dotnet-sdk-$sdk_version-linux-x64.tar.gz"

python3 - "$sdk_archive" <<'PY'
import hashlib
import sys

expected = "51c8b999af9e8dd9998c9edc5944e19a90788862068acd38694e098889054ce8c23d4f0c5cccfa16bf187d044562359e5ee69a9f8ad0bbe913ba90311fbce25b"
digest = hashlib.sha512()
with open(sys.argv[1], "rb") as archive:
    for block in iter(lambda: archive.read(1024 * 1024), b""):
        digest.update(block)
if digest.hexdigest() != expected:
    raise SystemExit("SDK SHA512 verification failed; refusing extraction")
print("SDK SHA512 verified")
PY

tar -xzf "$sdk_archive" -C "$sdk_root"
"$sdk_root/dotnet" --version
