using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Vision.Motion;
using System.Runtime.InteropServices;
using AForge.Imaging;

namespace WebcamMotionDetector
{
    public partial class Form1 : Form
    {
        bool isStarted = false;
        TwoFramesDifferenceDetector diffDetector;

        public Form1()
        {
            InitializeComponent();
            motionProcessing = new BlobCountingObjectsProcessing()
            {
                HighlightColor = Color.Red, 
                HighlightMotionRegions = true,
                MinObjectsHeight = 50, // Object Height (Pixels)
                MinObjectsWidth = 50 // Object Width (Pixels)
            };
            
            diffDetector = new TwoFramesDifferenceDetector()
            {
                SuppressNoise = true, // Applies 3x3 erosion to noisy pixels
                DifferenceThreshold = 15 // Pixel Difference that alarms motion
            };

            motionDetector = new MotionDetector(diffDetector,motionProcessing);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (isStarted)
            {
                await(Task.Run(() => videoCaptureDevice.SignalToStop()));
                videoCaptureDevice.WaitForStop();
                isStarted = false;
                pictureBox1.Visible = false;
                button1.Text = "Start";
                //if (IsConnectedToInternet() && checkBox1.Checked)
                //{
                //    UseWaitCursor = true;
                //    BlobStorageHelper b = new BlobStorageHelper("<YourSASUri>");
                //    await b.UploadDeleteDirAsync(textBox1.Text);
                //    MessageBox.Show("Files uploaded Succesfully");
                //    UseWaitCursor = false;
                //}
            }
            else
            {
                if (pictureBox1 != null)
                {
                    pictureBox1.Refresh();
                    pictureBox1.Visible = true;
                }
                if(comboBox1.SelectedIndex > -1)
                {
                    videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[comboBox1.SelectedIndex].MonikerString);
                    videoCaptureDevice.NewFrame += SnapshotFrame_EventHandler;
                    videoCaptureDevice.Start();
                    isStarted = true;
                    if (button1 != null)
                    {
                        button1.Text = "Stop";
                    }
                }

            }
        }

        private void SnapshotFrame_EventHandler(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bmp = (Bitmap)eventArgs.Frame.Clone();
            if (pictureBox1 != null)
            {
                pictureBox1.Image = bmp;
            }
            if (motionDetector.ProcessFrame((Bitmap)eventArgs.Frame.Clone()) > 0.001)
            {
                try
                {
                    if (motionProcessing.ObjectsCount > 0)
                    {
                        UnmanagedImage unmanagedImage = UnmanagedImage.FromManagedImage(bmp);
                        motionProcessing.ProcessFrame(unmanagedImage, diffDetector.MotionFrame);
                        Bitmap bmpProcessed = unmanagedImage.ToManagedImage();
                        unmanagedImage.Dispose();
                        pictureBox1.Image = bmpProcessed;
                    }
                }
                catch
                {
                    pictureBox1.Image = bmp;
                }
            }
            if (textBox1 != null && !string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrWhiteSpace(textBox1.Text) && checkBox1 != null && checkBox1.Checked)
            {
                DirectoryInfo dir = new DirectoryInfo(textBox1.Text);
                if (dir.Exists)
                {
                    try
                    {
                        bmp.Save($"{textBox1.Text}//{DateTime.UtcNow.ToFileTimeUtc()}.bmp");
                    }
                    catch (System.IO.IOException)
                    {
                        MessageBox.Show("An error occurred in saving your screenshot, please check available storage.");
                    }
                }
            }
        }

        private void ToggleShowSaveOptions()
        {
            if (checkBox1.Checked)
            {
                label3.Enabled = true;
                label3.Visible = true;
                textBox1.Enabled = true;
                textBox1.Visible = true;
            }
            else
            {
                label3.Enabled = false;
                label3.Visible = false;
                textBox1.Enabled = false;
                textBox1.Visible = false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(!isStarted)
            {
                ToggleShowSaveOptions();
            }
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        //Creating a function that uses the API function...  
        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }
    }
}
