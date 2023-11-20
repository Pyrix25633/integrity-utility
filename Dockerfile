FROM mcr.microsoft.com/dotnet/sdk:8.0

RUN apt-get -y update
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY *.cs ./
COPY launch-compilation.sh ./
RUN chmod +x /app/launch-compilation.sh

CMD [ "/app/launch-compilation.sh" ]