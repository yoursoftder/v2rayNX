﻿using System;
using System.Windows.Forms;
using vNet.Mode;

namespace vNet.Forms
{
    public partial class BaseForm : Form
    {
        protected static Config config;
        protected static System.Drawing.Icon icon;

        public BaseForm()
        {
            InitializeComponent();
            LoadCustomIcon();
        }

        private void LoadCustomIcon()
        {
            try
            {
                if (icon == null)
                {
                    string file = Utils.GetPath(Global.CustomIconName);
                    if (!System.IO.File.Exists(file))
                    {
                        return;
                    }
                    icon = new System.Drawing.Icon(file);
                }
                this.Icon = icon;
            }
            catch (Exception e)
            {
                Utils.SaveLog($"Loading custom icon failed: {e.Message}");
            }
        }

    }
}
