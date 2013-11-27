using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace rhel {
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
		}

		private string getAccessToken(string username, string password) {
			const string uri = "https://login.eveonline.com/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%2F%3Fclient_id%3DeveLauncherTQ%26lang%3Den%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Flogin.eveonline.com%2Flauncher%3Fclient_id%3DeveLauncherTQ%26scope%3DeveClientToken";
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
			req.AllowAutoRedirect = true;
			req.Headers.Add("Origin", "https://login.eveonline.com");
			req.Referer = uri;
			req.CookieContainer = new CookieContainer(8);
			req.Method = "POST";
			req.ContentType = "application/x-www-form-urlencoded";
			byte[] body = Encoding.ASCII.GetBytes(String.Format("UserName={0}&Password={1}", username, password));
			req.ContentLength = body.Length;
			Stream reqStream = req.GetRequestStream();
			reqStream.Write(body, 0, body.Length);
			reqStream.Close();
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
			// https://login.eveonline.com/launcher?client_id=eveLauncherTQ#access_token=...&token_type=Bearer&expires_in=43200
			string accessToken = this.extractAccessToken(resp.ResponseUri.Fragment);
			return accessToken;
		}

		private string getSSOToken(string username, string password) {
			string accessToken = this.getAccessToken(username, password);
			string uri = "https://login.eveonline.com/launcher/token?accesstoken=" + accessToken;
			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
			req.AllowAutoRedirect = false;
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
			string ssoToken = this.extractAccessToken(resp.GetResponseHeader("Location"));
			return ssoToken;
		}

		private string extractAccessToken(string urlFragment) {
			const string search = "#access_token=";
			int start = urlFragment.IndexOf(search);
			if (start == -1)
				return null;
			start += search.Length;
			string accessToken = urlFragment.Substring(start, urlFragment.IndexOf('&') - start);
			return accessToken;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			string path = null;
			string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			foreach (string dir in Directory.EnumerateDirectories(Path.Combine(appdata, "CCP", "EVE"), "*_tranquility")) {
				string[] split = dir.Split(new char[]{'_'}, 2);
				string drive = split[0].Substring(split[0].Length-1);
				path = split[1].Substring(0, split[1].Length - "_tranquility".Length).Replace('_', Path.DirectorySeparatorChar);
				path = drive.ToUpper() + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar + path;
				break;
			}
			if (path != null) {
				this.evePath.Text = path;
			}
		}

		private void launch_Click(object sender, RoutedEventArgs e) {
			string ssoToken = this.getSSOToken(this.username.Text, this.password.Password);
			const string args = @"/noconsole /ssoToken={0} /triPlatform=dx11";
			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(
				@".\bin\ExeFile.exe", String.Format(args, ssoToken)
			);
			psi.WorkingDirectory = this.evePath.Text;
			System.Diagnostics.Process.Start(psi);
		}
	}
}
