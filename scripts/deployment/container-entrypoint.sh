#!/bin/sh
set -eu

if [ "${1:-}" = "migrate" ]; then
    # Factories read the connection string from the environment. Do not put
    # credentials on command lines or print them to deployment logs.
    : "${ConnectionStrings__Workflow:?A migration connection secret is required}"
    for module in Properties Claims Documents Communications Playbooks Workflow; do
        echo "Applying $module migrations"
        "/app/migrations/$module"
    done
else
    exec dotnet /app/PropertyIntelligence.Api.dll "$@"
fi
