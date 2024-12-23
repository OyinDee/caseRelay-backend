#!/bin/bash
cd "${BASH_SOURCE%/*}" || exit
export ASPNETCORE_URLS="http://+:80"  # Standard HTTP port
export ASPNETCORE_ENVIRONMENT="Production"
dotnet CaseRelayAPI.dll
