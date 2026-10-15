#!/usr/bin/env sh
set -eu
IOT_TEST_CONNECTION="Host=db;Database=iot_tests;Username=iot;Password=$(cat /run/secrets/db-password)"
export IOT_TEST_CONNECTION
exec dotnet test IotGateway.slnx -c Release --no-restore --logger 'trx;LogFileName=docker-tests.trx' --results-directory /test-results
