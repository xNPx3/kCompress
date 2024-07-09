using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Squirrel;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace kCompress
{
    public partial class Form1 : Form
    {
        readonly string n = Environment.NewLine;

        VideoSettings vs_original;
        VideoSettings vs_video;
        VideoResolution[] videoResolutions;

        string videoPath = "";
        string outputPath = "";

        bool browseOK = false;
        bool vLoaded = false;
        bool acceptData = true;

        int compressOutputLength = 0;
        string compressOutputLines = "";

        long targetBitrate = 0;
        int crfValue = 30;

        DateTime startTime = DateTime.UnixEpoch;

        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        public Form1()
        {
            InitializeComponent();
            Icon = Resources.video_1024x1024;
            statusStrip1.Padding = new Padding(statusStrip1.Padding.Left, statusStrip1.Padding.Top, statusStrip1.Padding.Left, statusStrip1.Padding.Bottom);

            videoResolutions = new VideoResolution[]
            {
                // 4:3
                new VideoResolution(800, 600),
                new VideoResolution(1280, 960),
                new VideoResolution(1440, 1080),
                new VideoResolution(1920, 1440),

                // 16:9
                new VideoResolution(1280, 720),
                new VideoResolution(1920, 1080),
                new VideoResolution(2560, 1440),
            };
            var data = videoResolutions.ToList();

            presetComboBox.DataSource = data;
            presetComboBox.DisplayMember = "Text";
            presetComboBox.SelectedItem = data.Find(v => v.Width == vs_video.Width && v.Height == vs_video.Height)
                                         ?? data.Find(v => v.Width == 1920 && v.Height == 1080);

            richTextBox1.GotFocus += (s, e) => { HideCaret(richTextBox1.Handle); };
            richTextBox1.SelectionChanged += (s, e) => { HideCaret(richTextBox1.Handle); };

#if (!DEBUG)
            CheckForUpdates();
#endif
        }

        public static async void CheckForUpdates()
        {
            try
            {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/xNPx3/QCompress"))
                {
                    var release = await mgr.UpdateApp();
                }
            }
            catch (Exception ex)
            {
                string message = "UpdateManager:" + Environment.NewLine + ex.Message + Environment.NewLine;
                if (ex.InnerException != null)
                    message += ex.InnerException.Message;
                MessageBox.Show(message);
            }
        }

        void ChangeBitrate()
        {
            if (browseOK && vLoaded)
            {
                decimal targetFileSize = targetFSize.Value;
                long m = 1000000;
                long mebi = 1048576;

                targetBitrate = Convert.ToInt64(vs_video.Width * vs_video.Height * vs_video.Framerate * 0.14m);
                long estFileSize = Convert.ToInt64(targetBitrate * vs_video.Length / 8);
                if (estFileSize > targetFileSize * mebi)
                {
                    targetBitrate = Convert.ToInt64(targetFileSize * 7 * mebi / vs_video.Length);
                    estFileSize = Convert.ToInt64(targetBitrate * vs_video.Length / 7);
                }

                Debug.WriteLine($"Estimated {estFileSize} B with bitrate {Convert.ToInt64(targetBitrate / 1000L)} kbit/s");
                label13.Text = $"{Convert.ToInt64(targetBitrate / 1000L)} kbit/s";
                label14.Text = $"{Convert.ToInt64(estFileSize / mebi)} MiB";
                label16.Text = crfValue.ToString();
            }
        }

        void BrowseVideoFile()
        {
            DialogResult result = openFileDialog1.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                //statusLabel.Text = "Status: LOADING";
                videoPath = openFileDialog1.FileName;
                InputVideo();
            }
        }

        void InputVideo()
        {
            Invoke(delegate
            {
                //statusLabel.Text = "Status: LOADING";
            });

            this.Text = "kCompress - " + videoPath;
            browseOK = true;
            LoadVideo();
        }

        void LoadVideo()
        {
            if (!browseOK)
            {
                MessageBox.Show("You have to select a video!", "Video not selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get video info
            JsonNode node = JsonNode.Parse(FF("ffprobe", $"-v error -select_streams v:0 -count_packets -show_streams -of json \"{videoPath}\"").ReadToEnd())!;

            Dictionary<string, object> data = node["streams"]![0].Deserialize<Dictionary<string, object>>()!;
            data.Remove("tags");
            data.Remove("disposition");
            dataGridView1.DataSource = (from row in data select new { Item = row.Key.ToUpper(), Price = row.Value }).ToArray();

            JsonNode s0 = node["streams"]![0]!;
            string[] _fr = s0["r_frame_rate"]!.ToString().Split('/', 2);
            int _es = int.Parse(s0["duration"]!.ToString().Split(new char[] { '.', ',' })[0]);
            vs_original = new VideoSettings()
            {
                Width = int.Parse(s0["width"]!.ToString()),
                Height = int.Parse(s0["height"]!.ToString()),
                EndSeconds = _es,
                Length = _es,
                End = TimeSpan.FromSeconds(_es),
                Framerate = (int)(decimal.Parse(_fr[0]) / decimal.Parse(_fr[1]))
            };
            vs_video = vs_original;

            toolStripProgressBar1.Maximum = int.Parse(s0["nb_read_packets"]!.ToString());

            DataTable dt = new DataTable();
            dt.Columns.Add("Key");
            dt.Columns.Add("Value");
            foreach (PropertyInfo p in typeof(VideoSettings).GetProperties())
            {
                if (p.Name != "StartSeconds" && p.Name != "EndSeconds" && p.Name != "AudioMuted")
                    dt.Rows.Add(p.Name, p.GetValue(vs_original));
            }

            dataGridView2.DataSource = dt;

            // Output thumbnail to stdout
            Image thumb = Image.FromStream(FF("ffmpeg", $"-y -i \"{videoPath}\" -vf \"select=eq(n\\,1)\" -update true -vframes 1 -c:v png -f image2pipe -").BaseStream);
            pictureBox1.Image = thumb;

            saveFileDialog1.InitialDirectory = Path.GetDirectoryName(videoPath);
            saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(videoPath) + "_kc" + Path.GetExtension(videoPath);

            richTextBox1.AppendText($"[VIDEO LOADED]");

            vLoaded = true;
            ChangeBitrate();
            DataToUI();
        }

        private void DataToUI()
        {
            checkBox1.Checked = vs_video.AudioMuted;
            trackBar1.Value = (int)(vs_video.AudioVolume * 10m);

            widthUpDown.Value = vs_video.Width;
            heightUpDown.Value = vs_video.Height;

            trimStartMTextBox.Text = vs_video.Start.ToString("mmss");
            trimEndMTextBox.Text = vs_video.End.ToString("mmss");
            numericUpDown1.Value = vs_video.Framerate;

            presetComboBox.SelectedItem = videoResolutions.ToList().Find(v => v.Width == vs_video.Width && v.Height == vs_video.Height);
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            pictureBox1.BackColor = SystemColors.ControlLight;
            if (e.Data != null)
            {
                var input = e.Data.GetData(DataFormats.FileDrop);
                if (e.Data != null && input != null)
                {
                    videoPath = ((string[])input)[0];
                    InputVideo();
                }
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    pictureBox1.BackColor = SystemColors.ActiveCaption;
                    e.Effect = DragDropEffects.Copy;
                }
            }
        }

        private void Form1_DragLeave(object sender, EventArgs e)
        {
            pictureBox1.BackColor = SystemColors.ControlLight;
        }

        private void Unfocus_Click(object sender, EventArgs e)
        {
            tabPage2.Focus();
            ActiveControl = tabPage2;
            Validate();
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e) => pictureBox1.BackColor = SystemColors.ActiveCaption;
        private void pictureBox1_MouseLeave(object sender, EventArgs e) => pictureBox1.BackColor = SystemColors.ControlLight;

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            decimal value = trackBar1.Value / 10m;
            checkBox1.Checked = value == 0;
            volumeLabel.Text = value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            BrowseVideoFile();
        }

        private void targetFSize_ValueChanged(object sender, EventArgs e) => ChangeBitrate();

        private void tabPage2_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine("validating...");
            if (vLoaded)
            {
                if ((int)widthUpDown.Value % 2 == 1)
                    widthUpDown.Value += 1;
                if ((int)heightUpDown.Value % 2 == 1)
                    heightUpDown.Value += 1;

                double _s = TimeSpan.Parse("00:" + trimStartMTextBox.Text).TotalSeconds;
                double _e = TimeSpan.Parse("00:" + trimEndMTextBox.Text).TotalSeconds;

                if (_s > vs_original.EndSeconds)
                    trimStartMTextBox.Text = "";
                if (_e > vs_original.EndSeconds)
                    trimEndMTextBox.Text = vs_original.End.ToString("mmss");

                if (_s > _e)
                    trimStartMTextBox.Text = "";
                if (_e < _s)
                    trimEndMTextBox.Text = "";

                if (checkBox1.Checked)
                {
                    trackBar1.Value = 0;
                    volumeLabel.Text = "0.0";
                }

                presetComboBox.SelectedItem = videoResolutions.ToList().Find(v => v.Width == widthUpDown.Value && v.Height == heightUpDown.Value);
            }
        }

        private void tabPage2_Validated(object sender, EventArgs e)
        {
            Debug.WriteLine("validated");
            if (vLoaded)
            {
                vs_video.AudioMuted = checkBox1.Checked;
                vs_video.AudioVolume = trackBar1.Value / 10m;

                vs_video.Width = (int)widthUpDown.Value;
                vs_video.Height = (int)heightUpDown.Value;

                vs_video.Start = TimeSpan.Parse("00:" + trimStartMTextBox.Text);
                vs_video.End = TimeSpan.Parse("00:" + trimEndMTextBox.Text);

                vs_video.StartSeconds = (int)vs_video.Start.TotalSeconds;
                vs_video.EndSeconds = (int)vs_video.End.TotalSeconds;

                vs_video.Length = vs_video.EndSeconds - vs_video.StartSeconds;

                vs_video.Framerate = (int)numericUpDown1.Value;
            }
        }

        private void presetComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            widthUpDown.Value = ((VideoResolution)presetComboBox.SelectedItem!).Width;
            heightUpDown.Value = ((VideoResolution)presetComboBox.SelectedItem!).Height;
        }

        private void compressBtn_Click(object sender, EventArgs e)
        {
            if (!vLoaded)
            {
                MessageBox.Show("You have to load a video first!", "Video not loaded", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Debug.WriteLine(saveFileDialog1.FileName);
                outputPath = saveFileDialog1.FileName;
            }
            else return;

            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
            compressBtn.Enabled = false;

            // Set custom output resolution if enabled
            string scale = "";
            if (vs_video.Width != vs_original.Width || vs_video.Height != vs_original.Height)
                scale = $"-vf scale={vs_video.Width}:{vs_video.Height},setsar=1:1 ";

            string muteAudio = "";
            if (vs_video.AudioMuted)
                muteAudio = "-an ";

            string framerate = "";
            if (vs_video.Framerate != vs_original.Framerate)
                framerate = $"-filter:v fps={vs_video.Framerate} ";

            string trimStart = "";
            string trimEnd = "";
            if (vs_video.Start != vs_original.Start || vs_video.End != vs_original.End)
            {
                trimStart = $"-ss {vs_video.Start:mm':'ss} ";
                trimEnd = $"-to {vs_video.End:mm':'ss} ";
            }

            string volume = "";
            decimal value = trackBar1.Value / 10m;
            string v = value.ToString("0.0", CultureInfo.InvariantCulture);
            if (vs_video.AudioVolume != 1)
                volume = $"-af \"volume={v}\" ";

            string compression = $"-b:v {Convert.ToInt64(targetBitrate / 1000L)}k ";
            if (radioButton2.Checked)
                compression = $"-crf {crfValue} ";

            string[] presets = {
                "veryslow",
                "slower",
                "slow",
                "medium",
                "fast",
                "faster",
                "veryfast",
                "superfast",
                "ultrafast",
            };
            string preset = $"-preset {presets[trackBar2.Value]} ";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Join(AppSettings.Default.FFmpegPath, "ffmpeg.exe"),
                Arguments = $"-y {trimStart}{trimEnd}-i \"{videoPath}\" {scale}{muteAudio}{volume}{framerate}-vcodec libx264 {compression}{preset}-loglevel error -progress - -nostats \"{outputPath}\"",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            Debug.WriteLine($"Running {startInfo.FileName} {startInfo.Arguments}");
            Process? p = Process.Start(startInfo);
            if (p != null)
            {
                p.OutputDataReceived += P_OutputDataReceived;
                p.ErrorDataReceived += P_ErrorDataReceived;
                startTime = DateTime.Now;
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                //richTextBox1.AppendText(p.StandardError.ReadToEnd());
            }

        }

        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                acceptData = false;
                Invoke(delegate
                {
                    richTextBox1.AppendText($"[ERROR] {e.Data}");
                });
            }
        }

        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //Debug.WriteLine(e.Data);

            if (!acceptData)
                return;

            Invoke(delegate
            {
                toolStripStatusLabel2.Text = (DateTime.Now - startTime).ToString("mm':'ss", CultureInfo.InvariantCulture);
            });

            compressOutputLength += 1;

            if (compressOutputLength < 12)
            {
                compressOutputLines += e.Data + "\n";
            }
            else
            {
                compressOutputLines += e.Data;

                richTextBox1.Invoke(delegate
                {
                    richTextBox1.Text = $"--------------------{n}COMPRESSION STARTED{n}--------------------{n}Time elapsed: {DateTime.Now - startTime:hh':'mm':'ss'.'fff}{n}{compressOutputLines}";
                });

                compressOutputLines = "";
                compressOutputLength = 0;
            }

            if (e.Data != null)
            {
                if (e.Data.StartsWith("frame="))
                {
                    int frame = int.Parse(e.Data.Replace("frame=", ""));
                    Invoke(delegate
                    {
                        if (frame <= toolStripProgressBar1.Maximum)
                        {
                            toolStripProgressBar1.Value = frame;
                        }
                    });
                }
                else if (e.Data.StartsWith("progress=end"))
                {
                    if (((Process)sender).WaitForExit(1000))
                    {
                        SystemSounds.Beep.Play();
                        Invoke(delegate
                        {
                            compressBtn.Enabled = true;
                            //groupBox1.Enabled = true;
                            //groupBox3.Enabled = true;
                            //statusLabel.Text = "Status: DONE";
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            richTextBox1.AppendText($"{n}--------------------{n}COMPRESSION FINISHED{n}--------------------{n}Output file:{n}{Path.GetFileName(outputPath)} in {Path.GetDirectoryName(outputPath)!}");
                        });
                    }
                }
            }
        }

        private void dataGridView3_Paint(object sender, PaintEventArgs e)
        {
            if (vLoaded)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Key");
                dt.Columns.Add("Value");
                foreach (PropertyInfo p in typeof(VideoSettings).GetProperties())
                {
                    dt.Rows.Add(p.Name, p.GetValue(vs_video));
                }

                dt.Rows.Add("CompressionMode", radioButton1.Checked ? "Bitrate" : "CRF");
                dt.Rows.Add("TargetSize", targetFSize.Value);

                dataGridView3.DataSource = dt;
            }
        }
    }
}
