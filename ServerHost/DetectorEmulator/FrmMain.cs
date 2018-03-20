using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DetectorEmulator
{
    public partial class FrmMain : Form
    {
        private string serverAddress;
        public string ServerAddress
        {
            get { return serverAddress; }
            set { serverAddress = value; }
        }
        private string port;
        public string Port
        {
            get { return port; }
            set { port = value; }
        }
        private string userAccountID;
        public string UserAccountID
        {
            get { return userAccountID; }
            set { userAccountID = value; }
        }
        private string detectorID;
        public string DetectorID
        {
            get { return detectorID; }
            set { detectorID = value; }
        }
        private string detectorDescription;
        public string DetectorDescription
        {
            get { return detectorDescription; }
            set { detectorDescription = value; }
        }

        public FrmMain()
        {
            InitializeComponent();
        }

        TCPServer ts = null;

        private void Form1_Load(object sender, EventArgs e)
        {
            ServerAddress = tbServerAddress.Text = GetIPAddress();
            tbServerAddress.Text = "";
            Port = tbPort.Text = ConfigurationManager.AppSettings["Port"];
            DetectorID = tbDetectorID.Text = ConfigurationManager.AppSettings["DetectorID"];
            ts = new TCPServer();
            ts.ServerAddress = tbServerAddress.Text = ConfigurationManager.AppSettings["ServerAddress"];
            ts.Port = tbPort.Text;
            ts.ReturnBytes = GetReturnBytes();
            ts.OnException += ts_OnException;
            ts.OnSendingData += ts_OnSendingData;
        }

        void ts_OnSendingData(string data)
        {
            toolStripStatusLabel1.Text = data;
        }
        private string GetReturnBytes()
        {
            string detectorState = "0000" + GetDetectorState(checkBox4) +
                GetDetectorState(checkBox3) +
                GetDetectorState(checkBox2) +
                GetDetectorState(checkBox1);
            return Convert.ToString(Convert.ToByte(detectorState, 2));
        }
        private string GetDetectorState(CheckBox checkbox)
        {
            return checkbox.Checked ? "1" : "0";
        }
        void ts_OnException(string message, string stacktrace)
        {
            MessageBox.Show(message + "\n" + stacktrace);
        }


        private string GetIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
        private void EnableControls(bool state)
        {
            tbPort.Enabled = tbDetectorID.Enabled = state;
        }

        private void btnStartListening_Click(object sender, EventArgs e)
        {
            //if (btnStartListening.Text == "开始侦听")
            //{
            //    btnStartListening.Text = "停止";
            //    EnableControls(false);
            //    Thread listeningThread = new Thread(ts.StartListening);
            //    listeningThread.Start();
            //}
            //else
            //{
            //    btnStartListening.Text = "开始侦听";
            //    EnableControls(true);
            //    ts.StopListening();
            //}
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            ts.ReturnBytes = GetReturnBytes();
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ts != null && ts.IsListening)
            {
                ts.StopListening();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ts.SendData(tbServerAddress.Text,Int32.Parse(tbPort.Text));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ts.ServerAddress = GetIPAddress();
            ts.Port = "502";
            if (btnStartListening.Text == "开始侦听")
            {
                btnStartListening.Text = "停止";
                EnableControls(false);
                Thread listeningThread = new Thread(ts.StartListening);
                listeningThread.Start();
            }
            else
            {
                btnStartListening.Text = "开始侦听";
                EnableControls(true);
                ts.StopListening();
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            ts.SendEIOData(tbServerAddress.Text,Int32.Parse(tbPort.Text), "00 06 98 97 02 44 10 00 00 00 00 01 00 00 00 00");
        }
    }
}
