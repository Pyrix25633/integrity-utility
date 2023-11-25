.RECIPEPREFIX=>
VERSION=1.0.0

default:
> clear
> make build-image
> make run-container
> make run-dotnet-debug

release:
> clear
> make build-image
> make run-container
> make run-dotnet-release

build-image:
> docker build -t integrity-utility:$(VERSION) .

run-container:
> docker run -v $(shell pwd)/transfer:/transfer integrity-utility:$(VERSION)
> chown -R pyrix25633:pyrix25633 ./transfer/docker/*

run-dotnet-debug:
> dotnet ./transfer/docker/debug/integrity-utility.dll -- -p /media/pyrix25633/Seagate\ Basic/Pictures1/2014-09-15 -a SHA3-512 -t 16 -u -l -s -f test.sh

run-dotnet-release:
> ./transfer/docker/release/linux-x64/integrity-utility --help