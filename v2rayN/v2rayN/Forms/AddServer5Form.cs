using System;
using System.Windows.Forms;
using vNet.Handler;
using vNet.Mode;

namespace vNet.Forms
{
    public partial class AddServer5Form : BaseForm
    {
        public int EditIndex { get; set; }
        VmessItem vmessItem = null;

        public AddServer5Form()
        {
            InitializeComponent();
        }

        private void AddServer5Form_Load(object sender, EventArgs e)
        {
            if (EditIndex >= 0)
            {
                vmessItem = config.vmess[EditIndex];
                BindingServer();
            }
            else
            {
                vmessItem = new VmessItem();
                ClearServer();
            }
        }

        /// <summary>
        /// 绑定数据
        /// </summary>
        private void BindingServer()
        {
            txtAddress.Text = vmessItem.address;
            txtPort.Text = vmessItem.port.ToString();
            txtId.Text = vmessItem.id;
          txtEmail.Text = vmessItem.security;
            txtRemarks.Text = vmessItem.remarks;
            comboBox1.Text = vmessItem.allowInsecure;
        }


        /// <summary>
        /// 清除设置
        /// </summary>
        private void ClearServer()
        {
            txtAddress.Text = "";
            txtPort.Text = "";
            txtId.Text = "";
            txtEmail.Text = "";
            txtRemarks.Text = "";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string address = txtAddress.Text;
            string port = txtPort.Text;
            string id = txtId.Text;
            string security = txtEmail.Text;
            string remarks = txtRemarks.Text;
            string allowInsecure = comboBox1.Text;

            if (Utils.IsNullOrEmpty(address))
            {
                UI.Show(UIRes.I18N("FillServerAddress"));
                return;
            }
            if (Utils.IsNullOrEmpty(port) || !Utils.IsNumberic(port))
            {
                UI.Show(UIRes.I18N("FillCorrectServerPort"));
                return;
            }
           
            vmessItem.address = address;
            vmessItem.port = Utils.ToInt(port);
            vmessItem.id = id;
         vmessItem.security = security;
            vmessItem.remarks = remarks;
            vmessItem.streamSecurity = "tls";
            vmessItem.allowInsecure = allowInsecure;
            vmessItem.configType = (int)EConfigType.Trojan;

            if (ConfigHandler.AddTrojanServer(ref config, vmessItem, EditIndex) == 0)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                UI.Show(UIRes.I18N("OperationFailed"));
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }


        #region 导入配置
         
        /// <summary>
        /// 从剪贴板导入URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItemImportClipboard_Click(object sender, EventArgs e)
        {
            ImportConfig();
        }

        private void ImportConfig()
        {
            ClearServer();

            string msg;
            VmessItem vmessItem = V2rayConfigHandler.ImportFromClipboardConfig(Utils.GetClipboardData(), out msg);
            if (vmessItem == null)
            {
                UI.Show(msg);
                return;
            }

            txtAddress.Text = vmessItem.address;
            txtPort.Text = vmessItem.port.ToString();
           // txtSecurity.Text = vmessItem.security;
            txtId.Text = vmessItem.id;
            txtRemarks.Text = vmessItem.remarks;
        }        

        #endregion
         

    }
}
