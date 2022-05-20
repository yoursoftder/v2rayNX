﻿using System.Windows.Forms;
using vNet.Handler;
using vNet.Mode;

namespace vNet.Forms
{
    public partial class QRCodeControl : UserControl
    {
        public QRCodeControl()
        {
            InitializeComponent();
        }
        private void QRCodeControl_Load(object sender, System.EventArgs e)
        {
            chkShow_CheckedChanged(null, null);
            txtUrl.MouseUp += txtUrl_MouseUp;      
        }

        void txtUrl_MouseUp(object sender, MouseEventArgs e)
        {
            txtUrl.SelectAll();
        }

        public void showQRCode(int Index, Config config)
        {
            if (Index >= 0)
            {
                string url = ConfigHandler.GetVmessQRCode(config, Index);
                if (Utils.IsNullOrEmpty(url))
                {
                    picQRCode.Image = null;
                    txtUrl.Text = string.Empty;
                    return;
                }
                picQRCode.Image = QRCodeHelper.GetQRCode(url);
                txtUrl.Text = url;
            }
        }

        private void chkShow_CheckedChanged(object sender, System.EventArgs e)
        {
            picQRCode.Visible =
            txtUrl.Visible = chkShow.Checked;
        }

    }
}
