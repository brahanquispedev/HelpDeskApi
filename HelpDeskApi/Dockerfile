FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY HelpDesk.Domain/HelpDesk.Domain.csproj HelpDesk.Domain/
COPY HelpDesk.Application/HelpDesk.Application.csproj HelpDesk.Application/
COPY HelpDesk.Infrastructure/HelpDesk.Infrastructure.csproj HelpDesk.Infrastructure/
COPY HelpDeskApi/HelpDeskApi.csproj HelpDeskApi/
RUN dotnet restore HelpDeskApi/HelpDeskApi.csproj

COPY . .
RUN dotnet publish HelpDeskApi/HelpDeskApi.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

USER app
ENTRYPOINT ["dotnet", "HelpDeskApi.dll"]
