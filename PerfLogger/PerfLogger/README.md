# NestedPerfLogger.net
A Simple performance logger with support for nested measurements

# Publish nuget package
1. Update nuget package version number in .nuspec
2. ```nuget pack```
3. ```nuget push .\NestedPerfLogger.1.x.x.nupkg -Source https://api.nuget.org/v3/index.json```