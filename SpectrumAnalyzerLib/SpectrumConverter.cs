using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio;
using NAudio.Wave;
using ILNumerics;

namespace SpectrumAnalyzerLib
{
    public static class SpectrumConverter
    {
        /// <summary>
        /// センサーの波形データをFFTしてスペクトラムを得る
        /// </summary>
        /// <param name="buffer">波形データ</param>
        /// <param name="offset">FFTしたい波形領域の開始位置</param>
        /// <param name="count">FFTしたい波形領域の長さ</param>
        /// <param name="outSpectrums">出力：振幅スペクトラム（チャンネルごと）。チャンネルごとにcount/2個の要素を持つ</param>
        /// <returns></returns>
        public static bool GetSpectrum(List<List<short>> buffer, int offset, int count, List<List<double>> outSpectrums)
        {
            if (buffer == null || outSpectrums == null)
                return false;

            if (buffer.Count <= 0 || outSpectrums.Count < buffer.Count)
                return false;

            int from = offset;
            int to = offset + count;

            if (to <= 0 || from < 0)
                return false;

            double[] _fftIn = new double[count];

            float ratio = 1f / short.MaxValue;

            for (int channel = 0; channel < buffer.Count; channel++)
            {
                if (buffer[channel].Count <= to)
                    return false;

                outSpectrums[channel].Clear();

                // ハミング関数をかける
                double cangle = 2 * Math.PI / (to - from - 1);
                var buf = buffer[channel];
                for (int i = from; i < to; i++)
                {
                    double hamming = 0.54 - 0.46 * Math.Cos(cangle * (i - from));
                    double val = buf[i] * ratio * hamming;
                    _fftIn[i - from] = val;
                }

                // FFT
                ILArray<double> fftIn = _fftIn;
                ILArray<complex> fft = ILMath.fft(fftIn);

                // スペクトラム
                foreach (var z in fft)
                {
                    outSpectrums[channel].Add(z.Abs());
                    if (outSpectrums[channel].Count >= count / 2)
                        break;
                }
            }

            return true;
        }

    }
}
