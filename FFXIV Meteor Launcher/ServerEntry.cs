using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Meteor_Launcher
{
    public class ServerEntry
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string LoginUrl { get; set; }
        public string ThemeUrl { get; set; }

        public ServerEntry()
        {
            Name = "";
            Address = "";
            LoginUrl = "";
            ThemeUrl = "";
        }

        public ServerEntry(string name, string address, string loginUrl, string themeUrl)
        {
            Name = name;
            Address = address;
            LoginUrl = loginUrl;
            ThemeUrl = themeUrl;
        }
    }
}
