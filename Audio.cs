using System.Reflection;
using System.Runtime.InteropServices;

namespace Teknomli
{
    public class Audio
    {
        private readonly PInvoke _invInfo;
        private readonly MethodInfo? _audio1;
        public Audio(string libPath)
        {
            this._invInfo = new PInvoke()
            {
                ProcName = "Play",
                EntryPoint = "Play",
                ModuleFile = libPath,
                ReturnType = typeof(void),
                ParameterTypes = new[] { typeof(float), typeof(int), typeof(int), typeof(int) },
                CallingConvention = CallingConvention.StdCall,
                CharSet = CharSet.Unicode
            };
            this._audio1 = RunDll.CreateMethodInfo(this._invInfo);
        }

        public async void Play(float frq, int volume, int playtime, int type)
        {
            this._audio1?.Invoke(null, new object[] { frq, volume, playtime, type });
        }
    }
}
