using System;

namespace Teknomli
{
    /// <summary>
    /// a16 CPU命令
    /// </summary>
    internal static class instruction
    {
        //入力
        public const UInt32 LOD = 0x1;
        public const UInt32 LAD = 0x2;
        public const UInt32 DEL = 0x3;

        //出力

        ///演算
        public const UInt32 ADD = 0x10;
        public const UInt32 SUB = 0x11;
        public const UInt32 AND = 0x14;
        public const UInt32 OR = 0x15;

        //制御

    }
}
