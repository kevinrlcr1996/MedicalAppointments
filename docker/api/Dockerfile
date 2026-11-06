FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props MedicalAppointments.slnx ./
COPY src/ src/
COPY tests/ tests/

RUN dotnet restore MedicalAppointments.slnx
RUN dotnet publish src/MedicalAppointments.Api/MedicalAppointments.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5001

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MedicalAppointments.Api.dll"]