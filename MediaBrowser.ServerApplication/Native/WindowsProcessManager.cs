using MediaBrowser.Controller.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaBrowser.ServerApplication.Native
{
    public class WindowsProcessManager : IProcessManager
    {
        public void SuspendProcess(Process process)
        {
            process.Suspend();
        }

        public void ResumeProcess(Process process)
        {
            process.Resume();
        }

        public bool SupportsSuspension
        {
            get { return true; }
        }
    }

    public static class ProcessExtension
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        public static void Suspend(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }
        public static void Resume(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                ResumeThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }
        public static void Print(this Process process)
        {
            Console.WriteLine("{0,8}    {1}", process.Id, process.ProcessName);
        }
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
}
