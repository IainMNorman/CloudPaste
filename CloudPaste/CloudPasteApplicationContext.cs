using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using GlobalHotKey;
using System.IO;
using System.Drawing.Imaging;
using Renci.SshNet;
using System.Threading;
using System.Windows.Input;
using CloudPaste.Properties;

namespace CloudPaste
{
    class CloudPasteApplicationContext : ApplicationContext
    {
        //Component declarations
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayIconContextMenu;
        private ToolStripMenuItem CloseMenuItem;
        private HotKeyManager HotKeyManager;

        public CloudPasteApplicationContext()
        {
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            InitializeComponent();
            TrayIcon.Visible = true;
        }

        private void InitializeComponent()
        {
            TrayIcon = new NotifyIcon();
            TrayIcon.Icon = Properties.Resources.TrayIcon;
            TrayIconContextMenu = new ContextMenuStrip();
            CloseMenuItem = new ToolStripMenuItem();
            TrayIconContextMenu.SuspendLayout();

            // 
            // TrayIconContextMenu
            // 
            this.TrayIconContextMenu.Items.AddRange(new ToolStripItem[] {
            this.CloseMenuItem});
            this.TrayIconContextMenu.Name = "TrayIconContextMenu";
            this.TrayIconContextMenu.Size = new Size(153, 70);
            // 
            // CloseMenuItem
            // 
            this.CloseMenuItem.Name = "CloseMenuItem";
            this.CloseMenuItem.Size = new Size(152, 22);
            this.CloseMenuItem.Text = "Exit CloudPaste";
            this.CloseMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);

            TrayIconContextMenu.ResumeLayout(false);
            TrayIcon.ContextMenuStrip = TrayIconContextMenu;

            HotKeyManager = new HotKeyManager();
            var hotKey = new HotKey() { Key = System.Windows.Input.Key.PrintScreen, Modifiers = ModifierKeys.Alt | ModifierKeys.Shift };
            HotKeyManager.Register(hotKey);
            HotKeyManager.KeyPressed += HotKeyManager_KeyPressed;
        }

        private void HotKeyManager_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                ShowBalloon("CloudPaste Starting", "Image detected, uploading...", ToolTipIcon.Info, 1000);
                using (MemoryStream ms = new MemoryStream())
                {
                    var image = Clipboard.GetImage();
                    image.Save(ms, ImageFormat.Png);
                    var bytes = ms.ToArray();
                    var sftp = new SftpClient(Settings.Default.FTPServer, Settings.Default.Username, Settings.Default.Password);
                    var fileName = Guid.NewGuid().ToString().Replace("-", "") + ".png";

                    
                    try
                    {
                        sftp.Connect();
                        using (Stream stm = new MemoryStream(bytes))
                        {
                            try
                            {
                                sftp.UploadFile(stm, "/home/teknohippy/teknohippy.net/j/u/" + fileName);
                                Clipboard.SetText(Settings.Default.FTPPath + fileName);
                                ShowBalloon("CloudPaste", "Upload finished, link in clipboard", ToolTipIcon.Info);

                            }
                            catch (Exception)
                            {
                                ShowBalloon("CloudPaste Upload Error", "Something went wrong", ToolTipIcon.Error);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        ShowBalloon("CloudPaste Upload Error", "Something went wrong", ToolTipIcon.Error);
                    }
                    finally
                    {
                        sftp.Disconnect();
                        sftp.Dispose();
                    }




                }
            }
            else
            {
                ShowBalloon("CloudPaste Error", "No image in clipboard.", ToolTipIcon.Error);
            }
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.None, int timeOut = 5000)
        {
            TrayIcon.BalloonTipIcon = icon;
            TrayIcon.BalloonTipTitle = title;
            TrayIcon.BalloonTipText = text;
            TrayIcon.ShowBalloonTip(timeOut);
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            TrayIcon.Visible = false;
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            ShowBalloon("CloudPaste Closing", "CloudPaste is exiting", ToolTipIcon.Info, 2000);
            Thread.Sleep(2000);
            Application.Exit();
        }
    }
}

