using System;
using System.IO;
using System.Text;
using System.Threading;

/// <summary>
/// SA:MP cmd .NET namespace
/// </summary>
namespace SAMPcmdNET
{
    /// <summary>
    /// Program class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Exe directory
        /// </summary>
        public static string ExeDir
        {
            get
            {
                return Directory.GetCurrentDirectory();
            }
        }

        /// <summary>
        /// GTA San Andread Exe
        /// </summary>
        public static string GTASAExePath
        {
            get
            {
                return ExeDir + "\\gta_sa.exe";
            }
        }

        /// <summary>
        /// SA:MP DLL path
        /// </summary>
        public static string SAMPDLLPath
        {
            get
            {
                return ExeDir + "\\samp.dll";
            }
        }

        /// <summary>
        /// Is SA:MP available
        /// </summary>
        public static bool IsSAMPAvailable
        {
            get
            {
                return (File.Exists(GTASAExePath) && File.Exists(SAMPDLLPath));
            }
        }

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args">Arguments</param>
        static void Main(string[] args)
        {
            if (IsSAMPAvailable)
            {
                StringBuilder arguments = new StringBuilder();
                bool first = true;
                foreach (string arg in args)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        arguments.Append(" ");
                    }
                    arguments.Append(arg);
                }
                IntPtr mh = Kernel32.GetModuleHandle("kernel32.dll");
                if (mh != IntPtr.Zero)
                {
                    IntPtr pa = Kernel32.GetProcAddress(mh, "LoadLibraryW");
                    if (pa != IntPtr.Zero)
                    {
                        Kernel32.PROCESS_INFORMATION process_info;
                        Kernel32.STARTUPINFO startup_info = new Kernel32.STARTUPINFO();
                        if (Kernel32.CreateProcess(GTASAExePath, arguments.ToString(), IntPtr.Zero, IntPtr.Zero, false, /* DETACHED_PROCESS */ 0x8 | /* CREATE_SUSPENDED */ 0x4, IntPtr.Zero, ExeDir, ref startup_info, out process_info))
                        {
                            IntPtr ptr = Kernel32.VirtualAllocEx(process_info.hProcess, IntPtr.Zero, (uint)(SAMPDLLPath.Length + 1) * 2U, Kernel32.AllocationType.Reserve | Kernel32.AllocationType.Commit, Kernel32.MemoryProtection.ReadWrite);
                            if (ptr != IntPtr.Zero)
                            {
                                int nobw = 0;
                                byte[] p = Encoding.Unicode.GetBytes(SAMPDLLPath);
                                byte[] nt = Encoding.Unicode.GetBytes("\0");
                                if (Kernel32.WriteProcessMemory(process_info.hProcess, ptr, p, (uint)(p.Length), out nobw) && Kernel32.WriteProcessMemory(process_info.hProcess, new IntPtr(ptr.ToInt64() + p.LongLength), nt, (uint)(nt.Length), out nobw))
                                {
                                    uint tid = 0U;
                                    IntPtr rt = Kernel32.CreateRemoteThread(process_info.hProcess, IntPtr.Zero, 0U, pa, ptr, /* CREATE_SUSPENDED */ 0x4, out tid);
                                    if (rt != IntPtr.Zero)
                                    {
                                        Kernel32.ResumeThread(rt);
                                        unchecked
                                        {
                                            Console.WriteLine("SA:MP module injected. Waiting process to finish...");
                                            Kernel32.WaitForSingleObject(rt, (uint)(Timeout.Infinite));
                                        }
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine("Failed to create remote thread with \"CreateRemoteThread\".");
                                    }
                                }
                                else
                                {
                                    Console.Error.WriteLine("Failed to write into process memory with \"WriteProcessMemory\".");
                                }
                                Kernel32.VirtualFreeEx(process_info.hProcess, ptr, 0, Kernel32.AllocationType.Release);
                            }
                            else
                            {
                                Console.Error.WriteLine("Failed to allocate memory with \"VirtualAllocEx\".");
                            }
                            Kernel32.ResumeThread(process_info.hThread);
                            Kernel32.CloseHandle(process_info.hProcess);
                        }
                        else
                        {
                            Console.Error.WriteLine("Failed to create process \"" + GTASAExePath + "\"");
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("Function LoadLibraryW not found.");
                    }
                }
                else
                {
                    Console.Error.WriteLine("Module kernel32.dll not found.");
                }
            }
            else
            {
                Console.Error.WriteLine("SA:MP is not installed on \"" + ExeDir + "\".");
            }
        }
    }
}
