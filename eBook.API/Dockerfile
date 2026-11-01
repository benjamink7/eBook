FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY eBook.Model/eBook.Model.csproj eBook.Model/
COPY eBook.Services/eBook.Services.csproj eBook.Services/
COPY eBook.API/eBook.API.csproj eBook.API/
RUN dotnet restore eBook.API/eBook.API.csproj

COPY . .
RUN dotnet publish eBook.API/eBook.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "eBook.API.dll"]
