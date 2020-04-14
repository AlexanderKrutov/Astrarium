﻿using Astrarium.Config;
using Astrarium.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrarium.Plugins.MilkyWay
{
    public class Plugin : AbstractPlugin
    {
        public Plugin()
        {
            #region Settings

            SettingItems.Add("Grids", new SettingItem("MilkyWay", true, "Grids"));
            SettingItems.Add("Colors", new SettingItem("ColorMilkyWay", Color.FromArgb(20, 20, 20), "Colors"));

            #endregion Settings
        }
    }
}
