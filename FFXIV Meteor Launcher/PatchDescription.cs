using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Meteor_Launcher
{
    public class PatchDescription
    {
        public string Path { get; set; }
        public long Size { get; set; }
        public long CRC32 { get; set; }

        public PatchDescription(string path, long size, long crc32)
        {
            this.Path = path;
            this.Size = size;
            this.CRC32 = crc32;
        }
    }
}
