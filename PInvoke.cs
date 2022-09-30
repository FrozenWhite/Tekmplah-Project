using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Teknomli
{
    /// <summary>
    /// PInvoke関数情報
    /// </summary>
    public class PInvoke
    {
        /// <summary>
        /// 関数名
        /// </summary>
        public string ProcName { get; set; }
        /// <summary>
        /// DLLファイル
        /// </summary>
        public string ModuleFile { get; set; }
        /// <summary>
        /// エントリポイント
        /// </summary>
        public string EntryPoint { get; set; }
        /// <summary>
        /// 戻り値の型（戻り値無しはSystem.Void）
        /// </summary>
        public Type ReturnType { get; set; } = typeof(void);
        /// <summary>
        /// 関数のパラメータの型
        /// </summary>
        public Type[] ParameterTypes { get; set; } = { };
        /// <summary>
        /// 呼び出し規約
        /// </summary>
        public CallingConvention CallingConvention { get; set; } = CallingConvention.StdCall;
        /// <summary>
        /// メソッドのキャラクターセット
        /// </summary>
        public CharSet CharSet { get; set; } = CharSet.Auto;
    }
}
