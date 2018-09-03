# Leptjson

This is a remake version of [Milo Yip's json-tutorial](https://github.com/miloyip/json-tutorial), written in C#, unit tested by [xUnit](https://xunit.github.io/).

## Requirements

* [.NET Core SDK](https://dotnet.github.io/)
* [Internet Connection](https://www.baidu.com/)

## Build

```batch
#dotnet restore
dotnet test # [bug]This will test Source.
```

## Process of Establishing Folders

~~This part records my process of establishing folders~~ actually I new them manually. If you only want to build from source, no need to do this.

```powershell
dotnet new sln --name LeptJSON;
#dotnet new sln --name UnitTest;

md ./Tutorial01/Source;
md ./Tutorial01/UnitTest;
dotnet new classlib --output ./Tutorial01/Source;
dotnet new xunit --output ./Tutorial01/UnitTest;
dotnet add ./Tutorial01/UnitTest/UnitTest.csproj reference ./Tutorial01/Source/Source.csproj;
dotnet sln Leptjson.sln add ./Tutorial01/Source/Source.csproj ./Tutorial01/UnitTest/UnitTest.csproj;

2..8 | % {
    md ./Tutorial0$_; # If destination folder doesn't exists, PS will copy the first folder's content and then copy other folders themselves.
    Get-ChildItem ./Tutorial01 | Copy-Item -Destination "./Tutorial0$_" -Recurse;
    dotnet sln Leptjson.sln add ./Tutorial0$_/Source/Source.csproj ./Tutorial0$_/UnitTest/UnitTest.csproj;
};
```

## TODO

* [ ] Use my own specific Exception.
