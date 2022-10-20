using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;

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
        public string? ProcName { get; set; }
        /// <summary>
        /// DLLファイル
        /// </summary>
        public string? ModuleFile { get; set; }
        /// <summary>
        /// エントリポイント
        /// </summary>
        public string? EntryPoint { get; set; }
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

    public static class RunDll
    {
        /// <summary>
        /// PInvoke関数情報から、メソッドのメタデータを作成する。
        /// </summary>
        /// <param name="invInfo">PInvoke関数情報</param>
        /// <returns>PInvoke関数メタデータ</returns>
        public static MethodInfo? CreateMethodInfo(PInvoke invInfo)
        {
            string moduleName = Path.GetFileNameWithoutExtension(invInfo.ModuleFile)!.ToUpper();
            AssemblyBuilder asmBld = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("Asm" + moduleName), AssemblyBuilderAccess.Run);

            ModuleBuilder modBld = asmBld.DefineDynamicModule(
                "Mod" + moduleName);

            TypeBuilder typBld = modBld.DefineType(
                "Class" + moduleName,
                TypeAttributes.Public | TypeAttributes.Class);

            if (invInfo.ProcName != null &&
                invInfo.ModuleFile != null &&
                invInfo.EntryPoint != null)
            {
                MethodBuilder methodBuilder = typBld.DefinePInvokeMethod(
                    name: invInfo.ProcName,
                    dllName: invInfo.ModuleFile,
                    entryName: invInfo.EntryPoint,
                    attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl | MethodAttributes.HideBySig,
                    callingConvention: CallingConventions.Standard,
                    returnType: invInfo.ReturnType,
                    invInfo.ParameterTypes.ToArray(),
                    invInfo.CallingConvention,
                    invInfo.CharSet);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig);
            }

            return typBld.CreateType()?.GetMethod(invInfo.ProcName ?? "");
        }
    }
}
