using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio;
using NAudio.Wave;
using ILNumerics;

namespace SpectrumAnalyzerLib
{
    public enum AudioType
    {
        Monaural,
        Stereo,
    }

    public class AudioSensor : IDisposable
    {
        const int maxBufferSize = 8000 * 2 * 600;

        public AudioSensorData Data { get { return data; } }

        AudioSensorData data;
        int rate, bits;
        WaveIn waveIn = null;
        int curChannel = 0;
        bool lowBit = true;
        Action<byte[], int> action;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="rate">サンプリングレート(Hz)</param>
        /// <param name="bits">ビットレート(bit)</param>
        /// <param name="channels">チャンネル数（モノラルなら1, ステレオなら2）</param>
        /// <param name="onUpdate">データが更新されたときの処理</param>
        public AudioSensor(int rate = 8000, int bits = 16, AudioType audioType = AudioType.Monaural, Action<byte[], int> onUpdate = null)
        {
            this.rate = rate;
            this.bits = bits;
            this.data = new AudioSensorData(audioType == AudioType.Monaural ? 1 : 2);
            this.action = onUpdate;
        }

        /// <summary>
        /// 録音を停止してインスタンスを破棄
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// 録音を開始
        /// </summary>
        public void Start()
        {
            if (waveIn != null)
                waveIn.StopRecording();
            Cleanup();

            waveIn = new WaveIn();
            waveIn.WaveFormat = new WaveFormat(rate, bits, data.Channels);
            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;
            waveIn.StartRecording();
        }

        /// <summary>
        /// 録音を停止
        /// </summary>
        public void Stop()
        {
            if (waveIn != null)
                waveIn.StopRecording();
            Cleanup();
        }

        void Cleanup()
        {
            if (waveIn != null)
            {
                waveIn.Dispose();
                waveIn = null;
            }
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            AddBytes(e.Buffer, e.BytesRecorded);
            if (action != null)
                action(e.Buffer, e.BytesRecorded);
        }

        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                throw e.Exception;
        }

        void AddBytes(byte[] bytes, int count)
        {
            if (bytes == null || bytes.Length < count)
                return;

            if (data.Buffer.Count <= curChannel)
                return;

            for (int i = 0; i < count; i++)
            {
                byte n = bytes[i];
                if (lowBit)
                {
                    data.Buffer[curChannel].Add(n);
                    lowBit = false;
                }
                else
                {
                    short highBits = (short)(n << 8);
                    if (data.Buffer[curChannel].Count < 1)
                        continue;
                    short lowBits = (short)data.Buffer[curChannel][data.Buffer[curChannel].Count - 1];
                    data.Buffer[curChannel].Add((short)(highBits | lowBits));
                    curChannel = (curChannel + 1) % data.Buffer.Count;
                    lowBit = true;
                }
            }

            int maxSize = maxBufferSize / 2 * 2;
            for (int i = 0; i < data.Channels; i++)
            {
                if (data.Buffer[i].Count > maxSize)
                    data.Buffer[i].RemoveRange(0, data.Buffer[i].Count - maxSize);
            }
        }
    }
}