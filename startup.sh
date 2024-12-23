#!/bin/bash
cd "${BASH_SOURCE%/*}" || exit
export ASPNETCORE_URLS="http://+:80" 
export ASPNETCORE_ENVIRONMENT="Production"
dotnet CaseRelayAPI.dll
