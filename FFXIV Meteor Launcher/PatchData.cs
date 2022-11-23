using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Meteor_Launcher
{
    public static class PatchData
    {
        public static string PatchDownloadUrl = "http://ffxivpatches.s3.amazonaws.com/";

        public static List<PatchDescription> PatchDataList = new List<PatchDescription>
        {
            { new PatchDescription("2d2a390f/patch/D2010.09.18.0000.patch", 5571687, 0x47DDE5ED) },

            { new PatchDescription("48eca647/patch/D2010.09.19.0000.patch", 444398866, 0xD55C7ACD) },
            { new PatchDescription("48eca647/patch/D2010.09.23.0000.patch", 6907277, 0xCA135D55) },
            { new PatchDescription("48eca647/patch/D2010.09.28.0000.patch", 18803280, 0xB19B32FE) },

            { new PatchDescription("48eca647/patch/D2010.10.07.0001.patch", 19226330, 0xD6118CEE) },
            { new PatchDescription("48eca647/patch/D2010.10.14.0000.patch", 19464329, 0x34BF6A99) },
            { new PatchDescription("48eca647/patch/D2010.10.22.0000.patch", 19778252, 0x2543DB5C) },
            { new PatchDescription("48eca647/patch/D2010.10.26.0000.patch", 19778391, 0x20F94876) },

            { new PatchDescription("48eca647/patch/D2010.11.25.0002.patch", 250718651, 0x5FBB5B24) },
            { new PatchDescription("48eca647/patch/D2010.11.30.0000.patch", 6921623, 0xA5479111) },

            { new PatchDescription("48eca647/patch/D2010.12.06.0000.patch", 7158904, 0xCAD6BC31) },
            { new PatchDescription("48eca647/patch/D2010.12.13.0000.patch", 263311481, 0xE51EFC06) },
            { new PatchDescription("48eca647/patch/D2010.12.21.0000.patch", 7521358, 0x93EE1510) },

            { new PatchDescription("48eca647/patch/D2011.01.18.0000.patch", 9954265, 0x059E8900) },

            { new PatchDescription("48eca647/patch/D2011.02.01.0000.patch", 11632816, 0x9EE60B39) },
            { new PatchDescription("48eca647/patch/D2011.02.10.0000.patch", 11714096, 0x0ADE7243) },

            { new PatchDescription("48eca647/patch/D2011.03.01.0000.patch", 77464101, 0x7818B5BF) },
            { new PatchDescription("48eca647/patch/D2011.03.24.0000.patch", 108923937, 0xF21852AD) },
            { new PatchDescription("48eca647/patch/D2011.03.30.0000.patch", 109010880, 0x84CB2682) },

            { new PatchDescription("48eca647/patch/D2011.04.13.0000.patch", 341603850, 0xFF6C3DB0) },
            { new PatchDescription("48eca647/patch/D2011.04.21.0000.patch", 343579198, 0x57F4041C) },

            { new PatchDescription("48eca647/patch/D2011.05.19.0000.patch", 344239925, 0xB16FF18C) },

            { new PatchDescription("48eca647/patch/D2011.06.10.0000.patch", 344334860, 0xB1CAA88B) },

            { new PatchDescription("48eca647/patch/D2011.07.20.0000.patch", 584926805, 0x2EA149A9) },
            { new PatchDescription("48eca647/patch/D2011.07.26.0000.patch", 7649141, 0x5670BA07) },

            { new PatchDescription("48eca647/patch/D2011.08.05.0000.patch", 152064532, 0x0D9E9FD8) },
            { new PatchDescription("48eca647/patch/D2011.08.09.0000.patch", 8573687, 0x9B54551A) },
            { new PatchDescription("48eca647/patch/D2011.08.16.0000.patch", 6118907, 0x75231C57) },

            { new PatchDescription("48eca647/patch/D2011.10.04.0000.patch", 677633296, 0x95C15318) },
            { new PatchDescription("48eca647/patch/D2011.10.12.0001.patch", 28941655, 0xB37993E3) },
            { new PatchDescription("48eca647/patch/D2011.10.27.0000.patch", 29179764, 0x977480DC) },

            { new PatchDescription("48eca647/patch/D2011.12.14.0000.patch", 374617428, 0xC6FE8FED) },
            { new PatchDescription("48eca647/patch/D2011.12.23.0000.patch", 22363713, 0x93137C93) },

            { new PatchDescription("48eca647/patch/D2012.01.18.0000.patch", 48998794, 0x9E55EC7E) },
            { new PatchDescription("48eca647/patch/D2012.01.24.0000.patch", 49126606, 0x3008D942) },
            { new PatchDescription("48eca647/patch/D2012.01.31.0000.patch", 49536396, 0x60FDBD0B) },

            { new PatchDescription("48eca647/patch/D2012.03.07.0000.patch", 320630782, 0x885AD768) },
            { new PatchDescription("48eca647/patch/D2012.03.09.0000.patch", 8312819, 0xC0040D8C) },
            { new PatchDescription("48eca647/patch/D2012.03.22.0000.patch", 22027738, 0xEABC501B) },
            { new PatchDescription("48eca647/patch/D2012.03.29.0000.patch", 8322920, 0x63811C35) },

            { new PatchDescription("48eca647/patch/D2012.04.04.0000.patch", 8678570, 0xF6E43EEC) },
            { new PatchDescription("48eca647/patch/D2012.04.23.0001.patch", 289511791, 0x6C3C0201) },

            { new PatchDescription("48eca647/patch/D2012.05.08.0000.patch", 27266546, 0xB6AABF18) },
            { new PatchDescription("48eca647/patch/D2012.05.15.0000.patch", 27416023, 0x2D428126) },
            { new PatchDescription("48eca647/patch/D2012.05.22.0000.patch", 27742726, 0x9163549D) },

            { new PatchDescription("48eca647/patch/D2012.06.06.0000.patch", 129984024, 0x21DF7238) },
            { new PatchDescription("48eca647/patch/D2012.06.19.0000.patch", 133434217, 0x8280988A) },
            { new PatchDescription("48eca647/patch/D2012.06.26.0000.patch", 133581048, 0x4CF33FC8) },

            { new PatchDescription("48eca647/patch/D2012.07.21.0000.patch", 253224781, 0xA8A42A32) },

            { new PatchDescription("48eca647/patch/D2012.08.10.0000.patch", 42851112, 0xD8ED4CE3) },

            { new PatchDescription("48eca647/patch/D2012.09.06.0000.patch", 20566711, 0x4235DF72) },
            { new PatchDescription("48eca647/patch/D2012.09.19.0001.patch", 20874726, 0x8A775526) }
        };
    }
}
