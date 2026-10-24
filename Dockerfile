FROM mcr.microsoft.com/dotnet/aspnet:8.0
MAINTAINER cluesofdevops@gmail.com
WORKDIR /app
EXPOSE 8080
COPY dtag-auth-service/publish .
ENTRYPOINT ["dotnet", "AuthorizationService.dll"]
