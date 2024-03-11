using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private PictureBox pictureBoxOriginal;
        private TextBox txtDownscalingFactor;
        private PictureBox pictureBoxDownscaled;

        public Form1()
        {
            InitializeComponent();

            var btnOpenImage = new Button
            {
                Text = "Open Image",
                Location = new Point(10, 10),
                Size = new Size(100, 30)
            };
            btnOpenImage.Click += btnOpenImage_Click;
            Controls.Add(btnOpenImage);

            var lblDownscaleFactor = new Label
            {
                Text = "Downscaling Factor (%):",
                Location = new Point(10, 50),
                Size = new Size(180, 20)
            };
            Controls.Add(lblDownscaleFactor);

            txtDownscalingFactor = new TextBox
            {
                Location = new Point(200, 50),
                Size = new Size(100, 20)
            };
            Controls.Add(txtDownscalingFactor);

            var btnDownscale = new Button
            {
                Text = "Downscale Image",
                Location = new Point(10, 80),
                Size = new Size(100, 30)
            };
            btnDownscale.Click += btnDownscale_Click;
            Controls.Add(btnDownscale);

            pictureBoxOriginal = new PictureBox
            {
                Location = new Point(10, 120),
                Size = new Size(200, 200),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            Controls.Add(pictureBoxOriginal);

            pictureBoxDownscaled = new PictureBox
            {
                Location = new Point(220, 120),
                Size = new Size(200, 200),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            Controls.Add(pictureBoxDownscaled);
        }

        private void btnOpenImage_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBoxOriginal.Image = Image.FromFile(openFileDialog.FileName);
                }
            }
        }

        private void btnDownscale_Click(object sender, EventArgs e)
        {
            if (pictureBoxOriginal.Image is Bitmap originalBitmap)
            {
                if (double.TryParse(txtDownscalingFactor.Text, out double downscaleFactor) && downscaleFactor > 0 && downscaleFactor <= 100)
                {
                    downscaleFactor /= 100; // Convert percentage to decimal
                    Stopwatch sw = Stopwatch.StartNew();

                    // Choose sequential or parallel method based on your needs
                    Bitmap downscaledImage = DownscaleImageSequentially(originalBitmap, downscaleFactor);
                    // Bitmap downscaledImage = DownscaleImageParallelly(originalBitmap, downscaleFactor);

                    sw.Stop();
                    MessageBox.Show($"Downscaling took {sw.ElapsedMilliseconds} ms", "Performance Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    pictureBoxDownscaled.Image = downscaledImage;
                }
                else
                {
                    MessageBox.Show("Please enter a valid downscaling factor (a number greater than 0 and up to 100).");
                }
            }
            else
            {
                MessageBox.Show("Please select an image first.");
            }
        }

        private Bitmap DownscaleImageSequentially(Bitmap originalImage, double downscaleFactor)
        {
            int newWidth = (int)(originalImage.Width * downscaleFactor);
            int newHeight = (int)(originalImage.Height * downscaleFactor);

            Bitmap downscaledImage = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            Rectangle originalRect = new Rectangle(0, 0, originalImage.Width, originalImage.Height);
            Rectangle downscaledRect = new Rectangle(0, 0, newWidth, newHeight);

            BitmapData originalData = originalImage.LockBits(originalRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData downscaledData = downscaledImage.LockBits(downscaledRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* origBytes = (byte*)originalData.Scan0;
                byte* downBytes = (byte*)downscaledData.Scan0;

                int origStride = originalData.Stride;
                int downStride = downscaledData.Stride;

                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        int origX = (int)(x / downscaleFactor);
                        int origY = (int)(y / downscaleFactor);

                        byte* origPixel = origBytes + (origY * origStride) + (origX * 3);
                        byte* downPixel = downBytes + (y * downStride) + (x * 3);

                        downPixel[0] = origPixel[0]; // B
                        downPixel[1] = origPixel[1]; // G
                        downPixel[2] = origPixel[2]; // R
                    }
                }
            }

            originalImage.UnlockBits(originalData);
            downscaledImage.UnlockBits(downscaledData);

            return downscaledImage;
        }


        private Bitmap DownscaleImageParallelly(Bitmap originalImage, double downscaleFactor)
        {
            int newWidth = (int)(originalImage.Width * downscaleFactor);
            int newHeight = (int)(originalImage.Height * downscaleFactor);

            Bitmap downscaledImage = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            Rectangle originalRect = new Rectangle(0, 0, originalImage.Width, originalImage.Height);
            Rectangle downscaledRect = new Rectangle(0, 0, newWidth, newHeight);

            BitmapData originalData = originalImage.LockBits(originalRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData downscaledData = downscaledImage.LockBits(downscaledRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* origBytes = (byte*)originalData.Scan0;
                byte* downBytes = (byte*)downscaledData.Scan0;

                int origStride = originalData.Stride;
                int downStride = downscaledData.Stride;

                Parallel.For(0, newHeight, y =>
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        int origX = (int)(x / downscaleFactor);
                        int origY = (int)(y / downscaleFactor);

                        byte* origPixel = origBytes + (origY * origStride) + (origX * 3);
                        byte* downPixel = downBytes + (y * downStride) + (x * 3);

                        downPixel[0] = origPixel[0]; // B
                        downPixel[1] = origPixel[1]; // G
                        downPixel[2] = origPixel[2]; // R
                    }
                });
            }

            originalImage.UnlockBits(originalData);
            downscaledImage.UnlockBits(downscaledData);

            return downscaledImage;
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
    }
}
