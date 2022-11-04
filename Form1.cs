using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ClickRecorder.Form1;

namespace ClickRecorder
{
    public partial class Form1 : Form
    {
        private static bool WorksInMenus = false;

        private static Random rand = new Random();

        private static IntPtr mc = IntPtr.Zero; //minecraft window

        private static int currentClick = 0; //click that will be played by the clicker

        private static ClickInfo loadedClicks = new ClickInfo() { Seconds = 0 }; //these are the clicks selected by the "load clicks" button

        private static string clicks = ""; //string holds the delay between clicks. if the time from the last click was 50ms and the
                                           //mouse was held down for 10ms, and that was repeated twice, it would look like 10-50,10-50,10-50
                                           //last value is total time the recording took in seconds. Example: 4-20,3-30,0.9342

        private const int clickLimit = 100; //how many clicks will get recorded before recording is over

        private static Stopwatch clickDelay = new Stopwatch(); //stopwatch gets delay between clicks

        private static Stopwatch recordingTime = new Stopwatch(); //stopwatch gets time that the recording lasted

        private static string directory = @"C:\Users\chris\Downloads"; //AppDomain.CurrentDomain.BaseDirectory;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int VirtualKeyPressed);

        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();
        public Form1()
        {
            InitializeComponent();
            timer2.Start();
        }

        private void button1_MouseDown(object sender, EventArgs e)
        {
            if (button1.Text.ToString() == "Click here to start recording")
            {
                clicks = "";
                button1.Text = "Recording...";
            }
            else
            {
                clicks += $"-{clickDelay.ElapsedMilliseconds},"; //adds time between this mousedown and the mouseup
                if (clicks.Split(',').Length - 1 == clickLimit)
                {
                    ClickLimitReached();
                }
                else
                {
                    clickDelay.Restart();
                }
                clickDelay.Restart();
            }
        }

        private void button1_MouseUp(object sender, EventArgs e)
        {
            if (clickDelay.IsRunning)
            {
                clicks += $"{clickDelay.ElapsedMilliseconds}"; //adds time until next mousedown
            }
            else
            {
                recordingTime.Restart();
                clickDelay.Restart();
            }
        }

        private void ClickLimitReached()
        {
            button1.Enabled = false;
            button1.Text = "Click here to start recording";

            clickDelay.Stop();

            clicks += recordingTime.ElapsedMilliseconds;
            recordingTime.Stop();

            int outliersRemoved = RemoveOutliers(ref clicks);

            int fileNum = int.Parse(File.ReadAllText(directory + @"\Clicks\info.drill")) + 1;
            File.WriteAllText(directory + @"\Clicks\info.drill", fileNum.ToString());

            File.Create(directory + $@"\Clicks\clicks{fileNum}.drill").Close();
            File.WriteAllText(directory + $@"\Clicks\clicks{fileNum}.drill", clicks);

            //new Message("Remove outliers?", "If so, select jitter or butterfly", new string[3] {"Jitter", "Butterfly", "OK"}).ShowDialog();
            new Message($@"saved to {directory}\Clicks\clicks{fileNum}.drill", $"{outliersRemoved} outliers removed.").ShowDialog(); //show dialog

            button1.Enabled = true;
        }

        private int RemoveOutliers(ref string data)
        {
            int outliersRemoved = 0;

            string[] splitData = data.Split(',');
            string[] splitClicks = splitData.Take(splitData.Length - 1).ToArray();

            int recordingTime = int.Parse(splitData[splitData.Length - 1]);

            for (int i = 0; i < splitClicks.Length - 1; i++)
            {
                string[] clickDelays = splitClicks[i].Split('-');
                if (int.Parse(clickDelays[0]) > 170 || int.Parse(clickDelays[0]) == 0
                    || int.Parse(clickDelays[1]) > 100 || int.Parse(clickDelays[1]) == 0)
                {
                    splitClicks = splitClicks.Where((source, index) => index != i).ToArray();
                    recordingTime -= int.Parse(clickDelays[0]) + int.Parse(clickDelays[1]);
                    i--;
                    outliersRemoved++;
                }
            }
            clicks = string.Join(",", splitClicks);
            clicks += "," + recordingTime;
            return outliersRemoved;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Enable")
            {
                currentClick = 0;

                OpenFileDialog o = new OpenFileDialog();
                o.Filter = "drill files (*.drill)|*.drill";
                o.InitialDirectory = directory + @"\Clicks";

                if (o.ShowDialog() == DialogResult.OK)
                {
                    LoadClicks(o.FileName);
                }
                else
                {
                    new Message("There was an issue", "Try again").ShowDialog();
                }
            }
            else
            {
                new Message("Error:", "Please disable the clicker before loading clicks").ShowDialog();
            }
        }

        private void LoadClicks(string filePath)
        {
            ClickInfo clickInfo = GetClickInfo(File.ReadAllText(filePath));

            if (clickInfo.AverageCPS > 20)
            {
                new Message("Warning:", "CPS is higher than recommended.").ShowDialog();
            }
            loadedClicks = clickInfo;
            label1.Text = $"Average CPS: {clickInfo.AverageCPS}";
            label2.Text = $"Total Clicks: {clickInfo.NumOfClicks}";
        }

        private ClickInfo GetClickInfo(string data)
        {
            string[] splitData = data.Split(',');
            int recordingTime = int.Parse(splitData[splitData.Length - 1]);
            string[] clicks = new string[splitData.Length - 1];
            Array.Copy(splitData, 1, clicks, 0, splitData.Length - 1);

            return new ClickInfo()
            {
                Clicks = clicks,
                NumOfClicks = clicks.Length,
                Seconds = recordingTime,
                AverageCPS = (float)(clicks.Length / TimeSpan.FromMilliseconds(recordingTime).TotalSeconds)
            };
        }

        internal struct ClickInfo
        {
            public string[] Clicks;
            public int NumOfClicks;
            public float Seconds;
            public float AverageCPS;
        }

        private void button3_Click(object sender = null, EventArgs e = null)
        {
            if (button3.Text == "Enable")
            {
                if (loadedClicks.Seconds != 0)
                {
                    button3.Text = "Disable";
                    timer1.Start();
                }
                else
                {
                    new Message("No clicks loaded", "Record and load some clicks to enable clicker").ShowDialog();
                }
            }
            else
            {
                button3.Text = "Enable";
                timer1.Stop();
            }
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            Cursorinfo ci = new Cursorinfo()
            {
                CbSize = Marshal.SizeOf(typeof(Cursorinfo))
            };
            GetCursorInfo(ref ci);

            if (GetAsyncKeyState(0x01) != 0
                && mc == GetForegroundWindow()
                && (ci.HCursor.ToInt32() > 100000 || WorksInMenus))
            {
                timer1.Interval = int.Parse(loadedClicks.Clicks[currentClick].Split('-')[1]);

                PostMessage(mc, 0x201, 0, 0);
                await Task.Delay(int.Parse(loadedClicks.Clicks[currentClick].Split('-')[0]));
                PostMessage(mc, 0x202, 0, 0);

                currentClick = rand.Next(0, loadedClicks.NumOfClicks - 1);
            }
        }

        private async void timer2_Tick(object sender, EventArgs e)
        {
            mc = await FindMc();
            if (mc == IntPtr.Zero && button3.Text == "Disable")
            {
                button3_Click();
                new Message("Minecraft not found", "Is it open?").ShowDialog();
            }
        }

        public static async Task<IntPtr> FindMc()
        {
            IntPtr mcWindow = IntPtr.Zero;
            await Task.Run(async () =>
            {
                Process[] processes = Process.GetProcessesByName("javaw");
                foreach (Process process in processes)
                {
                    mcWindow = FindWindow(null, process.MainWindowTitle);
                }
            });
            return mcWindow;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            WorksInMenus = !WorksInMenus;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public static POINT Null = new POINT { x = 0, y = 0 };
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Cursorinfo
        {
            public int CbSize;
            public readonly int Flags;
            public IntPtr HCursor;
            private readonly POINT PtScreenPos;
        }
        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(ref Cursorinfo pci);
    }
}
