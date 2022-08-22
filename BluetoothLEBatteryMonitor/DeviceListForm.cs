﻿using BluetoothLEBatteryMonitor.Service;
using System;
using System.Collections.Concurrent;
using System.Windows.Forms;
using Windows.Devices.Enumeration;

namespace BluetoothLEBatteryMonitor
{
    public partial class DeviceListForm : Form
    {
        public DeviceListForm()
        {
            InitializeComponent();
        }

        private void DeviceListForm_Load(object sender, EventArgs e)

        {
            ConcurrentDictionary<string, DeviceInformation> deviceInformationDict = DeviceManager.Instance.DeviceInformationDict;
            string aqsFilter = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
            string[] bleAdditionalProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.Bluetooth.Le.IsConnectable", };
            DeviceWatcher watcher = DeviceInformation.CreateWatcher(aqsFilter, bleAdditionalProperties, DeviceInformationKind.AssociationEndpoint);
            watcher.Added += (DeviceWatcher deviceWatcher, DeviceInformation devInfo) =>
            {
                if (!String.IsNullOrWhiteSpace(devInfo.Name))
                {
                    deviceInformationDict.AddOrUpdate(devInfo.Id, devInfo, (k, v) => devInfo);
                }
            };
            watcher.Updated += (_, __) => { };
            watcher.EnumerationCompleted += (DeviceWatcher deviceWatcher, object arg) => { deviceWatcher.Stop(); };
            watcher.Stopped += (DeviceWatcher deviceWatcher, object arg) => { deviceWatcher.Start(); };
            watcher.Start();
            int width = DeviceListView.Width / 3;
            DeviceListView.Columns.Add("Device Name", width);
            DeviceListView.Columns.Add("Status", width);
            DeviceListView.Columns.Add("ID", width);
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            ConcurrentDictionary<string, DeviceInformation> deviceInformationDict = DeviceManager.Instance.DeviceInformationDict;
            if (!deviceInformationDict.IsEmpty)
            {
                DeviceListView.BeginUpdate();
                DeviceListView.Items.Clear();
                foreach(DeviceInformation devInfo in deviceInformationDict.Values)
                {
                    ListViewItem listViewItem = new ListViewItem
                    {
                        Text = devInfo.Name
                    };
                    listViewItem.SubItems.Add(devInfo.Pairing.IsPaired ? "Paired " : "Unpair");
                    listViewItem.SubItems.Add(devInfo.Id);
                    listViewItem.Tag = devInfo;
                    this.DeviceListView.Items.Add(listViewItem);
                }
                DeviceListView.EndUpdate();
            }
        }

        private void DeviceListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(DeviceListView.SelectedItems.Count > 0)
            {
                DeviceListView.Enabled = false;
                DeviceInformation devInfo = (DeviceInformation)DeviceListView.SelectedItems[0].Tag;
                DeviceManager.Instance.SelectDevice(devInfo.Id, this);
                DeviceListView.Enabled = true;
            }
            
        }

        private void IconTimer_Tick(object sender, EventArgs e)
        {
            ChangeIcon(DeviceManager.Instance.GetDeviceName(), DeviceManager.Instance.GetBatteryLevel(this));
        }

        private void ChangeIcon(string name, int battery)
        {
            if (battery >= 90)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_Full;
            }
            else if (battery >= 70)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_Three_Quarters;
            }
            else if (battery >= 50)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_Half;
            }
            else if (battery >= 30)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_Quarter;
            }
            else if(battery > 0)
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Battery_Empty;
            }
            else
            {
                NotifyIcon.Icon = BluetoothLEBatteryMonitor.Properties.Resources.Icon_Unlink;
                NotifyIcon.Text = "BluetoothLE Battery Monitor";
                return;
            }
            NotifyIcon.Text = String.Format("{0} {1}%\n{2} Update", name, battery, DateTime.Now.ToString());
        }

        public void StartUpdate()
        {
            Hide();
            IconTimer.Start();
            ChangeIcon(DeviceManager.Instance.GetDeviceName(), DeviceManager.Instance.GetBatteryLevel(this));
        }

        public void StopUpdate()
        {
            Notify("BLE device disconnect");
            IconTimer.Stop();
        }

        public void Notify(string message)
        {
            NotifyIcon.ShowBalloonTip(300, "BluetoothLE Battery Montior", message, ToolTipIcon.Info);
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void DeviceListForm_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState.Equals(FormWindowState.Minimized))
            {
                Hide();
            }
        }
    }
}
