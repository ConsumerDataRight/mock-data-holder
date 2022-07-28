<h2>Azure Functions</h2>
<div style="margin-left:18px;">
The Mock Data Holder solution contains an azure function project.<br />
The GetDataRecipients function is used to get the list of Data Recipients<br />
from the Mock Register and update the Mock Data Hoder repository.<br />
</div>

<h2>To Run and Debug Azure Functions</h2>
<div style="margin-left:18px;">
	The following procedures can be used to run the functions in a local development environment for evaluation of the functions.
<br />

<div style="margin-top:6px;margin-bottom:6px;">
1) Start the Mock Register and the Mock Data Holder solutions.
</div>

<div style="margin-top:6px;">
2) Start the Azure Storage Emulator (Azurite):
</div>
<div style="margin-left:18px;margin-bottom:6px;">
	using a MS Windows command prompt:<br />
</div>

```
md C:\azurite
cd "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator"
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

<div style="margin-left:18px;">
	Noting this is only required to be performed once, it will then be listening on ports - 10000, 10001 and 10002<br />
	when debugging is started from MS Visual Studio by selecting CDR.GetDataRecipients as the startup project<br />
	(by starting a debug instance using F5 or Debug > Start Debugging)
	<br />
</div>
<div style="margin-left:18px;margin-bottom:6px;">
	or by using a MS Windows command prompt:<br />
</div>

```
navigate to .\mock-data-holder\Source\CDR.GetDataRecipients<br />
func start --verbose<br />
```

<p>3) Open the Mock Data Holder in MS Visual Studio, select CDR.GetDataRecipients as the startup project.</p>

<p>4) Start each debug instances (F5 or Debug > Start Debugging), this will simulate the discovery of Data Recipients and the</p>
<div style="margin-left:18px;margin-top:-12px;">
	updating of the data in the Mock Data Holder repositories.
</div>

<div style="margin-left:18px;margin-top:12px;margin-bottom:6px;">
	Noting the below sql scripts are used to observe the results.<br />
</div>

```
SELECT * FROM [cdr-mdh].[dbo].[LegalEntity]
SELECT * FROM [cdr-mdh].[dbo].[Brand]
SELECT * FROM [cdr-mdh].[dbo].[SoftwareProduct]
SELECT * FROM [cdr-mdh].[dbo].[LogEvents_DRService]
```

<h2>To Build Azure Functions</h2>
<div style="margin-left:18px;">
	dotnet SDK 6.0.30x or higher is required. Latest SDK can be found from the link https://microsoft.com/net
<br />