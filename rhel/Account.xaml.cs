using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Net;
/*
using System.Linq;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
*/

namespace rhel {
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class Account : UserControl {
		MainWindow main;

		public Account(MainWindow main) {
			InitializeComponent();
			this.main = main;
		}

		private void launch_Click(object sender, RoutedEventArgs e) {
			string exefilePath = Path.Combine(this.main.evePath(), "bin", "ExeFile.exe");
			if (!File.Exists(exefilePath)) {
				this.main.showBalloon("eve path", "could not find " + exefilePath, System.Windows.Forms.ToolTipIcon.Error);
				return;
			}
			string ssoToken = this.getSSOToken(this.username.Text, this.password.Password);
			if (ssoToken == null) {
				this.main.showBalloon("logging in", "invalid username/password", System.Windows.Forms.ToolTipIcon.Error);
				return;
			}
			this.main.showBalloon("logging in", "launching", System.Windows.Forms.ToolTipIcon.None);
			const string args = @"/noconsole /ssoToken={0} /triPlatform=dx11";
			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(
				@".\bin\ExeFile.exe", String.Format(args, ssoToken)
			);
			psi.WorkingDirectory = this.main.evePath();
			System.Diagnostics.Process.Start(psi);
		}

		private string getAccessToken(string username, string password) {
			this.main.showBalloon("logging in", "getting access token", System.Windows.Forms.ToolTipIcon.None);
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
			if (accessToken == null)
				return null;
			this.main.showBalloon("logging in", "getting SSO token", System.Windows.Forms.ToolTipIcon.None);
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
	}
}
