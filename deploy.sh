#!/bin/sh

echo "Rebuild in Release..."
dotnet build ./xBudget.CeiCrawler/xBudget.CeiCrawler/xBudget.CeiCrawler.csproj -c Release

echo "Packing..."
dotnet pack ./xBudget.CeiCrawler/xBudget.CeiCrawler/xBudget.CeiCrawler.csproj -c Release

echo "Pushing..."
dotnet nuget push ./xBudget.CeiCrawler/xBudget.CeiCrawler/bin/Release/*.nupkg -a "https://nuget.org" -k $NUGET_API_KEY