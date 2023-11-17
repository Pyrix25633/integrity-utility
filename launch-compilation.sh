#!/bin/bash
chmod -x /transfer/local/*.sh
source /transfer/local/options.sh

if [ $DEBUG -eq 1 ]
then
    /transfer/local/compile-debug.sh $VERSION
fi
if [ $RELEASE -eq 1 ]
then
    /transfer/local/compile-release.sh $VERSION
fi