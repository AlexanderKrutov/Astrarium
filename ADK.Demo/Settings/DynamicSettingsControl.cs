﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ADK.Demo.Settings
{
    public partial class DynamicSettingsControl : UserControl
    {
        private ISettings settings;
        public ISettings Settings
        {
            get { return settings; }
            set
            {
                if (settings != null)
                {
                    settings.OnSettingValueChanged -= UpdateDependentControls;
                }
                settings = value;
                settings.OnSettingValueChanged += UpdateDependentControls;
                CreateControls();
            }
        }

        private Dictionary<string, Panel> SettingsPanels = new Dictionary<string, Panel>();
        private ICollection<Control> SettingsControls = new List<Control>();

        public DynamicSettingsControl()
        {
            InitializeComponent();
        }

        private void CreateControls()
        {
            foreach (var section in Settings.Tree.Sections)
            {
                listSections.Items.Add(section.Title ?? section.Name);
                FlowLayoutPanel panel = new FlowLayoutPanel();
                panel.FlowDirection = FlowDirection.TopDown;
                panel.Dock = DockStyle.Fill;
                panel.Visible = false;
                foreach (var setting in section.Settings)
                {
                    panel.Controls.Add(BuildSettingControl(setting));
                }
                splitContainer.Panel2.Controls.Add(panel);
                SettingsPanels[section.Name] = panel;
            }

            var masterSettings = Settings.All.Select(s => s.DependsOn).Distinct().Where(s => !string.IsNullOrWhiteSpace(s));
            foreach (var master in masterSettings)
            {
                UpdateDependentControls(master);
            }

            listSections.SelectedIndex = 0;
        }

        private void UpdateDependentControls(string masterSettingName)
        {
            var masterValue = Settings.Get<object>(masterSettingName);
            var dependentNodes = Settings.All.Where(n => n.DependsOn == masterSettingName);
            
            foreach (var n in dependentNodes)
            {
                var control = SettingsControls.FirstOrDefault(c => c.Name == n.Name);
                if (control != null)
                {
                    if (!string.IsNullOrEmpty(n.EnabledIf))
                    {                        
                        control.Enabled = masterValue.Equals(n.ValueFromString(n.EnabledIf));
                    }

                    if (!string.IsNullOrEmpty(n.VisibleIf))
                    {
                        control.Visible = masterValue.Equals(n.ValueFromString(n.VisibleIf));
                    }
                }
            }
        }

        private Control BuildSettingControl(SettingNode setting)
        {
            Control control = null;

            string controlType = setting.Control ?? setting.DefaultControl;

            switch (controlType)
            {
                case "checkbox":
                    CheckBox checkBox = new CheckBox();
                    checkBox.Text = setting.Title;
                    checkBox.Checked = Settings.Get<bool>(setting.Name);
                    checkBox.AutoSize = true;
                    checkBox.Name = setting.Name;
                    checkBox.UseVisualStyleBackColor = true;
                    checkBox.CheckedChanged += (s, e) => Settings.Set(setting.Name, checkBox.Checked);
                    control = checkBox;
                    break;
                case "colorpicker":
                    ColorPicker picker = new ColorPicker();
                    picker.Text = setting.Title;
                    picker.SelectedValue = Settings.Get<Color>(setting.Name);
                    picker.AutoSize = true;
                    picker.Name = setting.Name;
                    picker.SelectedValueChanged += (s, e) => Settings.Set(setting.Name, picker.SelectedValue);
                    control = picker;
                    break;
                default:
                    break;
            }

            if (control == null)
            {
                Label label = new Label();
                label.Name = setting.Name;
                label.Text = $"{setting.Name}: {controlType} not implemented.";
                label.ForeColor = Color.DarkRed;
                label.AutoSize = true;
                control = label;
            }

            SettingsControls.Add(control);
            return control;
        }

        private void listSections_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listSections.SelectedIndex == -1)
            {
                listSections.SelectedIndex = 0;
            }
            else
            {
                var section = Settings.Tree.Sections.ElementAt(listSections.SelectedIndex);
                var panel = SettingsPanels[section.Name];
                foreach (var p in SettingsPanels.Values)
                {
                    p.Visible = p == panel;
                }
            }
        }
    }
}