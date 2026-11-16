FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY eBook.Model/eBook.Model.csproj eBook.Model/
COPY eBook.Worker/eBook.Worker.csproj eBook.Worker/
RUN dotnet restore eBook.Worker/eBook.Worker.csproj

COPY . .
RUN dotnet publish eBook.Worker/eBook.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "eBook.Worker.dll"]
