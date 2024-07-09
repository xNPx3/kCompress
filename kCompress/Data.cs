using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace kCompress
{
    public partial class Form1
    {
        public class VideoResolution
        {
            public VideoResolution(int w, int h)
            {
                Width = w;
                Height = h;
                Text = Text = $"{Width} x {Height} ({Width / GCD(Width, Height)}:{Height / GCD(Width, Height)})";
            }
            public int Width { get; }
            public int Height { get; }
            public string Text { get; }
            private static int GCD(int a, int b)
            {
                while (a != 0 && b != 0)
                {
                    if (a > b)
                        a %= b;
                    else
                        b %= a;
                }

                return a | b;
            }
        }

        struct VideoSettings()
        {
            public int Width { get; set; } = 1;
            public int Height { get; set; } = 1;
            public string AspectRatio { get => $"{Width / GCD(Width, Height)}:{Height / GCD(Width, Height)}"; }
            public int StartSeconds { get; set; }
            public int EndSeconds { get; set; }
            public int Length { get; set; }
            public TimeSpan Start { get; set; }
            public TimeSpan End { get; set; }
            public int Framerate { get; set; }
            public bool AudioMuted { get; set; }
            public decimal AudioVolume { get; set; } = 1;

            public override string ToString()
            {
                string o = string.Empty;
                foreach (PropertyInfo p in typeof(VideoSettings).GetProperties())
                {
                    o += $"{p.Name}: {p.GetValue(this)}{Environment.NewLine}";
                }
                return o;
            }
            private static int GCD(int a, int b)
            {
                while (a != 0 && b != 0)
                {
                    if (a > b)
                        a %= b;
                    else
                        b %= a;
                }

                return a | b;
            }
        }

        public StreamReader FF(string exec, string args, ProcessStartInfo? info = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Join(AppSettings.Default.FFmpegPath, $"{exec}.exe"),
                Arguments = args,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            if (info != null)
                startInfo = info;

            try
            {
                Debug.WriteLine($"Running {startInfo.FileName} {startInfo.Arguments}");
                Process p = Process.Start(startInfo)!;
                return p.StandardOutput;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error trying to start process", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            return StreamReader.Null;
        }
    }
}
