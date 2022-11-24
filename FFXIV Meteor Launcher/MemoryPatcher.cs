using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static FFXIV_Meteor_Launcher.NativeMethods;

namespace FFXIV_Meteor_Launcher
{
    public static class MemoryPatcher
    {
        public static bool ApplyPatch(IntPtr hProcess, IntPtr address, byte[] patchBytes, uint patchSize)
        {
            uint oldProtect = 0;
            
            if (!VirtualProtectEx(hProcess, address, patchSize, (uint)MemoryProtectionFlags.PAGE_READWRITE, out oldProtect))
            {
                throw new Exception("Failed to change protection.");
            }

            int numBytesWritten = 0;
            if (!WriteProcessMemory(hProcess, address, patchBytes, patchSize, out numBytesWritten))
            {
                throw new Exception("Failed to apply patch.");
            }

            if (numBytesWritten != patchSize)
            {
                throw new Exception("Failed to apply patch.");
            }

            if (!VirtualProtectEx(hProcess, address, patchSize, oldProtect, out oldProtect))
            {
                throw new Exception("Failed to restore page protection.");
            }

            return true;
        }

        public static bool ApplyPatches(Process process, IntPtr hThread, string lobbyHostName, uint TickCount)
        {
            var hProcess = OpenProcess(ProcessAccessFlags.All, false, process.Id);
            // Hardcode the base address since process.MainModule.BaseAddress is unable to be used due to .net limitations without dipping into p/invoke solutions
            uint ImageBase = 0x400000;

            const uint g_encryptionTimePatchAddress = 0x9A15E3;
            byte[] g_encryptionTimePatch = { 0xB8, 0x12, 0xE8, 0xE0, 0x50 };
            const uint g_lobbyHostNameAddress = 0xB90110;
            const uint g_lobbyHostNamePatchSize = 0x14;

            bool result = true;

            Debug.WriteLine((ImageBase).ToString("X8"));
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + g_encryptionTimePatchAddress), g_encryptionTimePatch, (uint)g_encryptionTimePatch.Length);

            if ((uint)lobbyHostName.Length + 1 > g_lobbyHostNamePatchSize)
            {
                throw new Exception("Lobby host name too large.");
                return false;
            }

            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + g_lobbyHostNameAddress), Encoding.ASCII.GetBytes(lobbyHostName), (uint)lobbyHostName.Length + 1);

            // Patch out the entire initialization step of GetCurrentProcess > SetProcessAffinityMask > GetCurrentThread > SetThreadAffinityMask
            /* This normally crashes on 16+ core processors due to improper hardcoded values
            //
            // Patched original code:
            // CurrentProcess = GetCurrentProcess();
            // SetProcessAffinityMask(CurrentProcess, 0xFFFFFFFF);
            // CurrentThread = GetCurrentThread();
            // SetThreadAffinityMask(CurrentThread, 0xFFFFFFFF);
            */

            result = ApplyPatch(hProcess, (IntPtr)(0x403698), Enumerable.Repeat<byte>(0x90, 30).ToArray(), (uint)30);


            // VFX Effect Threads are normally limited to the number of cpu threads available, however it is hardcoded to a limit of 32 threads
            // Hitting or exceeding this limit causes an exception and crashes the client.
            // The following two patches remove this 32 thread limitation and allow the client to scale up to as many threads the host has available
            // Patched function (0xBB9500) SQEX::CDev::Engine::Vfx::Qix::Thread::ThreadManager::CreateEffectThread

            // Patch the processor count condition in CreateEffectThread
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + 0xBB952B), new byte[] { 0xB5, 0x1 }, (uint)2);
            // Patch the second version of CreateEffectThread's processor count condition
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + 0xBB95D3), new byte[] { 0xB5, 0x1 }, (uint)2);

            bool ForceFixedTickCountValue = false;
            if (ForceFixedTickCountValue)
            {
                // Write in a new hardcoded TickCount value for decryption
                // 0xB8 [4] 0x90
                var TickBytes = new byte[6] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0x90 };
                var TickCountBytes = BitConverter.GetBytes(TickCount);

                Buffer.BlockCopy(TickCountBytes, 0, TickBytes, 1, 4);
                string TickBytesString = BitConverter.ToString(TickBytes).Replace("-", "");
                uint[] GetTickCountAddresses = { 0x44FBDF, 0x44FA52, 0x44FCF1 };
                foreach (var GetTickCountAddress in GetTickCountAddresses)
                {
                    result = ApplyPatch(hProcess, (IntPtr)(ImageBase + GetTickCountAddress), TickBytes, (uint)TickBytes.Length);
                }
            }

            CloseHandle(hProcess);

            return result;
        }
    }
}
