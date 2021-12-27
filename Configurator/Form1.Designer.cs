namespace Configurator
{
	partial class Form1
	{
		/// <summary>
		/// Обязательная переменная конструктора.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Освободить все используемые ресурсы.
		/// </summary>
		/// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Код, автоматически созданный конструктором форм Windows

		/// <summary>
		/// Требуемый метод для поддержки конструктора — не изменяйте 
		/// содержимое этого метода с помощью редактора кода.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.name = new System.Windows.Forms.TextBox();
			this.rtsp_uri = new System.Windows.Forms.TextBox();
			this.transport_tcp = new System.Windows.Forms.CheckBox();
			this.audio_enabled = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.cameraList = new System.Windows.Forms.ListBox();
			this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
			this.label6 = new System.Windows.Forms.Label();
			this.addCam = new System.Windows.Forms.Button();
			this.delCam = new System.Windows.Forms.Button();
			this.saveCam = new System.Windows.Forms.Button();
			this.about = new System.Windows.Forms.Button();
			this.key = new System.Windows.Forms.TextBox();
			this.saveCommon = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.links = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.hls_allow_video_seek_back = new System.Windows.Forms.CheckBox();
			this.capture_cache_expire = new System.Windows.Forms.NumericUpDown();
			this.label13 = new System.Windows.Forms.Label();
			this.hls_cleanup_before_play = new System.Windows.Forms.CheckBox();
			this.label12 = new System.Windows.Forms.Label();
			this.server_hostname = new System.Windows.Forms.TextBox();
			this.hls_target_chunks = new System.Windows.Forms.NumericUpDown();
			this.label11 = new System.Windows.Forms.Label();
			this.https_mode_on = new System.Windows.Forms.CheckBox();
			this.label10 = new System.Windows.Forms.Label();
			this.cert_password = new System.Windows.Forms.TextBox();
			this.port = new System.Windows.Forms.NumericUpDown();
			this.label9 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.capture_cache_expire)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.hls_target_chunks)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.port)).BeginInit();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// name
			// 
			this.name.Enabled = false;
			this.name.Location = new System.Drawing.Point(21, 57);
			this.name.Name = "name";
			this.name.Size = new System.Drawing.Size(481, 21);
			this.name.TabIndex = 0;
			// 
			// rtsp_uri
			// 
			this.rtsp_uri.Enabled = false;
			this.rtsp_uri.Location = new System.Drawing.Point(21, 121);
			this.rtsp_uri.Name = "rtsp_uri";
			this.rtsp_uri.Size = new System.Drawing.Size(481, 21);
			this.rtsp_uri.TabIndex = 1;
			// 
			// transport_tcp
			// 
			this.transport_tcp.AutoSize = true;
			this.transport_tcp.Checked = true;
			this.transport_tcp.CheckState = System.Windows.Forms.CheckState.Checked;
			this.transport_tcp.Enabled = false;
			this.transport_tcp.Location = new System.Drawing.Point(21, 197);
			this.transport_tcp.Name = "transport_tcp";
			this.transport_tcp.Size = new System.Drawing.Size(15, 14);
			this.transport_tcp.TabIndex = 2;
			this.transport_tcp.UseVisualStyleBackColor = true;
			// 
			// audio_enabled
			// 
			this.audio_enabled.AutoSize = true;
			this.audio_enabled.Enabled = false;
			this.audio_enabled.Location = new System.Drawing.Point(21, 167);
			this.audio_enabled.Name = "audio_enabled";
			this.audio_enabled.Size = new System.Drawing.Size(15, 14);
			this.audio_enabled.TabIndex = 3;
			this.audio_enabled.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(18, 33);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(110, 16);
			this.label1.TabIndex = 4;
			this.label1.Text = "Наименование:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(18, 97);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(98, 16);
			this.label2.TabIndex = 4;
			this.label2.Text = "RTSP-ссылка:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(42, 195);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(238, 16);
			this.label3.TabIndex = 4;
			this.label3.Text = "Использовать TCP для транспорта";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(42, 165);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(115, 16);
			this.label4.TabIndex = 4;
			this.label4.Text = "Включить аудио";
			// 
			// label5
			// 
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label5.Location = new System.Drawing.Point(18, 322);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(484, 52);
			this.label5.TabIndex = 4;
			this.label5.Text = "Примечание. Включение аудио в камере без аудио может привести к невозможности про" +
    "смотра. Использование транспорта TCP обычно повышает надежность передачи, выключ" +
    "ите при возникновении проблем.";
			// 
			// cameraList
			// 
			this.cameraList.DataSource = this.bindingSource1;
			this.cameraList.FormattingEnabled = true;
			this.cameraList.IntegralHeight = false;
			this.cameraList.ItemHeight = 15;
			this.cameraList.Location = new System.Drawing.Point(17, 37);
			this.cameraList.Name = "cameraList";
			this.cameraList.Size = new System.Drawing.Size(320, 321);
			this.cameraList.TabIndex = 5;
			this.cameraList.SelectedIndexChanged += new System.EventHandler(this.cameraList_SelectedIndexChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(17, 13);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(101, 16);
			this.label6.TabIndex = 6;
			this.label6.Text = "Список камер:";
			// 
			// addCam
			// 
			this.addCam.Location = new System.Drawing.Point(17, 364);
			this.addCam.Name = "addCam";
			this.addCam.Size = new System.Drawing.Size(153, 34);
			this.addCam.TabIndex = 7;
			this.addCam.Text = "Добавить";
			this.addCam.UseVisualStyleBackColor = true;
			this.addCam.Click += new System.EventHandler(this.addCam_Click);
			// 
			// delCam
			// 
			this.delCam.Location = new System.Drawing.Point(184, 364);
			this.delCam.Name = "delCam";
			this.delCam.Size = new System.Drawing.Size(153, 34);
			this.delCam.TabIndex = 8;
			this.delCam.Text = "Удалить";
			this.delCam.UseVisualStyleBackColor = true;
			this.delCam.Click += new System.EventHandler(this.delCam_Click);
			// 
			// saveCam
			// 
			this.saveCam.Enabled = false;
			this.saveCam.Location = new System.Drawing.Point(21, 239);
			this.saveCam.Name = "saveCam";
			this.saveCam.Size = new System.Drawing.Size(250, 34);
			this.saveCam.TabIndex = 9;
			this.saveCam.Text = "Сохранить настройки камеры";
			this.saveCam.UseVisualStyleBackColor = true;
			this.saveCam.Click += new System.EventHandler(this.saveCam_Click);
			// 
			// about
			// 
			this.about.Location = new System.Drawing.Point(965, 628);
			this.about.Name = "about";
			this.about.Size = new System.Drawing.Size(204, 34);
			this.about.TabIndex = 10;
			this.about.Text = "О программе...";
			this.about.UseVisualStyleBackColor = true;
			this.about.Click += new System.EventHandler(this.about_Click);
			// 
			// key
			// 
			this.key.Location = new System.Drawing.Point(22, 69);
			this.key.Name = "key";
			this.key.Size = new System.Drawing.Size(250, 21);
			this.key.TabIndex = 11;
			// 
			// saveCommon
			// 
			this.saveCommon.Location = new System.Drawing.Point(22, 550);
			this.saveCommon.Name = "saveCommon";
			this.saveCommon.Size = new System.Drawing.Size(250, 34);
			this.saveCommon.TabIndex = 12;
			this.saveCommon.Text = "Сохранить общие параметры";
			this.saveCommon.UseVisualStyleBackColor = true;
			this.saveCommon.Click += new System.EventHandler(this.saveCommon_Click);
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(19, 19);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(253, 41);
			this.label7.TabIndex = 13;
			this.label7.Text = "Общий ключ доступа (цифры и латинские буквы):";
			this.label7.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// links
			// 
			this.links.Location = new System.Drawing.Point(17, 447);
			this.links.Multiline = true;
			this.links.Name = "links";
			this.links.ReadOnly = true;
			this.links.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.links.Size = new System.Drawing.Size(850, 215);
			this.links.TabIndex = 14;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(17, 423);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(420, 16);
			this.label8.TabIndex = 15;
			this.label8.Text = "Ниже приведены ссылки на камеры для просмотра в браузере:";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.hls_allow_video_seek_back);
			this.groupBox1.Controls.Add(this.capture_cache_expire);
			this.groupBox1.Controls.Add(this.label13);
			this.groupBox1.Controls.Add(this.hls_cleanup_before_play);
			this.groupBox1.Controls.Add(this.label12);
			this.groupBox1.Controls.Add(this.server_hostname);
			this.groupBox1.Controls.Add(this.hls_target_chunks);
			this.groupBox1.Controls.Add(this.label11);
			this.groupBox1.Controls.Add(this.https_mode_on);
			this.groupBox1.Controls.Add(this.label10);
			this.groupBox1.Controls.Add(this.cert_password);
			this.groupBox1.Controls.Add(this.port);
			this.groupBox1.Controls.Add(this.label9);
			this.groupBox1.Controls.Add(this.label7);
			this.groupBox1.Controls.Add(this.key);
			this.groupBox1.Controls.Add(this.saveCommon);
			this.groupBox1.Location = new System.Drawing.Point(873, 13);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(296, 602);
			this.groupBox1.TabIndex = 16;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Общие параметры";
			// 
			// hls_allow_video_seek_back
			// 
			this.hls_allow_video_seek_back.Location = new System.Drawing.Point(22, 497);
			this.hls_allow_video_seek_back.Name = "hls_allow_video_seek_back";
			this.hls_allow_video_seek_back.Size = new System.Drawing.Size(250, 34);
			this.hls_allow_video_seek_back.TabIndex = 27;
			this.hls_allow_video_seek_back.Text = "Разрешить перемотку назад";
			this.hls_allow_video_seek_back.UseVisualStyleBackColor = true;
			// 
			// capture_cache_expire
			// 
			this.capture_cache_expire.Location = new System.Drawing.Point(205, 388);
			this.capture_cache_expire.Maximum = new decimal(new int[] {
            3600,
            0,
            0,
            0});
			this.capture_cache_expire.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.capture_cache_expire.Name = "capture_cache_expire";
			this.capture_cache_expire.Size = new System.Drawing.Size(67, 21);
			this.capture_cache_expire.TabIndex = 26;
			this.capture_cache_expire.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
			// 
			// label13
			// 
			this.label13.Location = new System.Drawing.Point(22, 376);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(177, 43);
			this.label13.TabIndex = 25;
			this.label13.Text = "Время хранения снимков в кэше, секунд:";
			this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// hls_cleanup_before_play
			// 
			this.hls_cleanup_before_play.Location = new System.Drawing.Point(22, 450);
			this.hls_cleanup_before_play.Name = "hls_cleanup_before_play";
			this.hls_cleanup_before_play.Size = new System.Drawing.Size(250, 49);
			this.hls_cleanup_before_play.TabIndex = 24;
			this.hls_cleanup_before_play.Text = "Не показывать старые сегменты в начале воспроизведения";
			this.hls_cleanup_before_play.UseVisualStyleBackColor = true;
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(22, 93);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(250, 41);
			this.label12.TabIndex = 23;
			this.label12.Text = "Адрес сервера (IP или доменное имя):";
			this.label12.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// server_hostname
			// 
			this.server_hostname.Location = new System.Drawing.Point(22, 143);
			this.server_hostname.Name = "server_hostname";
			this.server_hostname.Size = new System.Drawing.Size(250, 21);
			this.server_hostname.TabIndex = 22;
			// 
			// hls_target_chunks
			// 
			this.hls_target_chunks.Location = new System.Drawing.Point(205, 425);
			this.hls_target_chunks.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
			this.hls_target_chunks.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
			this.hls_target_chunks.Name = "hls_target_chunks";
			this.hls_target_chunks.Size = new System.Drawing.Size(67, 21);
			this.hls_target_chunks.TabIndex = 21;
			this.hls_target_chunks.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(22, 421);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(177, 29);
			this.label11.TabIndex = 20;
			this.label11.Text = "Сегментов в плейлисте:";
			this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// https_mode_on
			// 
			this.https_mode_on.Location = new System.Drawing.Point(22, 207);
			this.https_mode_on.Name = "https_mode_on";
			this.https_mode_on.Size = new System.Drawing.Size(250, 67);
			this.https_mode_on.TabIndex = 19;
			this.https_mode_on.Text = "Режим защищенного соединения SSL (требуется установка сертификата)";
			this.https_mode_on.UseVisualStyleBackColor = true;
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(19, 261);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(253, 47);
			this.label10.TabIndex = 18;
			this.label10.Text = "Пароль на файл сертификата cert.pfx (если используется этот формат):";
			this.label10.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// cert_password
			// 
			this.cert_password.Location = new System.Drawing.Point(22, 317);
			this.cert_password.Name = "cert_password";
			this.cert_password.Size = new System.Drawing.Size(250, 21);
			this.cert_password.TabIndex = 17;
			// 
			// port
			// 
			this.port.Location = new System.Drawing.Point(143, 177);
			this.port.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.port.Name = "port";
			this.port.Size = new System.Drawing.Size(129, 21);
			this.port.TabIndex = 16;
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(19, 173);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(118, 29);
			this.label9.TabIndex = 15;
			this.label9.Text = "Порт сервера:";
			this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.name);
			this.groupBox2.Controls.Add(this.rtsp_uri);
			this.groupBox2.Controls.Add(this.transport_tcp);
			this.groupBox2.Controls.Add(this.audio_enabled);
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.saveCam);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Location = new System.Drawing.Point(343, 13);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(524, 385);
			this.groupBox2.TabIndex = 17;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Параметры камеры";
			// 
			// toolTip1
			// 
			this.toolTip1.AutoPopDelay = 25000;
			this.toolTip1.InitialDelay = 500;
			this.toolTip1.ReshowDelay = 100;
			this.toolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			this.toolTip1.ToolTipTitle = "Пояснение";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1188, 674);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.links);
			this.Controls.Add(this.about);
			this.Controls.Add(this.delCam);
			this.Controls.Add(this.addCam);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.cameraList);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Конфигуратор";
			((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.capture_cache_expire)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.hls_target_chunks)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.port)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox name;
		private System.Windows.Forms.TextBox rtsp_uri;
		private System.Windows.Forms.CheckBox transport_tcp;
		private System.Windows.Forms.CheckBox audio_enabled;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ListBox cameraList;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Button addCam;
		private System.Windows.Forms.Button delCam;
		private System.Windows.Forms.Button saveCam;
		private System.Windows.Forms.Button about;
		private System.Windows.Forms.TextBox key;
		private System.Windows.Forms.Button saveCommon;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.BindingSource bindingSource1;
		private System.Windows.Forms.TextBox links;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.NumericUpDown port;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.TextBox cert_password;
		private System.Windows.Forms.CheckBox https_mode_on;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.NumericUpDown hls_target_chunks;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TextBox server_hostname;
		private System.Windows.Forms.CheckBox hls_cleanup_before_play;
		private System.Windows.Forms.NumericUpDown capture_cache_expire;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.CheckBox hls_allow_video_seek_back;
	}
}

