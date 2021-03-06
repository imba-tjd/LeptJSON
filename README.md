# Leptjson

[![GitHub license](https://img.shields.io/github/license/imba-tjd/LeptJSON.svg)](https://github.com/imba-tjd/LeptJSON/blob/master/LICENSE) [![Build Status](https://www.travis-ci.com/imba-tjd/LeptJSON.svg?branch=master)](https://www.travis-ci.com/imba-tjd/LeptJSON)

This is a remake version of [Milo Yip's json-tutorial](https://github.com/miloyip/json-tutorial), written in C#, unit tested by [xUnit](https://xunit.github.io/), **only for practice**. If you want to know my commit history following the tutorial, see [Tutorial](../../tree/Tutorial) branch.

## Features

* Standard-compliant JSON parser and generator
* Handwritten recursive descent parser
* C# 7.0 or above
* .NET Standard 2.0
* Supports only UTF-8 JSON text
* Supports only storing the JSON `number` type as `double`

## Prerequisites

* [.NET Core SDK](https://dotnet.github.io/)
* [Internet Connection](https://www.baidu.com/)

## Build

```console
#dotnet restore
dotnet build
```

## Test

```console
cd ./UnitTest
dotnet add package Microsoft.NET.Test.Sdk # update nuget packages
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
cd ..
dotnet build
cd ./UnitTest && dotnet test
```

## TODO

* [ ] Use my own specific Exception
* [ ] Unit Test in Tutorial03 to Tutorial04_Answer can't run parallel
* [ ] Dynamic type API
* [ ] Use `Span<T>`
* [x] Use Travis CI
* [ ] [only exist low surrogate](https://github.com/miloyip/json-tutorial/issues/62)
