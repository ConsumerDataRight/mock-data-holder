@echo off
echo Run solutions from .Net CLI using localhost and localdb from appsettings.Development.json
pause

setx ASPNETCORE_ENVIRONMENT Development
setx Industry Banking

dotnet build ..\Shared\CDR.DataHolder.Shared.API.Gateway.mTLS
dotnet build CDR.DataHolder.Banking.Resource.API
dotnet build ..\Shared\CDR.DataHolder.Public.API
dotnet build ..\Shared\CDR.DataHolder.Manage.API
dotnet build ..\Common\CDR.DataHolder.Common.API

wt --maximized ^
--title MDHB_MTLS -d ..\Shared\CDR.DataHolder.Shared.API.Gateway.mTLS dotnet run --no-launch-profile; ^
--title MDHB_Res_API -d CDR.DataHolder.Banking.Resource.API dotnet run --no-launch-profile; ^
--title MDHB_Common_API -d ..\Common\CDR.DataHolder.Common.API dotnet run --no-launch-profile; ^
--title MDHB_Pub_API -d ..\Shared\CDR.DataHolder.Public.API dotnet run --no-launch-profile; ^
--title MDHB_Mgr_API -d ..\Shared\CDR.DataHolder.Manage.API dotnet run --no-launch-profile