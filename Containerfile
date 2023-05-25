FROM mcr.microsoft.com/dotnet/sdk:8.0-preview AS build-env
WORKDIR /app

COPY ./src/* ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0-preview
WORKDIR /app
COPY --from=build-env /app/out .
VOLUME /app/config

ENTRYPOINT ["dotnet", "mstg.dll"]