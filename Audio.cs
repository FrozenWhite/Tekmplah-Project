using System.Reflection;
using System.Runtime.InteropServices;

namespace Teknomli
{
    public class Audio
    {
        private readonly PInvoke _invInfo;
        private readonly MethodInfo? _audio1;
        private PlaySnd? playSnd;
        private delegate void PlaySnd(float frq, int volume, int playtime, int type, ref WAVEHDR hdr);
        public WAVEHDR hdr;
        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public uint dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public uint reserved;
        }

        public Audio(string libPath)
        {
            this._invInfo = new PInvoke()
            {
                ProcName = "Play",
                EntryPoint = "Play",
                ModuleFile = libPath,
                ReturnType = typeof(void),
                ParameterTypes = new[] { typeof(float), typeof(int), typeof(int), typeof(int), typeof(WAVEHDR).MakeByRefType() },
                CallingConvention = CallingConvention.StdCall,
                CharSet = CharSet.Unicode
            };
            this._audio1 = RunDll.CreateMethodInfo(this._invInfo);
            if (this._audio1 != null)
            {
                this.playSnd = (PlaySnd)this._audio1.CreateDelegate(typeof(PlaySnd));
            }
        }

        public async void Play(float frq, int volume, int playtime, int type)
        {
            if (this._audio1 == null || this.playSnd == null)
            {
                return;
            }
            await Task.Run(() => this.playSnd(frq, volume, playtime, type, ref this.hdr));
        }
    }
}
