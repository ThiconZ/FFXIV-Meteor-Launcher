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
            // This normally crashes on 16+ core processors due to improper hardcoded values
            result = ApplyPatch(hProcess, (IntPtr)(0x403698), Enumerable.Repeat<byte>(0x90, 30).ToArray(), (uint)30);

            // Patch the processor count condition in CreateEffectThread
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + 0xBB952B), new byte[] { 0xB5, 0x1 }, (uint)2);
            // Patch the second version of CreateEffectThread's processor count condition
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + 0xBB95D3), new byte[] { 0xB5, 0x1 }, (uint)2);

            /*
            // Patch out Processor Affinity Mask
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + 0x403698), new byte[] { 0x90, 0x90 }, (uint)2);

            // Patch out SetProcessorAffinity call
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + 0x4036A0), new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }, (uint)7);

            // Patch out Thread Affinity Mask
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + 0x4036A7), new byte[] { 0x90, 0x90 }, (uint)2);

            // Patch out SetThreadAffinity call
            result = ApplyPatch(hProcess, (IntPtr)(ImageBase + 0x4036AF), new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }, (uint)7);
            */

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
