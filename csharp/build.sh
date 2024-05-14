#! /usr/bin/env bash
set -uvx
set -e
cd "$(dirname "$0")"
cwd=`pwd`
ts=`date "+%Y.%m%d.%H%M.%S"`
version="${ts}"

cd $cwd
find . -name bin -exec rm -rf {} +
find . -name obj -exec rm -rf {} +
#find . -name packages -exec rm -rf {} +
rm -rf x??
dotnet restore -p:Configuration=Release -p:Platform="Any CPU" MacAddress.sln
msbuild.exe MacAddress.sln -p:Configuration=Release -p:Platform="Any CPU"
cp -rp bin/Release/net462/x?? .
