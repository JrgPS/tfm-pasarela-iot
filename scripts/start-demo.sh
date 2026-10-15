#!/usr/bin/env sh
set -eu
cd "$(dirname "$0")/.."
mkdir -p secrets results
docker compose run --rm --no-deps --build provision prepare --count "${DEVICES:-1000}"
docker compose up -d --wait db
docker compose run --rm --no-deps provision seed
docker compose up -d --build --scale "gateway=${GATEWAYS:-1}" --scale "registry=${REGISTRIES:-1}" gateway registry proxy
echo 'Services launched. Verify readiness in logs, then run the simulator at localhost:7443.'
