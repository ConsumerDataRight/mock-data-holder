@echo off
echo Run solutions from .Net CLI using localhost and localdb from appsettings.Development.json
pause

setx ASPNETCORE_ENVIRONMENT Development
setx Industry Energy

dotnet build ..\Shared\CDR.DataHolder.Shared.API.Gateway.mTLS
dotnet build CDR.DataHolder.Energy.Resource.API
dotnet build ..\Shared\CDR.DataHolder.Public.API
dotnet build ..\Shared\CDR.DataHolder.Manage.API
dotnet build ..\Common\CDR.DataHolder.Common.API

wt --maximized ^
--title MDHE_MTLS -d ..\Shared\CDR.DataHolder.Shared.API.Gateway.mTLS dotnet run --no-launch-profile; ^
--title MDHE_Res_API -d CDR.DataHolder.Energy.Resource.API dotnet run --no-launch-profile; ^
--title MDHE_Common_API -d ..\Common\CDR.DataHolder.Common.API dotnet run --no-launch-profile; ^
--title MDHE_Pub_API -d ..\Shared\CDR.DataHolder.Public.API dotnet run --no-launch-profile; ^
--title MDHE_Mgr_API -d ..\Shared\CDR.DataHolder.Manage.API dotnet run --no-launch-profile