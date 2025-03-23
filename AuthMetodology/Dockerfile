FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["AuthMetodology/AuthMetodology.API.csproj", "AuthMetodology/"]
COPY ["AuthMetodology.Application/AuthMetodology.Application.csproj","AuthMetodology.Application/"]
COPY ["AuthMetodology.Infrastructure/AuthMetodology.Infrastructure.csproj","AuthMetodology.Infrastructure/"]
COPY ["AuthMetodology.Logic/AuthMetodology.Logic.csproj","AuthMetodology.Logic/"]
COPY ["AuthMetodology.Persistence/AuthMetodology.Persistence.csproj","AuthMetodology.Persistence/"]

RUN dotnet restore "AuthMetodology/AuthMetodology.API.csproj"


COPY . .

WORKDIR "/src/AuthMetodology"
RUN dotnet publish -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

EXPOSE 80

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AuthMetodology.API.dll"]