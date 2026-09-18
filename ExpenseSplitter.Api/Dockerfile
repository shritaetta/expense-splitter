FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ExpenseSplitter.Api.csproj", "./"]
RUN dotnet restore "ExpenseSplitter.Api.csproj"
COPY . .
RUN dotnet build "ExpenseSplitter.Api.csproj" -c Release -o /app/build
RUN dotnet publish "ExpenseSplitter.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Use a volume for SQLite DB
VOLUME /app/data
ENTRYPOINT ["dotnet", "ExpenseSplitter.Api.dll"]
