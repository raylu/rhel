using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Collections.Specialized;

namespace rhel {
	public partial class MainWindow : Window {
		System.Windows.Forms.NotifyIcon tray; // yes, we're using Windows.Forms in a WPF project
		EventHandler contextMenuClick;

		public MainWindow() {
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			if (Properties.Settings.Default.evePath.Length == 0) {
				string path = null;
				string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				foreach (string dir in Directory.EnumerateDirectories(Path.Combine(appdata, "CCP", "EVE"), "*_tranquility")) {
					string[] split = dir.Split(new char[] { '_' }, 2);
					string drive = split[0].Substring(split[0].Length-1);
					path = split[1].Substring(0, split[1].Length - "_tranquility".Length).Replace('_', Path.DirectorySeparatorChar);
					path = drive.ToUpper() + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + path;
					break;
				}
				if (path != null && File.Exists(Path.Combine(path, "bin", "ExeFile.exe"))) {
					Properties.Settings.Default.evePath = path;
					Properties.Settings.Default.Save();
				}
			}
			this.txtEvePath.Text = Properties.Settings.Default.evePath;

			this.tray = new System.Windows.Forms.NotifyIcon();
			this.tray.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ResourceAssembly.Location);
			this.tray.Text = this.Title;
			this.tray.ContextMenu = new System.Windows.Forms.ContextMenu();
			this.tray.MouseClick += new System.Windows.Forms.MouseEventHandler(this.tray_Click);
			this.contextMenuClick = new EventHandler(this.contextMenu_Click);

			if (Properties.Settings.Default.accounts != null) {
				foreach (string credentials in Properties.Settings.Default.accounts) {
					Account account = new Account(this);
					string[] split = credentials.Split(new char[]{':'}, 2);
					account.username.Text = split[0];
					account.password.Password = split[1];
					this.accountsPanel.Children.Add(account);
					this.tray.ContextMenu.MenuItems.Add(split[0], this.contextMenuClick);
				}
			}

			this.tray.Visible = true;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			this.tray.Visible = false;
		}

		private void Window_StateChanged(object sender, EventArgs e) {
			this.ShowInTaskbar = (this.WindowState != System.Windows.WindowState.Minimized);
		}

		private void tray_Click(object sender, System.Windows.Forms.MouseEventArgs e) {
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
				this.WindowState = System.Windows.WindowState.Normal;
		}

		private void contextMenu_Click(object sender, EventArgs e) {
			string username = ((System.Windows.Forms.MenuItem)sender).Text;
			foreach (Account account in this.accountsPanel.Children) {
				if (account.username.Text == username) {
					account.launchAccount();
					break;
				}
			}
		}

		private void txtEvePath_LostFocus(object sender, RoutedEventArgs e) {
			this.evePath(this.txtEvePath.Text);
		}

		private void browse_Click(object sender, RoutedEventArgs e) {
			System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
			fbd.ShowNewFolderButton = false;
			fbd.SelectedPath = this.txtEvePath.Text;
			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				this.txtEvePath.Text = fbd.SelectedPath;
				this.evePath(fbd.SelectedPath);
			}
		}

		private void addAccount_Click(object sender, RoutedEventArgs e) {
			Account account = new Account(this);
			this.accountsPanel.Children.Add(account);
		}

		public string evePath() {
			return Properties.Settings.Default.evePath;
		}
		public void evePath(string path) {
			string exefilePath = Path.Combine(path, "bin", "ExeFile.exe");
			if (File.Exists(exefilePath)) {
				Properties.Settings.Default.evePath = path;
				Properties.Settings.Default.Save();
			} else
				this.showBalloon("eve path", "could not find " + exefilePath, System.Windows.Forms.ToolTipIcon.Error);
		}

		public void updateCredentials() {
			StringCollection accounts = new StringCollection();
			this.tray.ContextMenu.MenuItems.Clear();
			foreach (Account account in this.accountsPanel.Children) {
				string credentials = String.Format("{0}:{1}", account.username.Text, account.password.Password);
				accounts.Add(credentials);
				this.tray.ContextMenu.MenuItems.Add(account.username.Text, this.contextMenuClick);
			}
			Properties.Settings.Default.accounts = accounts;
			Properties.Settings.Default.Save();
		}

		public void showBalloon(string title, string text, System.Windows.Forms.ToolTipIcon icon) {
			this.tray.ShowBalloonTip(1000, title, text, icon);
		}
	}
}
