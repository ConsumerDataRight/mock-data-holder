wt --maximized ^
--title Gateway_MTLS -d CDR.DataHolder.API.Gateway.mTLS dotnet run; ^
--title IdentityServer -d CDR.DataHolder.IdentityServer dotnet run; ^
--title Resource_API -d CDR.DataHolder.Resource.API dotnet run; ^
--title Admin_API -d CDR.DataHolder.Admin.API dotnet run; ^
--title Public_API -d CDR.DataHolder.Public.API dotnet run; ^
--title Manage_API -d CDR.DataHolder.Manage.API dotnet run
