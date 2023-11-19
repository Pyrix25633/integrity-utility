#!/bin/bash
dotnet publish -c debug -p:Version=$1
rm -r /transfer/docker/debug/*
cp -r /app/bin/debug/net8.0/* /transfer/docker/debug