using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using SpectrumAnalyzerLib;

namespace SpectrumAnalyzer
{
    public partial class Form1 : Form
    {
        enum RenderType
        {
            None,
            Wave,
            Spectrum,
        }

        const int readCount = 8000 * 50 / 1000; // 50 ms
        const int shift = 8000 * 40 / 1000; // 40 ms

        int readIdx = 0;

        bool recording = false;

        AudioSensor sensor;
        List<List<double>> spectrums = new List<List<double>>();

        Pen pen = new Pen(Brushes.Black);
        PointF[] pts = new PointF[8000];

        Bitmap spectrumBmp = new Bitmap(800, 60, PixelFormat.Format24bppRgb);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            sensor = new AudioSensor(8000, 16, AudioType.Monaural, OnUpdate);
            for (int i = 0; i < sensor.Data.Channels; i++)
                spectrums.Add(new List<double>());
            UpdateGUI();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            recording = true;
            if (sensor != null)
                sensor.Start();
            UpdateGUI();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            recording = false;
            if (sensor != null)
                sensor.Stop();
            UpdateGUI();
        }

        void canvas_Paint(object sender, PaintEventArgs e)
        {
            RenderType renderType = GetRenderType();
            if (renderType == RenderType.Wave)
            {
                if (sensor != null)
                    DrawWaves(e.Graphics, sensor.Data);
            }
            if (renderType == RenderType.Spectrum)
            {
                if (spectrums != null)
                    DrawSpectrums(e.Graphics, spectrumBmp);
            }
        }

        private void waveRButton_CheckedChanged(object sender, EventArgs e)
        {
            canvas.Invalidate();
        }

        private void spectrumRButton_CheckedChanged(object sender, EventArgs e)
        {
            canvas.Invalidate();
        }

        // データが更新された
        void OnUpdate(byte[] bytes, int count)
        {
            if (spectrumRButton.Checked)
            {
                // スペクトラムを求めてビットマップに描画
                while (true)
                {
                    bool succeed = SpectrumConverter.GetSpectrum(sensor.Data.Buffer, readIdx, readCount, spectrums);
                    if (!succeed)
                        break;
                    readIdx += shift;
                    PredrawSpectrums(spectrums, spectrumBmp);
                }
            }
            canvas.Invalidate();
        }


        RenderType GetRenderType()
        {
            if (waveRButton.Checked)
                return RenderType.Wave;
            if (spectrumRButton.Checked)
                return RenderType.Spectrum;
            return RenderType.None;
        }

        //------------------------------------------------------------
        // View
        //------------------------------------------------------------

        void UpdateGUI()
        {
            if (recording)
            {
                startButton.Enabled = false;
                stopButton.Enabled = true;
            }
            else
            {
                startButton.Enabled = true;
                stopButton.Enabled = false;
            }
        }

        void DrawWaves(Graphics g, AudioSensorData data)
        {
            g.Clear(Color.White);

            if (pts == null || pts.Length <= 1)
                return;

            if (data == null || data.Buffer == null)
                return;

            if (data.Buffer.Count <= 0)
                return;

            float ratioy = -g.ClipBounds.Height / (2f * data.Channels * short.MaxValue);

            float dx = (float)g.ClipBounds.Width / pts.Length;
            float h = g.ClipBounds.Height / data.Channels;

            int i0 = Math.Max(0, data.Buffer[0].Count - pts.Length);

            for (int channel = 0; channel < data.Channels; channel++)
            {
                float oy = h * channel + 0.5f * h;
                for (int i = i0; i < data.Buffer[channel].Count; i++)
                {
                    pts[i - i0].X = dx * (i - i0);
                    pts[i - i0].Y = data.Buffer[channel][i] * ratioy + oy;
                }

                g.DrawLines(pen, pts);
            }
        }

        unsafe void PredrawSpectrums(List<List<double>> spectrums, Bitmap spectrumBmp)
        {
            const int dx = 10;

            if (spectrums == null || spectrums.Count <= 0)
                return;

            if (spectrumBmp == null || spectrumBmp.Width <= 0 || spectrumBmp.Height <= 0)
                return;

            using (Graphics g = Graphics.FromImage(spectrumBmp))
                g.DrawImage(spectrumBmp, -dx, 0);

            BitmapData lck = spectrumBmp.LockBits(new Rectangle(Point.Empty, spectrumBmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            byte* data = (byte*)lck.Scan0;
            int stride = lck.Width * 3;
            stride = stride % 4 == 0 ? stride : (stride / 4 + 1) * 4;

            float minIdx = 0;
            float maxIdx = spectrums[0].Count - 1;

            float yto01 = 1f / spectrumBmp.Height * spectrums.Count;
            float invdy = spectrums[0].Count * spectrums.Count / spectrumBmp.Height;

            for (int y = 0; y < spectrumBmp.Height; y++)
            {
                int channel = (int)(y * yto01);

                if (spectrums.Count <= channel)
                    continue;

                float oy = channel * spectrumBmp.Height / spectrums.Count;
                float t = Math.Max(0, Math.Min(1, (y - oy) * yto01));
                int i = (int)(t * maxIdx + (1 - t) * minIdx);
                double val = spectrums[channel][i];
                byte c = (byte)Math.Max(0, Math.Min(255, val * 255));

                for (int x = spectrumBmp.Width - dx; x < spectrumBmp.Width; x++)
                {
                    int offset = stride * y + 3 * x;
                    data[offset + 0] = 0;
                    data[offset + 1] = c;
                    data[offset + 2] = 0;
                }
            }

            spectrumBmp.UnlockBits(lck);
        }

        void DrawSpectrums(Graphics g, Bitmap bmp)
        {
            g.DrawImage(bmp, g.ClipBounds);
        }
    }
}