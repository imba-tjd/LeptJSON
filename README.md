# Leptjson

This is a remake version of [Milo Yip's json-tutorial](https://github.com/miloyip/json-tutorial), written in C#, unit tested by [xUnit](https://xunit.github.io/).

## Build

```
#dotnet restore
dotnet test # [bug]This will test Source.
```

## Process of Establishing Folders

This part records my process of establishing folders. If you only want to build from source, no need to do this.

```powershell
dotnet new sln --name LeptJSON;
#dotnet new sln --name UnitTest;
1..8 | % {
    md ./Tutorial0$_/Source
    md ./Tutorial0$_/UnitTest;
    dotnet new classlib --output ./Tutorial0$_/Source;
    dotnet new xunit --output ./Tutorial0$_/UnitTest;
    dotnet add ./Tutorial0$_/UnitTest/UnitTest.csproj reference ./Tutorial0$_/Source/Source.csproj;
    dotnet sln Leptjson add ./Tutorial0$_/Source/Source.csproj ./Tutorial0$_/UnitTest/UnitTest.csproj;
};
```
