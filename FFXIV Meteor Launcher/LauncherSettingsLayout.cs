using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Meteor_Launcher
{
    public class LauncherSettingsLayout
    {
        public string InstallLocation { get; set; }
        public string DefaultServerName { get; set; }

        public LauncherSettingsLayout()
        {
            InstallLocation = "";
            DefaultServerName = "";
        }
    }
}
