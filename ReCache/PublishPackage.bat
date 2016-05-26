@echo off
NuGet.exe pack ReCache.csproj -IncludeReferencedProjects -Properties OutDir="bin\release"

rem nuget push *.nupkg -Source %ResQNugetFeed%
rem del *.nupkg

