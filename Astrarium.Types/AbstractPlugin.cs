﻿using Astrarium.Config;
using Astrarium.Renderers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Astrarium.Types
{
    public abstract class AbstractPlugin : PropertyChangedBase
    {
        private List<SettingItem> settingItems = new List<SettingItem>();
        private List<ToolbarButtonBase> toolbarItems = new List<ToolbarButtonBase>();
        private List<MenuItem> contextMenuItems = new List<MenuItem>();
        private List<MenuItem> mainMenuItems = new List<MenuItem>();

        public IEnumerable<SettingItem> SettingItems => settingItems;
        public IEnumerable<ToolbarButtonBase> ToolbarItems => toolbarItems;
        public IEnumerable<MenuItem> ContextMenuItems => contextMenuItems;
        public IEnumerable<MenuItem> MainMenuItems => mainMenuItems;

        public static IEnumerable<Type> Renderers(Type pluginType)
        {            
            return pluginType.Assembly.GetTypes().Where(t => typeof(BaseRenderer).IsAssignableFrom(t) && !t.IsAbstract);
        }

        public static IEnumerable<Type> Calculators(Type pluginType)
        {
            return pluginType.Assembly.GetTypes().Where(t => typeof(BaseCalc).IsAssignableFrom(t) && !t.IsAbstract);
        }

        public static IEnumerable<Type> AstroEventProviders(Type pluginType)
        {
            return pluginType.Assembly.GetTypes().Where(t => typeof(BaseAstroEventsProvider).IsAssignableFrom(t) && !t.IsAbstract);
        }

        protected void AddSetting(SettingItem setting)
        {
            settingItems.Add(setting);
        }

        protected void AddToolbarItem(ToolbarButtonBase button)
        {
            toolbarItems.Add(button);
        }

        protected void AddContextMenuItem(MenuItem item)
        {
            contextMenuItems.Add(item);
        }

        protected void AddMainMenuItem(MenuItem item)
        {
            mainMenuItems.Add(item);
        }

        protected void ExportResourceDictionaries(params string[] names)
        {
            string assemblyName = GetType().Assembly.FullName;
            foreach (string name in names)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"/{assemblyName};component/{name}", UriKind.Relative) });
            }
        }
    }
}