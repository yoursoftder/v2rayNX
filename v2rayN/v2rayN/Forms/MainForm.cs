using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using vNet.Handler;
using vNet.HttpProxyHandler;
using vNet.Mode;
using vNet.Base;
using vNet.Tool;
using System.Diagnostics;
using vNet.Properties;
using Newtonsoft.Json;
using System.Threading;

namespace vNet.Forms
{
    public partial class MainForm : BaseForm
    {
        private VnetHandler v2rayHandler;
        private List<int> lvSelecteds = new List<int>();
        private StatisticsHandler statistics = null;
        string[] argse;//启动参数
        #region Window 事件

        public MainForm(string[] argse)
        {
            this.argse = argse;
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            HideForm();
            this.Text = Utils.GetVersion();
            Global.processJob = new Job();

            Application.ApplicationExit += (sender, args) =>
            {
                v2rayHandler.VnetStop();

                HttpProxyHandle.CloseHttpAgent(config);
                PACServerHandle.Stop();

                ConfigHandler.SaveConfig(ref config);
                statistics?.SaveToFile();
                statistics?.Close();
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (argse.Length > 0)
            {
                if (argse[0].Contains("hide"))
                {
                  
                   // this.Icon = null;
                }
            }
                    SafeCheck();
            ConfigHandler.LoadConfig(ref config);
            v2rayHandler = new VnetHandler();
            v2rayHandler.ProcessEvent += v2rayHandler_ProcessEvent;

            if (config.enableStatistics)
            {
                statistics = new StatisticsHandler(config, UpdateStatisticsHandler);
            }

        }
        internal void SafeCheck()
        {
            if(Passtxt.Text == "327")
            {
                tsMain.Visible = true;
                button1.Visible = true;
                repinglb.Visible = true;
                groupBox1.Visible = true;
                groupBox2.Visible = true;
                ssMain.Visible = true;
            }
            else
            {
                button1.Visible = false;
                tsMain.Visible = false;
                repinglb.Visible = false;
                groupBox1.Visible = false;
                groupBox2.Visible = false;
                ssMain.Visible = false;
            }
        }

        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            if (statistics == null || !statistics.Enable) return;
            if ((sender as Form).Visible)
            {
                statistics.UpdateUI = true;
            }
            else
            {
                statistics.UpdateUI = false;
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            InitServersView();
            RefreshServers();
            lvServers.AutoResizeColumns();

            LoadV2ray();

            HideForm();

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideForm();
                return;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            //if (this.WindowState == FormWindowState.Minimized)
            //{
            //    HideForm();
            //}
            //else
            //{
            //    //this.splitContainer1.SplitterDistance = config.uiItem.mainQRCodeWidth;
            //}
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            //config.uiItem.mainQRCodeWidth = splitContainer1.SplitterDistance;
        }

        //private const int WM_QUERYENDSESSION = 0x0011;
        //protected override void WndProc(ref Message m)
        //{
        //    switch (m.Msg)
        //    {
        //        case WM_QUERYENDSESSION:
        //            Utils.SaveLog("Windows shutdown UnsetProxy");

        //            ConfigHandler.ToJsonFile(config);
        //            statistics?.SaveToFile();
        //            ProxySetting.UnsetProxy();
        //            m.Result = (IntPtr)1;
        //            break;
        //        default:
        //            base.WndProc(ref m);
        //            break;
        //    }
        //}
        #endregion

        #region 显示服务器 listview 和 menu

        /// <summary>
        /// 刷新服务器
        /// </summary>
        private void RefreshServers()
        {
            RefreshServersView();
            RefreshServersMenu();
        }

        /// <summary>
        /// 初始化服务器列表
        /// </summary>
        private void InitServersView()
        {
            lvServers.Items.Clear();

            lvServers.GridLines = true;
            lvServers.FullRowSelect = true;
            lvServers.View = View.Details;
            lvServers.Scrollable = true;
            lvServers.MultiSelect = true;
            lvServers.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            lvServers.Columns.Add("", 15, HorizontalAlignment.Center);
            lvServers.Columns.Add(UIRes.I18N("LvServiceType"), 20, HorizontalAlignment.Left);
            lvServers.Columns.Add(UIRes.I18N("LvAlias"), 50, HorizontalAlignment.Left);
            lvServers.Columns.Add(UIRes.I18N("LvAddress"), 50, HorizontalAlignment.Left);
            lvServers.Columns.Add(UIRes.I18N("LvPort"), 50, HorizontalAlignment.Left);
            lvServers.Columns.Add(UIRes.I18N("LvEncryptionMethod"), 50, HorizontalAlignment.Left);
            lvServers.Columns.Add(UIRes.I18N("LvTransportProtocol"), 20, HorizontalAlignment.Left);
            lvServers.Columns.Add(UIRes.I18N("LvSubscription"), 20, HorizontalAlignment.Left);
            lvServers.Columns.Add(UIRes.I18N("LvTestResults"), 50, HorizontalAlignment.Left);

            if (statistics != null && statistics.Enable)
            {
                lvServers.Columns.Add(UIRes.I18N("LvTotalUploadDataAmount"), 30, HorizontalAlignment.Left);
                lvServers.Columns.Add(UIRes.I18N("LvTotalDownloadDataAmount"), 30, HorizontalAlignment.Left);
                lvServers.Columns.Add(UIRes.I18N("LvTodayUploadDataAmount"), 30, HorizontalAlignment.Left);
                lvServers.Columns.Add(UIRes.I18N("LvTodayDownloadDataAmount"), 30, HorizontalAlignment.Left);
            }
        }

        /// <summary>
        /// 刷新服务器列表
        /// </summary>
        private void RefreshServersView()
        {
            lvServers.Items.Clear();

            for (int k = 0; k < config.vmess.Count; k++)
            {
                string def = string.Empty;
                string totalUp = string.Empty,
                        totalDown = string.Empty,
                        todayUp = string.Empty,
                        todayDown = string.Empty;
                if (config.index.Equals(k))
                {
                    def = "√";
                }

                VmessItem item = config.vmess[k];

                ListViewItem lvItem = null;
                if (statistics != null && statistics.Enable)
                {
                    var index = statistics.Statistic.FindIndex(item_ => item_.itemId == item.getItemId());
                    if (index != -1)
                    {
                        totalUp = Utils.HumanFy(statistics.Statistic[index].totalUp);
                        totalDown = Utils.HumanFy(statistics.Statistic[index].totalDown);
                        todayUp = Utils.HumanFy(statistics.Statistic[index].todayUp);
                        todayDown = Utils.HumanFy(statistics.Statistic[index].todayDown);
                    }

                    lvItem = new ListViewItem(new string[]
                    {
                    def,
                    ((EConfigType)item.configType).ToString(),
                    item.remarks,
                    item.address,
                    item.port.ToString(),
                    //item.id,
                    //item.alterId.ToString(),
                    item.security,
                    item.network,
                    item.getSubRemarks(config),
                    item.testResult,
                    totalUp,
                    totalDown,
                    todayUp,
                    todayDown
                    });
                }
                else
                {
                    lvItem = new ListViewItem(new string[]
                   {
                    def,
                    ((EConfigType)item.configType).ToString(),
                    item.remarks,
                    item.address,
                    item.port.ToString(),
                    //item.id,
                    //item.alterId.ToString(),
                    item.security,
                    item.network,
                    item.getSubRemarks(config),
                    item.testResult
                    //totalUp,
                    //totalDown,
                    //todayUp,
                    //todayDown,
                   });
                }

                if (lvItem != null) lvServers.Items.Add(lvItem);
            }

            //if (lvServers.Items.Count > 0)
            //{
            //    if (lvServers.Items.Count <= testConfigIndex)
            //    {
            //        testConfigIndex = lvServers.Items.Count - 1;
            //    }
            //    lvServers.Items[testConfigIndex].Selected = true;
            //    lvServers.Select();
            //}
        }

        /// <summary>
        /// 刷新托盘服务器菜单
        /// </summary>
        private void RefreshServersMenu()
        {
            menuServers.DropDownItems.Clear();

            List<ToolStripMenuItem> lst = new List<ToolStripMenuItem>();
            for (int k = 0; k < config.vmess.Count; k++)
            {
                VmessItem item = config.vmess[k];
                string name = item.getSummary();

                ToolStripMenuItem ts = new ToolStripMenuItem(name);
                ts.Tag = k;
                if (config.index.Equals(k))
                {
                    ts.Checked = true;
                }
                ts.Click += new EventHandler(ts_Click);
                lst.Add(ts);
            }
            menuServers.DropDownItems.AddRange(lst.ToArray());
        }

        private void ts_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripItem ts = (ToolStripItem)sender;
                int index = Utils.ToInt(ts.Tag);
                SetDefaultServer(index);
            }
            catch
            {
            }
        }

        private void lvServers_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = -1;
            try
            {
                if (lvServers.SelectedIndices.Count > 0)
                {
                    index = lvServers.SelectedIndices[0];
                }
            }
            catch
            {
            }
            if (index < 0)
            {
                return;
            }
            //qrCodeControl.showQRCode(index, config);
        }

        private void DisplayToolStatus()
        {
            if (argse.Length > 0)
            {
                if (argse[0] == "hide")
                {
                    notifyMain = null;

                    notifyMain.Dispose();
                    notifyMain.Visible = false;
                }
            }
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory.ToString() + "/app.ini"))//不存在则创建文件
            {
                
            }
            else
            {
                string[] content = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory.ToString() + "/app.ini").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);//准备读取文件
             string   vmessport = content[0].Split(':')[1];
                string httpport = content[1].Split(':')[1];
             //   string runpac = content[2].Split(':')[1];
                // System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory.ToString() + "/app.ini", string.Join("\r\n", content));//将内容换行保存在本地TXT中开始
            if(!toolSslBlank2.Text.Contains("/" + httpport + "v" + vmessport))
            toolSslBlank2.Text = toolSslBlank2.Text + "/" + httpport+"v" + vmessport;
            }
            toolSslSocksPort.Text =
            toolSslHttpPort.Text =
            toolSslPacPort.Text = "NONE";

            toolSslSocksPort.Text = $"{Global.Loopback}:{config.inbound[0].localPort}";

            if (config.listenerType != 0)
            {
                toolSslHttpPort.Text = $"{Global.Loopback}:{Global.httpPort}";
                if (config.listenerType == 2 || config.listenerType == 4)
                {
                    if (PACServerHandle.IsRunning)
                    {
                        toolSslPacPort.Text = $"{HttpProxyHandle.GetPacUrl()}";
                    }
                    else
                    {
                        toolSslPacPort.Text = UIRes.I18N("StartPacFailed");
                    }
                }
            }
            notifyMain.Icon = MainFormHandler.Instance.GetNotifyIcon(config, this.Icon);

            
        }
        private void ssMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (!Utils.IsNullOrEmpty(e.ClickedItem.Text))
            {
                Utils.SetClipboardData(e.ClickedItem.Text);
            }
        }

        #endregion

        #region v2ray 操作

        /// <summary>
        /// 载入V2ray
        /// </summary>
        private void LoadV2ray()
        {
            tsbReload.Enabled = false;

            if (Global.reloadV2ray)
            {
                ClearMsg();
            }
            v2rayHandler.LoadVnet(config);
            Global.reloadV2ray = false;
            ConfigHandler.SaveConfig(ref config, false);
            statistics?.SaveToFile();

            ChangePACButtonStatus(config.listenerType);

            tsbReload.Enabled = true;
        }

        /// <summary>
        /// 关闭V2ray
        /// </summary>
        private void CloseV2ray()
        {
            ConfigHandler.SaveConfig(ref config, false);
            statistics?.SaveToFile();

            ChangePACButtonStatus(0);

            v2rayHandler.VnetStop();
        }

        #endregion

        #region 功能按钮

        private void lvServers_Click(object sender, EventArgs e)
        {
            int index = -1;
            try
            {
                if (lvServers.SelectedIndices.Count > 0)
                {
                    index = lvServers.SelectedIndices[0];
                }
            }
            catch
            {
            }
            if (index < 0)
            {
                return;
            }
            qrCodeControl.showQRCode(index, config);
        }

        private void lvServers_DoubleClick(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }

            if (config.vmess[index].configType == (int)EConfigType.Vmess)
            {
                var fm = new AddServerForm();
                fm.EditIndex = index;
                if (fm.ShowDialog() == DialogResult.OK)
                {
                    //刷新
                    RefreshServers();
                    LoadV2ray();
                }
            }
            else if (config.vmess[index].configType == (int)EConfigType.Shadowsocks)
            {
                var fm = new AddServer3Form();
                fm.EditIndex = index;
                if (fm.ShowDialog() == DialogResult.OK)
                {
                    RefreshServers();
                    LoadV2ray();
                }
            }
            else if (config.vmess[index].configType == (int)EConfigType.Socks)
            {
                var fm = new AddServer4Form();
                fm.EditIndex = index;
                if (fm.ShowDialog() == DialogResult.OK)
                {
                    RefreshServers();
                    LoadV2ray();
                }
            }
            else if (config.vmess[index].configType == (int)EConfigType.Trojan)
            {
                var fm = new AddServer5Form();
                fm.EditIndex = index;
                if (fm.ShowDialog() == DialogResult.OK)
                {
                    RefreshServers();
                    LoadV2ray();
                }
            }
            else
            {
                var fm2 = new AddServer2Form();
                fm2.EditIndex = index;
                if (fm2.ShowDialog() == DialogResult.OK)
                {
                    //刷新
                    RefreshServers();
                    LoadV2ray();
                }
            }
        }

        private void lvServers_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.A:
                        menuSelectAll_Click(null, null);
                        break;
                    case Keys.P:
                        menuPingServer_Click(null, null);
                        break;
                    case Keys.O:
                        menuTcpingServer_Click(null, null);
                        break;
                    case Keys.R:
                        menuRealPingServer_Click(null, null);
                        break;
                    case Keys.T:
                        menuSpeedServer_Click(null, null);
                        break;
                }
            }
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    menuSetDefaultServer_Click(null, null);
                    break;
                case Keys.Delete:
                    menuRemoveServer_Click(null, null);
                    break;
                case Keys.U:
                    menuMoveUp_Click(null, null);
                    break;
                case Keys.D:
                    menuMoveDown_Click(null, null);
                    break;
            }
        }

        private void menuAddVmessServer_Click(object sender, EventArgs e)
        {
            AddServerForm fm = new AddServerForm();
            fm.EditIndex = -1;
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
        }

        private void menuRemoveServer_Click(object sender, EventArgs e)
        {

            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            if (UI.ShowYesNo(UIRes.I18N("RemoveServer")) == DialogResult.No)
            {
                return;
            }
            for (int k = lvSelecteds.Count - 1; k >= 0; k--)
            {
                ConfigHandler.RemoveServer(ref config, lvSelecteds[k]);
            }
            //刷新
            RefreshServers();
            LoadV2ray();

        }

        private void menuRemoveDuplicateServer_Click(object sender, EventArgs e)
        {
            List<Mode.VmessItem> servers = null;
            Utils.DedupServerList(config.vmess, out servers);
            if (servers != null)
            {
                config.vmess = servers;
            }
            //刷新
            RefreshServers();
            LoadV2ray();
        }

        private void menuCopyServer_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            if (ConfigHandler.CopyServer(ref config, index) == 0)
            {
                //刷新
                RefreshServers();
            }
        }

        private void menuSetDefaultServer_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                return;
            }
            SetDefaultServer(index);
        }


        private void menuPingServer_Click(object sender, EventArgs e)
        {
            Speedtest("ping");
        }
        private void menuTcpingServer_Click(object sender, EventArgs e)
        {
            Speedtest("tcping");
        }

        private void menuRealPingServer_Click(object sender, EventArgs e)
        {
            //if (!config.sysAgentEnabled)
            //{
            //    UI.Show(UIRes.I18N("NeedHttpGlobalProxy"));
            //    return;
            //}

            //UI.Show(UIRes.I18N("SpeedServerTips"));

            Speedtest("realping");
        }

        private void menuSpeedServer_Click(object sender, EventArgs e)
        {
            //if (!config.sysAgentEnabled)
            //{
            //    UI.Show(UIRes.I18N("NeedHttpGlobalProxy"));
            //    return;
            //}

            //UI.Show(UIRes.I18N("SpeedServerTips"));

            Speedtest("speedtest");
        }
        private void Speedtest(string actionType)
        {
            GetLvSelectedIndex();
            ClearTestResult();
            var statistics = new SpeedtestHandler(ref config, ref v2rayHandler, lvSelecteds, actionType, UpdateSpeedtestHandler);
        } 
        private void SpeedtestAll(string actionType)
        {
            if (actionType == "speedtestone")
            {
                actionType = "speedtest";
                int s = GetLvSelectedIndexOneSUB();
            if (    s == -1)
                {
                    repinglb.Text = "StoppingTest Timer " + timera.Interval + " s:"+ s;
                 
                    timera.Enabled = false;
                    tab2log.AppendText("\r\nStoppingTest:" + timera.Enabled + " " + DateTime.Now.ToString("MM-dd HH:mm:ss"));
                    return;
                }
                ClearTestResult();
            }
            else
            {

                GetLvSelectedIndexAllSUB();
                ClearTestResult();
            }
            var statistics = new SpeedtestHandler(ref config, ref v2rayHandler, lvSelecteds, actionType, UpdateSpeedtestHandler);
        }

        private void menuExport2ClientConfig_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            MainFormHandler.Instance.Export2ClientConfig(index, config);
        }

        private void menuExport2ServerConfig_Click(object sender, EventArgs e)
        {
            int index = GetLvSelectedIndex();
            MainFormHandler.Instance.Export2ServerConfig(index, config);
        }

        private void menuExport2ShareUrl_Click(object sender, EventArgs e)
        {
            GetLvSelectedIndex();

            StringBuilder sb = new StringBuilder();
            for (int k = 0; k < lvSelecteds.Count; k++)
            {
                string url = ConfigHandler.GetVmessQRCode(config, lvSelecteds[k]);
                if (Utils.IsNullOrEmpty(url))
                {
                    continue;
                }
                sb.Append(url);
                sb.AppendLine();
            }
            if (sb.Length > 0)
            {
                Utils.SetClipboardData(sb.ToString());
                UI.Show(UIRes.I18N("BatchExportURLSuccessfully"));
            }
        }

        private void menuExport2SubContent_Click(object sender, EventArgs e)
        {
            GetLvSelectedIndex();

            StringBuilder sb = new StringBuilder();
            for (int k = 0; k < lvSelecteds.Count; k++)
            {
                string url = ConfigHandler.GetVmessQRCode(config, lvSelecteds[k]);
                if (Utils.IsNullOrEmpty(url))
                {
                    continue;
                }
                sb.Append(url);
                sb.AppendLine();
            }
            if (sb.Length > 0)
            {
                Utils.SetClipboardData(Utils.Base64Encode(sb.ToString()));
                UI.Show(UIRes.I18N("BatchExportSubscriptionSuccessfully"));
            }
        }

        private void tsbOptionSetting_Click(object sender, EventArgs e)
        {
            OptionSettingForm fm = new OptionSettingForm();
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
        }

        private void tsbReload_Click(object sender, EventArgs e)
        {
            Global.reloadV2ray = true;
            LoadV2ray();
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            HideForm();
            //this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// 设置活动服务器
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int SetDefaultServer(int index)
        {
            if (index < 0)
            {
                UI.Show(UIRes.I18N("PleaseSelectServer"));
                return -1;
            }
            if (ConfigHandler.SetDefaultServer(ref config, index) == 0)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
                //定时检查
                timera.Elapsed -= new System.Timers.ElapsedEventHandler(Autotestspeed);
                timera.Elapsed += new System.Timers.ElapsedEventHandler(Autotestspeed);
                 timera.AutoReset = true; //每到指定时间Elapsed事件是触发一次（false），还是一直触发（t

                timera.Enabled = true; //是否触发Elapsed事件
                tab2log.AppendText("\r\n timera:" + timera.Interval + " Open:" + timera.Enabled);
            }
            return 0;
        }

        /// <summary>
        /// 取得ListView选中的行
        /// </summary>
        /// <returns></returns>
        private int GetLvSelectedIndex()
        {
            int index = -1;
            lvSelecteds.Clear();
            try
            {
                if (lvServers.SelectedIndices.Count <= 0)
                {
                    UI.Show(UIRes.I18N("PleaseSelectServer"));
                    return index;
                }

                index = lvServers.SelectedIndices[0];
                foreach (int i in lvServers.SelectedIndices)
                {
                    lvSelecteds.Add(i);
                }
                return index;
            }
            catch
            {
                return index;
            }
        }

        private int GetLvSelectedIndexAllSUB()
        {
           
            int index = -1;
            lvSelecteds.Clear();
            try
            {
                foreach (ListViewItem eachItem in lvServers.Items)
                    if (eachItem.SubItems[4].Text != "0" && !FunA.MethodCanUse(eachItem.SubItems[5].Text))
                    {
                        lvServers.Items.Remove(eachItem);
                    }

                foreach (ListViewItem eachItem in lvServers.Items)
                    {
                    
                        if (eachItem.SubItems[7].Text.StartsWith("import") && FunA.MethodCanUse(eachItem.SubItems[5].Text) && eachItem.SubItems[4].Text != "0")
                    {
                        txtMsgBox.AppendText(eachItem.SubItems[1].Text + " " + eachItem.SubItems[2].Text + "\r\n");
                        lvSelecteds.Add(eachItem.Index); 
                     }
                    }

                if (lvSelecteds.Count <= 0)
                {
                   // UI.Show(UIRes.I18N("PleaseSelectServer"));
                    return index;
                }

                //index = lvServers.SelectedIndices[0];
                //foreach (int i in lvServers.SelectedIndices)
                //{
                //    lvSelecteds.Add(i);
                //}
                return index;
            }
            catch
            {
                return index;
            }
        }
        private int GetLvSelectedIndexOneSUB()
        {

            int index = -1;
            lvSelecteds.Clear();
            try
            {
               

                foreach (ListViewItem eachItem in lvServers.Items)
                {

                    if (eachItem.SubItems[7].Text .StartsWith("import") &&  eachItem.SubItems[0].Text.Length> 0)
                    {
                        tab2log.AppendText("testoneSelect "+eachItem.SubItems[1].Text + " " + eachItem.SubItems[2].Text + "" + "\r\n");
                        lvSelecteds.Add(eachItem.Index);
                        index++;
                    }
                }

                if (lvSelecteds.Count <= 0)
                {
                    tab2log.AppendText("testoneSelect1.Count: " + lvSelecteds.Count + "\r\n");
                    // UI.Show(UIRes.I18N("PleaseSelectServer"));
                    return index;
                }

                //index = lvServers.SelectedIndices[0];
                //foreach (int i in lvServers.SelectedIndices)
                //{
                //    lvSelecteds.Add(i);
                //}
                tab2log.AppendText("testoneSelect2.Count: " + lvSelecteds.Count + "\r\n");
                return index;
            }
            catch
            {
                tab2log.AppendText("testoneSelect3.Count: " + lvSelecteds.Count + "\r\n");
                return index;
            }
        }
        private void menuAddCustomServer_Click(object sender, EventArgs e)
        {
            UI.Show(UIRes.I18N("CustomServerTips"));

            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "Config|*.json|All|*.*";
            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string fileName = fileDialog.FileName;
            if (Utils.IsNullOrEmpty(fileName))
            {
                return;
            }

            if (ConfigHandler.AddCustomServer(ref config, fileName) == 0)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
                UI.Show(UIRes.I18N("SuccessfullyImportedCustomServer"));
            }
            else
            {
                UI.Show(UIRes.I18N("FailedImportedCustomServer"));
            }
        }

        private void menuAddShadowsocksServer_Click(object sender, EventArgs e)
        {
            var fm = new AddServer3Form();
            fm.EditIndex = -1;
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
            ShowForm();
        }

        private void menuAddSocksServer_Click(object sender, EventArgs e)
        {
            var fm = new AddServer4Form();
            fm.EditIndex = -1;
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
            ShowForm();
        }

        private void menuAddServers_Click(object sender, EventArgs e)
        {
            string clipboardData = Utils.GetClipboardData();
            if (AddBatchServers(clipboardData) == 0)
            {
                UI.Show(UIRes.I18N("SuccessfullyImportedServerViaClipboard"));
            }
        }

        private void menuScanScreen_Click(object sender, EventArgs e)
        {
            HideForm();
            bgwScan.RunWorkerAsync();
        }

        private int AddBatchServers(string clipboardData, string subid = "")
        {
            AppendText(false, $"{"3"}{"AddBatchServers1-clipboardData::" + clipboardData}");
            if (ConfigHandler.AddBatchServers(ref config, clipboardData, subid) != 0)
            {
                AppendText(false, $"{"4"}{"AddBatchServers1-AddBatchServers::" + "true"}");
                clipboardData = Utils.Base64Decode(clipboardData);
                if (ConfigHandler.AddBatchServers(ref config, clipboardData, subid) != 0)
                {
                    return -1;
                }
            }
            else
            {
                AppendText(false, $"{"4"}{"AddBatchServers1-AddBatchServers::" + "false"}");
            }
            RefreshServers();
            return 0;
        }

        #endregion


        #region 提示信息

        /// <summary>
        /// 消息委托
        /// </summary>
        /// <param name="notify"></param>
        /// <param name="msg"></param>
        void v2rayHandler_ProcessEvent(bool notify, string msg)
        {
            AppendText(notify, msg);
        }

        delegate void AppendTextDelegate(string text);
        void AppendText(bool notify, string msg)
        {
            try
            {
                AppendText(msg);
                if (notify)
                {
                    notifyMsg(msg);
                }
            }
            catch
            {
            }
        }

        void AppendText(string text)
        {
            if (this.txtMsgBox.InvokeRequired)
            {
                Invoke(new AppendTextDelegate(AppendText), new object[] { text });
            }
            else
            {
                //this.txtMsgBox.AppendText(text);
                ShowMsg(text);
            }
        }

        /// <summary>
        /// 提示信息
        /// </summary>
        /// <param name="msg"></param>
        private void ShowMsg(string msg)
        {
            if (txtMsgBox.Lines.Length > 999)
            {
                ClearMsg();
            }
            this.txtMsgBox.AppendText(msg);
            if (!msg.EndsWith(Environment.NewLine))
            {
                this.txtMsgBox.AppendText(Environment.NewLine);
            }
        }

        /// <summary>
        /// 清除信息
        /// </summary>
        private void ClearMsg()
        {
            this.txtMsgBox.Clear();
        }

        /// <summary>
        /// 托盘信息
        /// </summary>
        /// <param name="msg"></param>
        private void notifyMsg(string msg)
        {
            notifyMain.Text = msg;
        }

        #endregion


        #region 托盘事件

        private void notifyMain_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ShowForm();
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {

            this.Visible = false;
            this.Close();

            Application.Exit();
        }


        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            //this.notifyIcon1.Visible = false;
            this.ShowInTaskbar = true;
            this.txtMsgBox.ScrollToCaret();

            SetVisibleCore(true);
        }

        private void HideForm()
        {
            //this.WindowState = FormWindowState.Minimized;
            this.Hide();
            //this.notifyMain.Icon = this.Icon;
            this.notifyMain.Visible = true;
            this.ShowInTaskbar = false;

            SetVisibleCore(false);
        }

        #endregion

        #region 后台测速

        private void SetTestResult(int k, string txt)
        {
            if(k != -1)
            try
            {
                config.vmess[k].testResult = txt;
                lvServers.Items[k].SubItems[8].Text = txt;
            }catch(Exception ex)
            {
                Utils.SaveLog("SetTestResultErr:" + ex.Message, ex);
            }
        }
        private void ClearTestResult()
        {
            for (int k = 0; k < lvSelecteds.Count; k++)
            {
                SetTestResult(lvSelecteds[k], "");
            }
        }
        private void UpdateSpeedtestHandler(int index, string msg)
        {
            if (index < 0)
                return;
            lvServers.Invoke((MethodInvoker)delegate
            {
                lvServers.SuspendLayout();

                SetTestResult(index, msg);

                lvServers.ResumeLayout();
            });
        }

        private void UpdateStatisticsHandler(ulong up, ulong down, List<ServerStatItem> statistics)
        {
            try
            {
                up /= (ulong)(config.statisticsFreshRate / 1000f);
                down /= (ulong)(config.statisticsFreshRate / 1000f);
                toolSslServerSpeed.Text = string.Format("{0}/s↑ | {1}/s↓", Utils.HumanFy(up), Utils.HumanFy(down));

                List<string[]> datas = new List<string[]>();
                for (int i = 0; i < config.vmess.Count; i++)
                {
                    var index = statistics.FindIndex(item_ => item_.itemId == config.vmess[i].getItemId());
                    if (index != -1)
                    {
                        lvServers.Invoke((MethodInvoker)delegate
                        {
                            lvServers.SuspendLayout();

                            var indexStart = 9;
                            lvServers.Items[i].SubItems[indexStart++].Text = Utils.HumanFy(statistics[index].totalUp);
                            lvServers.Items[i].SubItems[indexStart++].Text = Utils.HumanFy(statistics[index].totalDown);
                            lvServers.Items[i].SubItems[indexStart++].Text = Utils.HumanFy(statistics[index].todayUp);
                            lvServers.Items[i].SubItems[indexStart++].Text = Utils.HumanFy(statistics[index].todayDown);

                            lvServers.ResumeLayout();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
            }
        }

        #endregion

        #region 移动服务器

        private void menuMoveTop_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Top);
        }

        private void menuMoveUp_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Up);
        }

        private void menuMoveDown_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Down);
        }

        private void menuMoveBottom_Click(object sender, EventArgs e)
        {
            MoveServer(EMove.Bottom);
        }

        private void MoveServer(EMove eMove)
        {
            int index = GetLvSelectedIndex();
            if (index < 0)
            {
                UI.Show(UIRes.I18N("PleaseSelectServer"));
                return;
            }
            if (ConfigHandler.MoveServer(ref config, index, eMove) == 0)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
        }
        private void menuSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvServers.Items)
            {
                item.Selected = true;
            }
        }

        #endregion

        #region 系统代理相关

        private void menuCopyPACUrl_Click(object sender, EventArgs e)
        {
            Utils.SetClipboardData(HttpProxyHandle.GetPacUrl());
        }

        private void menuNotEnabledHttp_Click(object sender, EventArgs e)
        {
            SetListenerType(0);
        }

        private void menuGlobal_Click(object sender, EventArgs e)
        {
            SetListenerType(1);
        }

        private void menuGlobalPAC_Click(object sender, EventArgs e)
        {
            SetListenerType(2);
        }

        private void menuKeep_Click(object sender, EventArgs e)
        {
            SetListenerType(3);
        }

        private void menuKeepPAC_Click(object sender, EventArgs e)
        {
            SetListenerType(4);
        }

        private void SetListenerType(int type)
        {
            config.listenerType = type;
            ChangePACButtonStatus(type);
        }

        private void ChangePACButtonStatus(int type)
        {
            if (type != 0)
            {
                HttpProxyHandle.RestartHttpAgent(config, false);
            }
            else
            {
                HttpProxyHandle.CloseHttpAgent(config);
            }

            for (int k = 0; k < menuSysAgentMode.DropDownItems.Count; k++)
            {
                var item = ((ToolStripMenuItem)menuSysAgentMode.DropDownItems[k]);
                item.Checked = (type == k);
            }

            ConfigHandler.SaveConfig(ref config, false);
            DisplayToolStatus();
        }

        #endregion


        #region CheckUpdate

        private void tsbCheckUpdateN_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start(Global.UpdateUrl);
            DownloadHandle downloadHandle = null;
            if (downloadHandle == null)
            {
                downloadHandle = new DownloadHandle();
                downloadHandle.AbsoluteCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, UIRes.I18N("MsgParsingV2rayCoreSuccessfully"));

                        string url = args.Msg;
                        this.Invoke((MethodInvoker)(delegate
                        {

                            if (UI.ShowYesNo(string.Format(UIRes.I18N("DownloadYesNo"), url)) == DialogResult.No)
                            {
                                return;
                            }
                            else
                            {
                                downloadHandle.DownloadFileAsync(config, url, null, -1);
                            }
                        }));
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, UIRes.I18N("MsgDownloadV2rayCoreSuccessfully"));

                        try
                        {
                            var fileName = Utils.GetPath(downloadHandle.DownloadFileName);
                            var process = Process.Start("v2rayUpgrade.exe", fileName);
                            if (process.Id > 0)
                            {
                                menuExit_Click(null, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendText(false, ex.Message);
                        }
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle.Error += (sender2, args) =>
                {
                    AppendText(true, args.GetException().Message);
                };
            }

            AppendText(false, UIRes.I18N("MsgStartUpdatingV2rayCore"));
            downloadHandle.AbsoluteV2rayN(config);
        }

        private void tsbCheckUpdateCore_Click(object sender, EventArgs e)
        {
            DownloadHandle downloadHandle = null;
            if (downloadHandle == null)
            {
                downloadHandle = new DownloadHandle();
                downloadHandle.AbsoluteCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, UIRes.I18N("MsgParsingV2rayCoreSuccessfully"));

                        string url = args.Msg;
                        this.Invoke((MethodInvoker)(delegate
                        {

                            if (UI.ShowYesNo(string.Format(UIRes.I18N("DownloadYesNo"), url)) == DialogResult.No)
                            {
                                return;
                            }
                            else
                            {
                                downloadHandle.DownloadFileAsync(config, url, null, -1);
                            }
                        }));
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, UIRes.I18N("MsgDownloadV2rayCoreSuccessfully"));
                        AppendText(false, UIRes.I18N("MsgUnpacking"));

                        try
                        {
                            CloseV2ray();

                            string fileName = downloadHandle.DownloadFileName;
                            fileName = Utils.GetPath(fileName);
                            FileManager.ZipExtractToFile(fileName);

                            AppendText(false, UIRes.I18N("MsgUpdateV2rayCoreSuccessfullyMore"));

                            Global.reloadV2ray = true;
                            LoadV2ray();

                            AppendText(false, UIRes.I18N("MsgUpdateV2rayCoreSuccessfully"));
                        }
                        catch (Exception ex)
                        {
                            AppendText(false, ex.Message);
                        }
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle.Error += (sender2, args) =>
                {
                    AppendText(true, args.GetException().Message);
                };
            }

            AppendText(false, UIRes.I18N("MsgStartUpdatingV2rayCore"));
            downloadHandle.AbsoluteV2rayCore(config);
        }

        private void tsbCheckUpdatePACList_Click(object sender, EventArgs e)
        {
            DownloadHandle pacListHandle = null;
            if (pacListHandle == null)
            {
                pacListHandle = new DownloadHandle();
                pacListHandle.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        var result = args.Msg;
                        if (Utils.IsNullOrEmpty(result))
                        {
                            return;
                        }
                        pacListHandle.GenPacFile(result);

                        AppendText(false, UIRes.I18N("MsgPACUpdateSuccessfully"));
                    }
                    else
                    {
                        AppendText(false, UIRes.I18N("MsgPACUpdateFailed"));
                    }
                };
                pacListHandle.Error += (sender2, args) =>
                {
                    AppendText(true, args.GetException().Message);
                };
            }
            AppendText(false, UIRes.I18N("MsgStartUpdatingPAC"));
            pacListHandle.WebDownloadString(config.urlGFWList);
        }

        private void tsbCheckClearPACList_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(Utils.GetPath(Global.pacFILE), Utils.GetEmbedText(Global.BlankPacFileName), Encoding.UTF8);
                AppendText(false, UIRes.I18N("MsgSimplifyPAC"));
            }
            catch (Exception ex)
            {
                Utils.SaveLog(ex.Message, ex);
            }
        }
        #endregion

        #region Help


        private void tsbAbout_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Global.AboutUrl);
        }

        private void tsbPromotion_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start($"{Utils.Base64Decode(Global.PromotionUrl)}?t={DateTime.Now.Ticks}");
        }
        #endregion

        #region ScanScreen


        private void bgwScan_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string ret = Utils.ScanScreen();
            bgwScan.ReportProgress(0, ret);
        }

        private void bgwScan_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            ShowForm();

            string result = Convert.ToString(e.UserState);
            if (Utils.IsNullOrEmpty(result))
            {
                UI.Show(UIRes.I18N("NoValidQRcodeFound"));
            }
            else
            {
                if (AddBatchServers(result) == 0)
                {
                    UI.Show(UIRes.I18N("SuccessfullyImportedServerViaScan"));
                }
            }
        }

        #endregion

        #region 订阅
        private void tsbSubSetting_Click(object sender, EventArgs e)
        {
            SubSettingForm fm = new SubSettingForm();
            if (fm.ShowDialog() == DialogResult.OK)
            {
                RefreshServers();
            }
        }

        private void tsbSubUpdate_Click(object sender, EventArgs e)
        {
            UpdateSub();
        }
        internal bool UpdateSub()
        {
            bool endok = false;
            SubSlb.Text = "substart";
            AppendText(false, UIRes.I18N("MsgUpdateSubscriptionStart"));

            if (config.subItem == null || config.subItem.Count <= 0)
            {
                AppendText(false, UIRes.I18N("MsgNoValidSubscription"));
                return endok;
            }

            for (int k = 1; k <= config.subItem.Count; k++)
            {
                string id = config.subItem[k - 1].id.TrimEx();
                string url = config.subItem[k - 1].url.TrimEx();
                string hashCode = $"{k}->";
                if (config.subItem[k - 1].enabled == false)
                {
                    continue;
                }
                if (Utils.IsNullOrEmpty(id) || Utils.IsNullOrEmpty(url))
                {
                    AppendText(false, $"{hashCode}{UIRes.I18N("MsgNoValidSubscription")}");
                    continue;
                }

                DownloadHandle downloadHandle3 = new DownloadHandle();
                downloadHandle3.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgGetSubscriptionSuccessfully")}");
                        var result = Utils.Base64Decode(args.Msg);
                        if (Utils.IsNullOrEmpty(result))
                        {
                            AppendText(false, $"{hashCode}{UIRes.I18N("MsgSubscriptionDecodingFailed")}");
                            return ;
                        }

                        ConfigHandler.RemoveServerViaSubid(ref config, id);
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgClearSubscription")}");
                        RefreshServers();
                        if (AddBatchServers(result, id) == 0)
                        {
                        }
                        else
                        {
                            AppendText(false, $"{hashCode}{"result3:" + result}");
                            AppendText(false, $"{hashCode}{UIRes.I18N("MsgFailedImportSubscription")}");
                        }
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgUpdateSubscriptionEnd")}");
                        endok = true;
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle3.Error += (sender2, args) =>
                {
                    AppendText(true, args.GetException().Message);
                };

                downloadHandle3.WebDownloadString(url);
                AppendText(false, $"{hashCode}{UIRes.I18N("MsgStartGettingSubscriptions")}");
            }
          
            SubSlb.Text = "subend";  return endok;
        }
        internal void SubscriptionUpdatewait()
        {
            SubSlb.Text = "substart";
            AppendText(false, UIRes.I18N("MsgUpdateSubscriptionStart"));

            if (config.subItem == null || config.subItem.Count <= 0)
            {
                AppendText(false, UIRes.I18N("MsgNoValidSubscription"));
                return;
            }

            for (int k = 1; k <= config.subItem.Count; k++)
            {
                string id = config.subItem[k - 1].id.TrimEx();
                string url = config.subItem[k - 1].url.TrimEx();
                string hashCode = $"{k}->";
                if (config.subItem[k - 1].enabled == false)
                {
                    continue;
                }
                if (Utils.IsNullOrEmpty(id) || Utils.IsNullOrEmpty(url))
                {
                    AppendText(false, $"{hashCode}{UIRes.I18N("MsgNoValidSubscription")}");
                    continue;
                }

                DownloadHandle downloadHandle3 = new DownloadHandle();
                downloadHandle3.UpdateCompleted += (sender2, args) =>
                {
                    if (args.Success)
                    {
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgGetSubscriptionSuccessfully")}");
                        var result = Utils.Base64Decode(args.Msg);
                        if (Utils.IsNullOrEmpty(result))
                        {
                            AppendText(false, $"{hashCode}{UIRes.I18N("MsgSubscriptionDecodingFailed")}");
                            return;
                        }

                        ConfigHandler.RemoveServerViaSubid(ref config, id);
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgClearSubscription")}");
                        RefreshServers();
                        if (AddBatchServers(result, id) == 0)
                        {
                            AppendText(false, $"{hashCode}{"result:"+ result}");

                        }
                        else
                        {
                            AppendText(false, $"{hashCode}{"result2:" + result}");
                            AppendText(false, $"{hashCode}{UIRes.I18N("MsgFailedImportSubscription")}");
                        }
                        AppendText(false, $"{hashCode}{UIRes.I18N("MsgUpdateSubscriptionEnd")}");
                        SubSlb.Text = "subend";
                        SpeedtestAll("allping");
                    }
                    else
                    {
                        AppendText(false, args.Msg);
                    }
                };
                downloadHandle3.Error += (sender2, args) =>
                {
                    AppendText(true, args.GetException().Message);
                };

                downloadHandle3.WebDownloadString(url);
                AppendText(false, $"{hashCode}{UIRes.I18N("MsgStartGettingSubscriptions")}");
            }
            
        }
        #endregion

        #region Language

        private void tsbLanguageDef_Click(object sender, EventArgs e)
        {
            SetCurrentLanguage("en");
        }       

        private void tsbLanguageZhHans_Click(object sender, EventArgs e)
        {
            SetCurrentLanguage("zh-Hans");
        }
        private void SetCurrentLanguage(string value)
        {
            Utils.RegWriteValue(Global.MyRegPath, Global.MyRegKeyLanguage, value);
        }

        #endregion
        

        private void 全部订阅pingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PingAllAndSelectOne();
        }
        internal void PingAllAndSelectOne()
        {
            repinglb.Text = "ping";
            SubscriptionUpdatewait();
      
          

            _workThread = new Thread(new ThreadStart(CheckPingOver));
            _workThread.IsBackground = true;
            _workThread.Start();
        }
        private Thread _workThread;
        internal bool existPingPass()
        {
            bool t = false;
            foreach (ListViewItem eachItem in lvServers.Items)
            {

                if (eachItem.SubItems[7].Text.StartsWith("import") && eachItem.SubItems[8].Text.Contains("ms")  && eachItem.SubItems[8].Text.StartsWith("-1"))
                {
                    t = true;
                    break;
                }
            }
            return t;
        }
        /// <summary>
        /// 等等ping全部结束后执行
        /// </summary>
        public void CheckPingOver()
        {
            int i = 0;
            int n = 0;
            while (true)
            {
                i = 0;
                foreach (ListViewItem eachItem in lvServers.Items)
                {

                    if (eachItem.SubItems[7].Text .StartsWith("import") && eachItem.SubItems[8].Text.Length <= 0)
                    {
                        i ++;
                        break;
                    }
                }
                if ((i == 0 || n >= 6) && existPingPass())
                {
                  
                    break;
                }
                if (( n >= 10) && !existPingPass())
                {
                    SpeedtestAll("ping");
                   
                }
                repinglb.Text = "ping " + n;
                Thread.Sleep(6000);
                n++;
            }
          
            Autoselect(1);
        }

        public void ChecktestOver()
        {
            if (timera.Enabled == false || lvSelecteds.Count<1)
                return;
            int n = 0;
            int index = -1;
            while (n<15)
            {
               
                if(index == -1)
                foreach (ListViewItem eachItem in lvServers.Items)
                {
             if (eachItem.SubItems[7].Text .StartsWith("import") && eachItem.SubItems[0].Text.Length > 0 )
                    {index = eachItem.Index;
                      //  i++;
                        break;
                    }
                }

                if(index != -1)
                if ( lvServers.Items[index].SubItems[8].Text.Length <= 0 || lvServers.Items[index].SubItems[8].Text.Contains("..."))
    {           
                }
                else if(lvServers.Items[index].SubItems[8].Text.StartsWith("无法连接到远程服务器"))
                    {
                        v2rayHandler.VnetStop();
                    }else
                {
                    n = 16;
                    break;
                }
               
                repinglb.Text = "ping test " + n + " :"+ index;
                Thread.Sleep(6000);
                n++;
            }
            double speed = 0.00;
            if (index != -1 && double.TryParse(lvServers.Items[index].SubItems[8].Text.Replace("<", "").Replace(" M/s", ""), out speed))
            {
            if (speed <= 0.15)
                    Autoselect(3);//换一个
                repinglb.Text = "testDone " + speed ;
                tab2log.AppendText("\r\n" + repinglb.Text + " "+timera.Interval/1000+" " + DateTime.Now.ToString("MM-dd HH:mm:ss"));
            }
            else
            {
                repinglb.Text = "testDone " + speed +" " +timera.Interval / 1000 + " " ;
                Autoselect(3);//换一个
            }
        }
        int Fastitem = -1;
        public void Autoselect(Byte type)
        {
            List<int> ls = new List<int>() { };
            if (type == 2)
            {
                repinglb.Text = "random";
                ls.Clear();
                foreach (ListViewItem eachItem in lvServers.Items)
                {
                    if (eachItem.SubItems[7].Text.StartsWith("import") && FunA.MethodCanUse(eachItem.SubItems[5].Text) && eachItem.SubItems[4].Text !="0")
                        ls.Add(eachItem.Index);
                       
                }
                Random ranone = new Random();
                int i = ranone.Next(0, ls.Count -1);

                SetDefaultServer(ls[i]);
                this.Text = lvServers.Items[ls[i]].SubItems[2].Text;
                repinglb.Text = "Donerandom " + ls[i];
                return;
            }
            if (type == 3)
            {
                repinglb.Text = "random";
                ls.Clear();
                foreach (ListViewItem eachItem in lvServers.Items)
                {
                    if (eachItem.SubItems[7].Text .StartsWith("import") && FunA.MethodCanUse(eachItem.SubItems[5].Text) && eachItem.SubItems[4].Text != "0" && eachItem.SubItems[8].Text == "")
                        ls.Add(eachItem.Index);

                }
                Random ranone = new Random();
                if(ls.Count == 0)
                {
                  
                        if (UpdateSub())
                        {
                            testonespeed();
                            return;
                    }
                    else
                    {
                        testonespeed();
                        return;
                    }
                    
                    //更新订阅
                   
                }
                int i = ranone.Next(0, ls.Count - 1);

                SetDefaultServer(ls[i]);
                this.Text = lvServers.Items[ls[i]].SubItems[2].Text;
                repinglb.Text = "DoneTestrandom " + ls[i];
                testonespeed();
                return;
            }
            Mslist.View = View.Details;
            Mslist.Clear();
            Mslist.Items.Clear();
            Mslist.Columns.Add("index", 30, HorizontalAlignment.Center);
            Mslist.Columns.Add("ms", 30, HorizontalAlignment.Center);
       
            Mslist.BeginUpdate();
           
            foreach (ListViewItem eachItem in lvServers.Items)
            {
               
                if (eachItem.SubItems[7].Text.StartsWith("import") && !eachItem.SubItems[5].Text.StartsWith("-1") && eachItem.SubItems[8].Text !="")
                {
                
                    Mslist.Items.Add(new ListViewItem(new string[] { eachItem.Index.ToString(), eachItem.SubItems[8].Text.Replace("ms", "") }));

                }
            }
            int nSortCode = 1;
            Mslist.ListViewItemSorter = new ListViewItemComparer(1, nSortCode);
            Mslist.Sort();
            int tryint;
            foreach (ListViewItem eachItem in Mslist.Items)
            {
                // txtMsgBox.AppendText(eachItem.SubItems[7].Text + " ");
                if ( !int.TryParse( eachItem.SubItems[1].Text ,out tryint))
                {
                    Mslist.Items[eachItem.Index].Remove();
                }
            }
            foreach (ListViewItem eachItem in Mslist.Items)
            {
               // txtMsgBox.AppendText(eachItem.SubItems[7].Text + " ");
                if (eachItem.Index>=10)
                {
                    Mslist.Items[eachItem.Index].Remove();
                }
            }
            Random  ran = new  Random();
            int aran = ran.Next(0, 3);
            if (Mslist.Items.Count < 3)
                aran = 0;
            if(!existPingPass())
            {
                repinglb.Text = "ping结束 00";
                return;
            }
            Fastitem = int.Parse(Mslist.Items[aran].SubItems[0].Text);
            Mslist.EndUpdate();
            int index = Fastitem;
            if (index < 0)
            {
                repinglb.Text = "ping结束 0";
                return;
            }
            SetDefaultServer(index);
            repinglb.Text = "ping结束" + Fastitem + "时间" + DateTime.Now;
            tab2log.AppendText("ping结束" + Fastitem + "时间" + DateTime.Now.ToString("MM-dd HH:mm:ss"));
        }

        private void 自动切换最快节点ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Autoselect(1);
        }

        private void txtMsgBox_TextChanged(object sender, EventArgs e)
        {

            if(txtMsgBox.Text.Contains("google.mn") && !repinglb.Text.StartsWith( "ping"))
            {
                int i = 0;
                foreach (ListViewItem eachItem in lvServers.Items)
                {

                    if (eachItem.SubItems[7].Text.StartsWith("import"))
                    {
                        i++;
                        break;
                    }
                }
                if (i > 0)
                {
                    txtMsgBox.Text = "RePingAll";
                    PingAllAndSelectOne();
                }
                //  timera.Elapsed -= new System.Timers.ElapsedEventHandler(AutoPingAllAgain);
                //   timera.Elapsed += new System.Timers.ElapsedEventHandler(AutoPingAllAgain);
                //   timera.AutoReset = true; //每到指定时间Elapsed事件是触发一次（false），还是一直触发（t
                //   timera.Enabled = true; //是否触发Elapsed事件
            }else if (txtMsgBox.Text.Contains("google.ms") && !repinglb.Text.StartsWith("random"))
            {
                Autoselect(2);
            }
        }
        private System.Timers.Timer timera = new System.Timers.Timer(1000 * 60 * 60); //设置时间间隔
        private void Autotestspeed(object sender, System.Timers.ElapsedEventArgs e)
        {
            testonespeed();


        }
        internal void testonespeed()
        {if (repinglb.Text.StartsWith("ping"))
                return;
            repinglb.Text = "ping Timer " + timera.Interval;
            //  tsbSubUpdate.PerformClick();
            SpeedtestAll("speedtestone");

            _workThread = new Thread(new ThreadStart(ChecktestOver));
            _workThread.IsBackground = true;
            _workThread.Start();
        }
        private void PingAllAgain_Click(object sender, EventArgs e)
        {
          
            repinglb.Text = "ping";

            //SpeedtestAll("ping");
            //foreach (ListViewItem eachItem in lvServers.Items)
            //{

            //    if (eachItem.SubItems[7].Text == "import sub" && eachItem.SubItems[8].Text.StartsWith("-"))
            //    {
            //        lvServers.Items.Remove(eachItem);
            //    }
            //}
            //  tsbSubUpdate.PerformClick();
            SpeedtestAll("allping");

            _workThread = new Thread(new ThreadStart(CheckPingOver));
            _workThread.IsBackground = true;
            _workThread.Start();
          
        }

        private void PAssBtn_Click(object sender, EventArgs e)
        {
            SafeCheck();
            long time = 0;
            if (Passtxt.Text != "327")
            {
                foreach (ListViewItem eachItem in lvServers.Items)
                {
                    // txtMsgBox.AppendText(eachItem.SubItems[7].Text + " ");
                    if (eachItem.Text.Length>0)
                    {
                      //  SelectIndex=eachItem.Index;
                        time = Utils.Ping(eachItem.SubItems[3].Text);
                    }
                }
                //  time = Utils.Ping("");
                Passtxt.Text = "最大延迟时间:" + time + "ms";
            }
        }
        private void Pass_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                PAssBtn.Focus();
                PAssBtn_Click(this, new EventArgs());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtMsgBox.AppendText("google.ms");
        }

        private void timeraGo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            double t = timera.Interval;
            testonespeed();

            timera.Enabled = true;
            tab2log.AppendText(" timera:" + timera.Interval/1000 + " " + timera.Enabled.ToString() + " t:" +t/1000);
             
        }

        private void addTrojanServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fm = new AddServer5Form();
            fm.EditIndex = -1;
            if (fm.ShowDialog() == DialogResult.OK)
            {
                //刷新
                RefreshServers();
                LoadV2ray();
            }
            ShowForm();
        }
    }
}
