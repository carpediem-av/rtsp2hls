// Copyright (c) 2021 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CDSD.Forms;
using RTSPLiveServer;

namespace Configurator
{
	public partial class Form1 : Form
	{
		static string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string logDir = Path.Combine(appDirectory, "data", "log");

		public Form1()
		{
			InitializeComponent();
			toolTip1.SetToolTip(https_mode_on, "Для формата PEM: разместите файлы сертификата под именами cert.pem и privkey.pem в подпапке data.\r\nДля формата PFX: разместите сертификат под именем cert.pfx в подпапке data.");
			toolTip1.SetToolTip(hls_allow_video_seek_back, "При включении данной функции рекомендуется установить настройку \"сегментов в плейлисте\" в значение как минимум 3.\r\nИначе возможна большая задержка воспроизводимого видео, связанная с подгрузкой новых сегментов");

			if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
			string logFilename = Path.Combine(logDir, "configurator_log.txt");
			Logger.init(logFilename, true);

			ConfigManager.load();
			bindingSource1.DataSource = ConfigManager.Current.cameraList;
			cameraList.DisplayMember = "name";

			key.Text = ConfigManager.Current.key;
			port.Value = ConfigManager.Current.port;
			cert_password.Text = ConfigManager.Current.cert_password;
			https_mode_on.Checked = ConfigManager.Current.https_mode_on;
			hls_cleanup_before_play.Checked = ConfigManager.Current.hls_cleanup_before_play;
			hls_allow_video_seek_back.Checked = ConfigManager.Current.hls_allow_video_seek_back;
			hls_target_chunks.Value = ConfigManager.Current.hls_target_chunks;
			capture_cache_expire.Value = ConfigManager.Current.capture_cache_expire;
			server_hostname.Text = ConfigManager.Current.server_hostname;

			generateLinks();
		}

		private void saveCommon_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(key.Text) || key.Text.Length < 10) {
				MessageBox.Show("Длина ключа не может быть меньше 10!");
				return;
			}

			if (!Regex.IsMatch(key.Text, @"^[a-zA-Z0-9]+$")) {
				MessageBox.Show("Ключ может содержать только латинские буквы или цифры!");
				return;
			}

			if (string.IsNullOrWhiteSpace(server_hostname.Text)) {
				MessageBox.Show("Не задан адрес сервера!");
				return;
			}

			ConfigManager.Current.key = key.Text;
			ConfigManager.Current.port = (ushort) port.Value;
			ConfigManager.Current.cert_password = cert_password.Text;
			ConfigManager.Current.https_mode_on = https_mode_on.Checked;
			ConfigManager.Current.hls_cleanup_before_play = hls_cleanup_before_play.Checked;
			ConfigManager.Current.hls_allow_video_seek_back = hls_allow_video_seek_back.Checked;
			ConfigManager.Current.hls_target_chunks = (int) hls_target_chunks.Value;
			ConfigManager.Current.capture_cache_expire = (int)capture_cache_expire.Value;
			ConfigManager.Current.server_hostname = server_hostname.Text;

			ConfigManager.save();
			generateLinks();
		}

		private void cameraList_SelectedIndexChanged(object sender, EventArgs e)
		{
			CameraItem item = bindingSource1.Current as CameraItem;

			if (item == null) {
				name.Enabled = rtsp_uri.Enabled = audio_enabled.Enabled = transport_tcp.Enabled = saveCam.Enabled = false;
			}
			else {
				name.Enabled = rtsp_uri.Enabled = audio_enabled.Enabled = transport_tcp.Enabled = saveCam.Enabled = true;
				name.Text = item.name;
				rtsp_uri.Text = item.rtsp_uri;
				audio_enabled.Checked = item.audio_enabled;
				transport_tcp.Checked = item.transport_tcp;
			}
		}

		private void addCam_Click(object sender, EventArgs e)
		{
			CameraItem item = new CameraItem() { name = "Новая камера", transport_tcp = true };
			bindingSource1.Add(item);
			cameraList.SelectedItem = null;
			cameraList.SelectedItem = item;
		}

		private void delCam_Click(object sender, EventArgs e)
		{
			CameraItem item = bindingSource1.Current as CameraItem;
			if (item == null) return;
			bindingSource1.Remove(item);
			ConfigManager.save();
			generateLinks();
		}

		private void saveCam_Click(object sender, EventArgs e)
		{
			CameraItem item = bindingSource1.Current as CameraItem;
			if (item == null) return;

			if (string.IsNullOrWhiteSpace(name.Text)) {
				MessageBox.Show("Имя не задано!");
				return;
			}

			if (string.IsNullOrWhiteSpace(rtsp_uri.Text)) {
				MessageBox.Show("RTSP-ссылка не задана!");
				return;
			}

			item.name =  name.Text;
			item.rtsp_uri = rtsp_uri.Text;
			item.audio_enabled = audio_enabled.Checked;
			item.transport_tcp = transport_tcp.Checked;
			bindingSource1.ResetBindings(false);

			ConfigManager.save();
			generateLinks();
		}

		private void about_Click(object sender, EventArgs e)
		{
			var f = new FAbout();
			f.ShowDialog();
		}

		void generateLinks()
		{
			links.Text = "";
			var protocol = ConfigManager.Current.https_mode_on ? "https" : "http";

			foreach (CameraItem x in bindingSource1.List) {
				links.Text += $"{x.name}:\t{protocol}://{ConfigManager.Current.server_hostname}:{ConfigManager.Current.port}"
					+ $"/player/?key={ConfigManager.Current.key}&cam={x.id}{Environment.NewLine}";
			}
		}
	}
}
