using System;
using System.Runtime.InteropServices;

namespace Vosk
{
    public class Model : IDisposable
    {
        internal IntPtr handle;

        [DllImport("vosk")]
        private static extern IntPtr vosk_model_new(string path);

        [DllImport("vosk")]
        private static extern void vosk_model_free(IntPtr model);

        public Model(string path)
        {
            handle = vosk_model_new(path);
            if (handle == IntPtr.Zero)
            {
                throw new Exception("Failed to create Vosk model");
            }
        }

        public void Dispose()
        {
            vosk_model_free(handle);
        }
    }

    public class VoskRecognizer : IDisposable
    {
        private IntPtr handle;

        [DllImport("vosk")]
        private static extern IntPtr vosk_recognizer_new(IntPtr model, float sample_rate);

        [DllImport("vosk")]
        private static extern void vosk_recognizer_free(IntPtr recognizer);

        [DllImport("vosk")]
        private static extern int vosk_recognizer_accept_waveform(IntPtr recognizer, byte[] data, int len);

        [DllImport("vosk")]
        private static extern IntPtr vosk_recognizer_result(IntPtr recognizer);

        [DllImport("vosk")]
        private static extern IntPtr vosk_recognizer_partial_result(IntPtr recognizer);

        public VoskRecognizer(Model model, float sampleRate)
        {
            handle = vosk_recognizer_new(model.handle, sampleRate);
            if (handle == IntPtr.Zero)
            {
                throw new Exception("Failed to create Vosk recognizer");
            }
        }

        public bool AcceptWaveform(byte[] data, int length)
        {
            return vosk_recognizer_accept_waveform(handle, data, length) != 0;
        }

        public string Result()
        {
            IntPtr ptr = vosk_recognizer_result(handle);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public string PartialResult()
        {
            IntPtr ptr = vosk_recognizer_partial_result(handle);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public void Dispose()
        {
            vosk_recognizer_free(handle);
        }
    }
}
