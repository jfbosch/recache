@echo off
NuGet.exe pack ResQ.Sodium.Engine.csproj -IncludeReferencedProjects -Properties OutDir="bin\release"

nuget push *.nupkg -Source %ResQNugetFeed%
del *.nupkg

