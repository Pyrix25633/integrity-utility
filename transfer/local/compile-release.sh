#!/bin/bash
dotnet publish -c release -r linux-x64 -p:Version=$1 -p:PublishSigleFile=true --self-contained true -p:PublishReadyToRun=true
dotnet publish -c release -r win-x64 -p:Version=$1 -p:PublishSigleFile=true --self-contained true -p:PublishReadyToRun=true
rm -r -v /transfer/docker/release/*
mkdir /transfer/docker/release/linux-x64
mkdir /transfer/docker/release/win-x64
cp -r -v /app/bin/release/net8.0/linux-x64/publish/* /transfer/docker/release/linux-x64
cp -r -v /app/bin/release/net8.0/win-x64/publish/* /transfer/docker/release/win-x64
cd /transfer/docker/release/linux-x64
tar -vczf integrity-utility-$1-linux-x64.tar.gz *
cd /transfer/docker/release/win-x64
zip integrity-utility-$1-win-x64.zip *