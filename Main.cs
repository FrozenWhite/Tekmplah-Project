// Created by FrozenWhite
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.Diagnostics;
using System.Text;
using System.Drawing.Imaging;
using Teknomli.Properties;
using Timer = System.Timers.Timer;

namespace Teknomli
{
    /// <summary>
    /// GUIのメインと裏の描画をしているやつ
    /// </summary>
    public partial class Main : Form
    {
        #region 仕様
        /*
        元になった機種は(設定上)Teknomli x4 Basyk
        一応FrozenWhiteが開発
        色々コメントは書いてあるからそれを読んで理解してね
        えっ、Apolen国向けなのになんでコメントは日本語かって？
        単純に、実装する量が多すぎてApolen語だと爆発するから(((

        レンダリングの仕組みとか

        Render1 (一番上の層) ============ <- 基本文字とかその辺
        Render2   (上の層)   ============ <- なんか色々書けるやつ(ゲームとかならいいじゃない?)
        Render3   (下の層)   ============ <- こんなにいる?ってやつ(実質予備)
        Back       (背景)    ============ <- 背景とか

        音源ボード
        YukiLibっていうライブラリになりました

        というか設定上って言う感じもの多くね？()

        */
        #endregion
        #region 定数
        /// <summary>
        /// フォントの幅
        /// </summary>
        private const int LetterWidth = 8;

        /// <summary>
        /// フォントの高さ
        /// </summary>
        private const int LetterHeight = 14;

        #region カラーパレット
        private readonly List<Color> pallet = new List<Color>();
        #endregion
        #endregion
        #region 変数
        #region エミュレータ上で使用するもの
        /// <summary>
        /// ランダムな値をとるやつ
        /// </summary>
        private Random rand = new Random();

        /// <summary>
        /// メモリ(640KB)
        /// こんなにいるかなぁ()
        /// </summary>
        private IntPtr mem;
        #endregion
        #region 描画する時に使用するもの
        /// <summary>
        /// Back用のBitmap
        /// </summary>
        private Bitmap? _backbmp;

        /// <summary>
        /// Render1用のBitmap
        /// </summary>
        private Bitmap? _render1Bmp;

        /// <summary>
        /// Render2用のBitmap
        /// </summary>
        private Bitmap? _render2Bmp;

        /// <summary>
        /// Render3用のBitmap
        /// </summary>
        private Bitmap? _render3Bmp;

        /// <summary>
        /// 一行にある文字数
        /// </summary>
        private int _lineLetterCount = 0;

        /// <summary>
        /// 出力された行数
        /// </summary>
        private int _outputLineCount = 0;

        /// <summary>
        /// カーソルがある場所
        /// </summary>
        private int _cursorPosition = 0;

        /// <summary>
        /// 入力された文字列
        /// </summary>
        private string _defaultInput = "";

        /// <summary>
        /// 出力された文字列
        /// </summary>
        private string _defaultOutput = "";

        /// <summary>
        /// カーソルが点滅中か
        /// </summary>
        private bool _isFlashed = false;

        /// <summary>
        /// カーソルを点滅させる用
        /// </summary>
        private Timer? _cursorFlash;

        /// <summary>
        /// コンソール画面かどうか
        /// </summary>
        private bool _isConsole = false;

        /// <summary>
        /// フォントセット
        /// </summary>
        private BitmapData[] _fonts;

        private MethodInfo? _audio1;

        private List<string>? _outputedLetters;
        #endregion
        #endregion
        #region C++のインポート
        [DllImport("winmm.dll")]
        public static extern int timeBeginPeriod(int uuPeriod);

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
        #endregion
        #region コンストラクタ
        public Main()
        {
            InitializeComponent();

            this.Text = $@"{ProductName} v{ProductVersion}";

            render1.Parent = render2;
            render2.Parent = render3;
            render3.Parent = back;
            render1.Dock = DockStyle.Fill;
            render2.Dock = DockStyle.Fill;
            render3.Dock = DockStyle.Fill;
            back.Dock = DockStyle.Fill;
        }

        public sealed override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        #endregion
        #region ブートローダー

        /// <summary>
        /// 起動
        /// </summary>
        private async void Start()
        {
            timeBeginPeriod(1);
            await Task.Run(() => Thread.Sleep(500));
            LoadDefaultFonts();
            //Bitmapを作成
            _backbmp = new Bitmap(back.Width, back.Height);
            _render1Bmp = new Bitmap(render1.Width, render1.Height);
            _render2Bmp = new Bitmap(render2.Width, render2.Height);
            _render3Bmp = new Bitmap(render3.Width, render3.Height);
            _render1Bmp.MakeTransparent(Color.Black);
            _render2Bmp.MakeTransparent(Color.Black);
            _render3Bmp.MakeTransparent(Color.Black);
            //セット
            back.Image = _backbmp;
            render1.Image = _render1Bmp;
            render2.Image = _render2Bmp;
            render3.Image = _render3Bmp;
            _outputedLetters = new List<string>();
            _outputedLetters.Add("");
            var invInfo = new PInvoke()
            {
                ProcName = "Play",
                EntryPoint = "Play",
                ModuleFile = "YukiLib.dll",
                ReturnType = typeof(void),
                // frq,volume,playtime,type
                ParameterTypes = new[] { typeof(float), typeof(int), typeof(int), typeof(int) },
                CallingConvention = CallingConvention.StdCall,
                CharSet = CharSet.Unicode
            };
            //Invokeで実行
            _audio1 = CreateMethodInfo(invInfo);
            _audio1?.Invoke(null, new object[] { 2000, 100, 100, 7 });
            await Task.Run(() => Thread.Sleep(90));
            _audio1?.Invoke(null, new object[] { 1000, 100, 100, 7 });
            await MemoryCheck();
            Echo("memory check OK"); NewLine();
            await CheckHeader();
            Init();
        }

        int memchki = 0;
        private async Task MemoryCheck()
        {
            int allmemcnt = 3200001;
            mem = Marshal.AllocHGlobal(allmemcnt);
            var sw = Stopwatch.StartNew();
            for (memchki = 0; memchki < allmemcnt; memchki++)
            {
                Marshal.WriteByte(mem, memchki, Convert.ToByte(255));
                if (sw.ElapsedMilliseconds >= 10)
                {
                    //Bitmapを作成
                    _backbmp = new Bitmap(back.Width, back.Height);
                    //セット
                    back.Image = _backbmp;
                    Echo($"MEMORY {memchki / 1000}KB OK");
                    templlc = 0;
                    _lineLetterCount = 0;
                    _defaultOutput = "";
                    sw.Restart();
                    await Task.Run(() => Thread.Sleep(1));
                }
            }

            sw.Stop();
            Echo($"MEMORY {memchki / 1000}KB OK");
            NewLine();
        }

        private void UnLoadDefaultFonts()
        {
            fonts._0.UnlockBits(_fonts[0]);
            fonts._1.UnlockBits(_fonts[1]);
            fonts._2.UnlockBits(_fonts[2]);
            fonts._3.UnlockBits(_fonts[3]);
            fonts._4.UnlockBits(_fonts[4]);
            fonts._5.UnlockBits(_fonts[5]);
            fonts._6.UnlockBits(_fonts[6]);
            fonts._7.UnlockBits(_fonts[7]);
            fonts._8.UnlockBits(_fonts[8]);
            fonts._9.UnlockBits(_fonts[9]);
            fonts.A.UnlockBits(_fonts[10]);
            fonts.B.UnlockBits(_fonts[11]);
            fonts.C.UnlockBits(_fonts[12]);
            fonts.D.UnlockBits(_fonts[13]);
            fonts.E.UnlockBits(_fonts[14]);
            fonts.F.UnlockBits(_fonts[15]);
            fonts.G.UnlockBits(_fonts[16]);
            fonts.H.UnlockBits(_fonts[17]);
            fonts.I.UnlockBits(_fonts[18]);
            fonts.J.UnlockBits(_fonts[19]);
            fonts.K.UnlockBits(_fonts[20]);
            fonts.L.UnlockBits(_fonts[21]);
            fonts.M.UnlockBits(_fonts[22]);
            fonts.N.UnlockBits(_fonts[23]);
            fonts.O.UnlockBits(_fonts[24]);
            fonts.P.UnlockBits(_fonts[25]);
            fonts.Q.UnlockBits(_fonts[26]);
            fonts.R.UnlockBits(_fonts[27]);
            fonts.S.UnlockBits(_fonts[28]);
            fonts.T.UnlockBits(_fonts[29]);
            fonts.U.UnlockBits(_fonts[30]);
            fonts.V.UnlockBits(_fonts[31]);
            fonts.W.UnlockBits(_fonts[32]);
            fonts.X.UnlockBits(_fonts[33]);
            fonts.Y.UnlockBits(_fonts[34]);
            fonts.Z.UnlockBits(_fonts[35]);
            fonts.a_s.UnlockBits(_fonts[36]);
            fonts.b_s.UnlockBits(_fonts[37]);
            fonts.c_s.UnlockBits(_fonts[38]);
            fonts.d_s.UnlockBits(_fonts[39]);
            fonts.e_s.UnlockBits(_fonts[40]);
            fonts.f_s.UnlockBits(_fonts[41]);
            fonts.g_s.UnlockBits(_fonts[42]);
            fonts.h_s.UnlockBits(_fonts[43]);
            fonts.i_s.UnlockBits(_fonts[44]);
            fonts.j_s.UnlockBits(_fonts[45]);
            fonts.k_s.UnlockBits(_fonts[46]);
            fonts.l_s.UnlockBits(_fonts[47]);
            fonts.m_s.UnlockBits(_fonts[48]);
            fonts.n_s.UnlockBits(_fonts[49]);
            fonts.o_s.UnlockBits(_fonts[50]);
            fonts.p_s.UnlockBits(_fonts[51]);
            fonts.q_s.UnlockBits(_fonts[52]);
            fonts.r_s.UnlockBits(_fonts[53]);
            fonts.s_s.UnlockBits(_fonts[54]);
            fonts.t_s.UnlockBits(_fonts[55]);
            fonts.u_s.UnlockBits(_fonts[56]);
            fonts.v_s.UnlockBits(_fonts[57]);
            fonts.w_s.UnlockBits(_fonts[58]);
            fonts.x_s.UnlockBits(_fonts[59]);
            fonts.y_s.UnlockBits(_fonts[60]);
            fonts.z_s.UnlockBits(_fonts[61]);
            fonts.que.UnlockBits(_fonts[62]);
            fonts.exc.UnlockBits(_fonts[63]);
            fonts.quo.UnlockBits(_fonts[64]);
            fonts.has.UnlockBits(_fonts[65]);
            fonts.dol.UnlockBits(_fonts[66]);
            fonts.per.UnlockBits(_fonts[67]);
            fonts.and.UnlockBits(_fonts[68]);
            fonts.sin.UnlockBits(_fonts[69]);
            fonts.brs.UnlockBits(_fonts[70]);
            fonts.bre.UnlockBits(_fonts[71]);
            fonts.bkt.UnlockBits(_fonts[72]);
            fonts.bsl.UnlockBits(_fonts[73]);
            fonts.cbs.UnlockBits(_fonts[74]);
            fonts.cbe.UnlockBits(_fonts[75]);
            fonts.sla.UnlockBits(_fonts[76]);
            fonts.plu.UnlockBits(_fonts[77]);
            fonts.min.UnlockBits(_fonts[78]);
            fonts.ast.UnlockBits(_fonts[79]);
            fonts.dot.UnlockBits(_fonts[80]);
            fonts.com.UnlockBits(_fonts[81]);
            fonts.spa.UnlockBits(_fonts[82]);
            fonts.gre.UnlockBits(_fonts[83]);
            fonts.les.UnlockBits(_fonts[84]);
            fonts.equ.UnlockBits(_fonts[85]);
            fonts.sai.UnlockBits(_fonts[86]);
        }

        /// <summary>
        /// システムデフォルトのフォントを読み込む
        /// </summary>
        private void LoadDefaultFonts()
        {
            _fonts = new[]
            {
                #region Number
                fonts._0.LockBits(new Rectangle(0, 0, fonts._0.Width, fonts._0.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._1.LockBits(new Rectangle(0, 0, fonts._1.Width, fonts._1.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._2.LockBits(new Rectangle(0, 0, fonts._2.Width, fonts._2.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._3.LockBits(new Rectangle(0, 0, fonts._3.Width, fonts._3.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._4.LockBits(new Rectangle(0, 0, fonts._4.Width, fonts._4.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._5.LockBits(new Rectangle(0, 0, fonts._5.Width, fonts._5.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._6.LockBits(new Rectangle(0, 0, fonts._6.Width, fonts._6.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._7.LockBits(new Rectangle(0, 0, fonts._7.Width, fonts._7.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._8.LockBits(new Rectangle(0, 0, fonts._8.Width, fonts._8.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts._9.LockBits(new Rectangle(0, 0, fonts._9.Width, fonts._9.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                #endregion
                #region Alphabet
                fonts.A.LockBits(new Rectangle(0, 0, fonts.A.Width, fonts.A.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.B.LockBits(new Rectangle(0, 0, fonts.B.Width, fonts.B.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.C.LockBits(new Rectangle(0, 0, fonts.C.Width, fonts.C.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.D.LockBits(new Rectangle(0, 0, fonts.D.Width, fonts.D.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.E.LockBits(new Rectangle(0, 0, fonts.E.Width, fonts.E.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.F.LockBits(new Rectangle(0, 0, fonts.F.Width, fonts.F.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.G.LockBits(new Rectangle(0, 0, fonts.G.Width, fonts.G.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.H.LockBits(new Rectangle(0, 0, fonts.H.Width, fonts.H.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.I.LockBits(new Rectangle(0, 0, fonts.I.Width, fonts.I.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.J.LockBits(new Rectangle(0, 0, fonts.J.Width, fonts.J.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.K.LockBits(new Rectangle(0, 0, fonts.K.Width, fonts.K.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.L.LockBits(new Rectangle(0, 0, fonts.L.Width, fonts.L.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.M.LockBits(new Rectangle(0, 0, fonts.M.Width, fonts.M.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.N.LockBits(new Rectangle(0, 0, fonts.N.Width, fonts.N.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.O.LockBits(new Rectangle(0, 0, fonts.O.Width, fonts.O.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.P.LockBits(new Rectangle(0, 0, fonts.P.Width, fonts.P.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.Q.LockBits(new Rectangle(0, 0, fonts.Q.Width, fonts.Q.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.R.LockBits(new Rectangle(0, 0, fonts.R.Width, fonts.R.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.S.LockBits(new Rectangle(0, 0, fonts.S.Width, fonts.S.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.T.LockBits(new Rectangle(0, 0, fonts.T.Width, fonts.T.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.U.LockBits(new Rectangle(0, 0, fonts.U.Width, fonts.U.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.V.LockBits(new Rectangle(0, 0, fonts.V.Width, fonts.V.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.W.LockBits(new Rectangle(0, 0, fonts.W.Width, fonts.W.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.X.LockBits(new Rectangle(0, 0, fonts.X.Width, fonts.X.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.Y.LockBits(new Rectangle(0, 0, fonts.Y.Width, fonts.Y.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.Z.LockBits(new Rectangle(0, 0, fonts.Z.Width, fonts.Z.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                #endregion
                #region Small Alphabet
                fonts.a_s.LockBits(new Rectangle(0, 0, fonts.a_s.Width, fonts.a_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.b_s.LockBits(new Rectangle(0, 0, fonts.b_s.Width, fonts.b_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.c_s.LockBits(new Rectangle(0, 0, fonts.c_s.Width, fonts.c_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.d_s.LockBits(new Rectangle(0, 0, fonts.d_s.Width, fonts.d_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.e_s.LockBits(new Rectangle(0, 0, fonts.e_s.Width, fonts.e_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.f_s.LockBits(new Rectangle(0, 0, fonts.f_s.Width, fonts.f_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.g_s.LockBits(new Rectangle(0, 0, fonts.g_s.Width, fonts.g_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.h_s.LockBits(new Rectangle(0, 0, fonts.h_s.Width, fonts.h_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.i_s.LockBits(new Rectangle(0, 0, fonts.i_s.Width, fonts.i_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.j_s.LockBits(new Rectangle(0, 0, fonts.j_s.Width, fonts.j_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.k_s.LockBits(new Rectangle(0, 0, fonts.k_s.Width, fonts.k_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.l_s.LockBits(new Rectangle(0, 0, fonts.l_s.Width, fonts.l_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.m_s.LockBits(new Rectangle(0, 0, fonts.m_s.Width, fonts.m_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.n_s.LockBits(new Rectangle(0, 0, fonts.n_s.Width, fonts.n_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.o_s.LockBits(new Rectangle(0, 0, fonts.o_s.Width, fonts.o_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.p_s.LockBits(new Rectangle(0, 0, fonts.p_s.Width, fonts.p_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.q_s.LockBits(new Rectangle(0, 0, fonts.q_s.Width, fonts.q_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.r_s.LockBits(new Rectangle(0, 0, fonts.r_s.Width, fonts.r_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.s_s.LockBits(new Rectangle(0, 0, fonts.s_s.Width, fonts.s_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.t_s.LockBits(new Rectangle(0, 0, fonts.t_s.Width, fonts.t_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.u_s.LockBits(new Rectangle(0, 0, fonts.u_s.Width, fonts.u_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.v_s.LockBits(new Rectangle(0, 0, fonts.v_s.Width, fonts.v_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.w_s.LockBits(new Rectangle(0, 0, fonts.w_s.Width, fonts.w_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.x_s.LockBits(new Rectangle(0, 0, fonts.x_s.Width, fonts.x_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.y_s.LockBits(new Rectangle(0, 0, fonts.y_s.Width, fonts.y_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                fonts.z_s.LockBits(new Rectangle(0, 0, fonts.z_s.Width, fonts.z_s.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                #endregion
                #region symbol
                // ?
                fonts.que.LockBits(new Rectangle(0, 0, fonts.que.Width, fonts.que.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // !
                fonts.exc.LockBits(new Rectangle(0, 0, fonts.exc.Width, fonts.exc.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // "
                fonts.quo.LockBits(new Rectangle(0, 0, fonts.quo.Width, fonts.quo.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // #
                fonts.has.LockBits(new Rectangle(0, 0, fonts.has.Width, fonts.has.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // $
                fonts.dol.LockBits(new Rectangle(0, 0, fonts.dol.Width, fonts.dol.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // %
                fonts.per.LockBits(new Rectangle(0, 0, fonts.per.Width, fonts.per.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // &
                fonts.and.LockBits(new Rectangle(0, 0, fonts.and.Width, fonts.and.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // '
                fonts.sin.LockBits(new Rectangle(0, 0, fonts.sin.Width, fonts.sin.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // (
                fonts.brs.LockBits(new Rectangle(0, 0, fonts.brs.Width, fonts.brs.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // )
                fonts.bre.LockBits(new Rectangle(0, 0, fonts.bre.Width, fonts.bre.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // `
                fonts.bkt.LockBits(new Rectangle(0, 0, fonts.bkt.Width, fonts.bkt.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // \
                fonts.bsl.LockBits(new Rectangle(0, 0, fonts.bsl.Width, fonts.bsl.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // {
                fonts.cbs.LockBits(new Rectangle(0, 0, fonts.cbs.Width, fonts.cbs.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // }
                fonts.cbe.LockBits(new Rectangle(0, 0, fonts.cbe.Width, fonts.cbe.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // /
                fonts.sla.LockBits(new Rectangle(0, 0, fonts.sla.Width, fonts.sla.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // +
                fonts.plu.LockBits(new Rectangle(0, 0, fonts.plu.Width, fonts.plu.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // -
                fonts.min.LockBits(new Rectangle(0, 0, fonts.min.Width, fonts.min.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // *
                fonts.ast.LockBits(new Rectangle(0, 0, fonts.ast.Width, fonts.ast.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // .
                fonts.dot.LockBits(new Rectangle(0, 0, fonts.dot.Width, fonts.dot.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // ,
                fonts.com.LockBits(new Rectangle(0, 0, fonts.com.Width, fonts.com.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                //  (space)
                fonts.spa.LockBits(new Rectangle(0, 0, fonts.spa.Width, fonts.spa.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // >
                fonts.gre.LockBits(new Rectangle(0, 0, fonts.gre.Width, fonts.gre.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // <
                fonts.les.LockBits(new Rectangle(0, 0, fonts.les.Width, fonts.les.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // =
                fonts.equ.LockBits(new Rectangle(0, 0, fonts.gre.Width, fonts.gre.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // 彩
                fonts.sai.LockBits(new Rectangle(0, 0, fonts.sai.Width, fonts.sai.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
				#endregion
            };
        }

        /// <summary>
        /// OSのディスクファイルを検証
        /// </summary>
        private async Task CheckHeader()
        {
            //起動音
            Echo("Version Teknomli x4 Basic"); NewLine();
            Echo("Copyright 2022 FrozenWhite"); NewLine();
            //ファイルを開く
            Echo("checking 8bytes"); NewLine();
            Echo("Load 1st"); NewLine();
            char[] cs = new char[8];
            using (StreamReader sr = new StreamReader(@".\os.hdf", Encoding.ASCII))
            {
                int n = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (Encoding.ASCII.GetBytes(cs)[i] != Encoding.ASCII.GetBytes("THISISAPPC;")[i]) continue;
                    await Task.Run(() => Thread.Sleep(10));
                    n++;
                    Echo("###");
                }
                NewLine();
                if (n == 8)
                    Echo("Completed check"); NewLine();
                Echo("Checking size"); NewLine();
            }
            Extract();
            await Task.Run(() => Thread.Sleep(1000));
            _outputedLetters = new List<string>();
            _outputedLetters.Add("");
            _outputLineCount = 0;
            _lineLetterCount = 0;
            _cursorPosition = 0;
            _defaultInput = "";
            _defaultOutput = "";
            //Bitmapを作成
            _backbmp = new Bitmap(back.Width, back.Height);
            //セット
            back.Image = _backbmp;
        }

        /// <summary>
        /// 起動
        /// </summary>
        private void Init()
        {
            _outputLineCount = 0;
            _cursorPosition = 0;
            _defaultOutput = ""; 
            Echo("------------------------------------------------------------"); NewLine();
            Echo("Horai OS 彩"); NewLine();
            Echo("Copyright (c) 2022 FrozenWhite"); NewLine();
            Echo("------------------------------------------------------------"); NewLine();
            _isConsole = true;
            //カーソルの点滅
            _cursorFlash = new Timer(500);
            _cursorFlash.Elapsed += CursorFlash_Elapsed!;
            _cursorFlash.Start();
        }

        /// <summary>
        /// HDFファイルの中身を出力する
        /// </summary>
        private void Extract()
        {

        }
        #endregion
        #region コマンド
        /// <summary>
        /// 引数のコマンドを実行
        /// 実行可能なコマンド
        /// echo
        /// run
        /// </summary>
        /// <param name="commands">コマンド(例:echo test)</param>
        private async void RunCommand(string commands)
        {
            var cmd = commands.Split(' ');
            switch (cmd[0].ToLower())
            {
                case "run":
                    RunApplication(cmd[^1]);
                    break;
                case "echo":
                    NewLine();
                    var str = "";
                    for (int i = 1; i < cmd.Length; i++)
                        str += cmd[i] + " ";
                    var rsl = str.Remove(0, str.IndexOf("'", StringComparison.Ordinal) + 1);
                    rsl = rsl.Remove(rsl.IndexOf("'", StringComparison.Ordinal));
                    if (rsl.Length <= str.Length)
                        Echo(rsl);
                    break;
                case "draw":
                    switch (cmd[1].ToLower())
                    {
                        case "line":
                            string value = "(10,10)";
                            string strValue = value.Remove(0, value.IndexOf("(", StringComparison.Ordinal) + 1);
                            strValue = strValue.Remove(strValue.IndexOf(")", StringComparison.Ordinal));
                            Debug.WriteLine(strValue);
                            break;
                        case "circle":
                            BitmapData render1bmpdat = _render1Bmp!.LockBits(new Rectangle(0, 0, _render1Bmp.Width, _render1Bmp.Height),  ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                            int width = 50;
                            int height = 50;
                            for (int x = 0; x < width; x++)
                            {
                                for (int y = 0; y < height; y++)
                                {
                                    if ((x - width / 2) * (x - width / 2) + (y - height / 2) * (y - height / 2) <= (width / 2) * (height / 2))
                                    {
                                        BitmapDataEx.SetPixel(render1bmpdat, x, y, Color.FromArgb(255, 255, 255, 255));
                                    }
                                }
                            }
                            _render1Bmp.UnlockBits(render1bmpdat);
                            _render1Bmp.MakeTransparent(Color.Black);
                            render1.Image = _render1Bmp;
                            break;
                    }
                    break;
                case "clean":
                    CleanScreen();
                    break;
                default:
                    NewLine();
                    Echo("'" + cmd[0] + "' is not recognized as an internal or external command");
                    break;
            }
        }

        private void RunApplication(string name)
        {
            _cursorFlash!.Stop();
        }

        private void reset()
        {

        }

        private void CleanScreen()
        {
            if (_cursorFlash != null)
                _cursorFlash.Stop();
            _isConsole = false;
            _lineLetterCount = 0;
            _outputLineCount = -1;
            _cursorPosition = 0;
            _defaultInput = "";
            _defaultOutput = "";
            //Bitmapを作成
            _backbmp = new Bitmap(back.Width, back.Height);
            _render1Bmp = new Bitmap(render1.Width, render1.Height);
            _render2Bmp = new Bitmap(render2.Width, render2.Height);
            _render3Bmp = new Bitmap(render3.Width, render3.Height);
            //セット
            back.Image = _backbmp;
            render1.Image = _render1Bmp;
            render2.Image = _render2Bmp;
            render3.Image = _render3Bmp;
            _isConsole = true;
            if (_cursorFlash != null)
                _cursorFlash.Start();
        }

        private void SetPixel(Point point, Color color, int layer)
        {
            _cursorFlash!.Stop();
            switch (layer)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pt">左上の基準点</param>
        /// <param name="n">パーティクルの大きさ</param>
        /// <param name="del">消すモードにするかどうか</param>
        /// <param name="minrad">最小のラジアン</param>
        /// <param name="maxrad">最大のラジアン</param>
        /// <param name="isred">赤にするかどうか</param>
        private void drawCircle(Point pt, int n = 5, bool del = false, int minrad = 0, int maxrad = 361, bool isred = false)
        {
            _cursorFlash!.Stop();
            Bitmap cirblue = new Bitmap(@"D:\Downloads\circleblue.bmp");
            Bitmap cirred = new Bitmap(@"D:\Downloads\circlered.bmp");
            BitmapData render1bmpdat = _render1Bmp!.LockBits(new Rectangle(0, 0, _render1Bmp.Width, _render1Bmp.Height),  ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData backbmpdat = _backbmp!.LockBits(new Rectangle(0, 0, _backbmp.Width, _backbmp.Height),  ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            BitmapData bluecircleData = cirblue.LockBits(new Rectangle(0, 0, cirblue.Width, cirblue.Height),  ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData redcircleData = cirred.LockBits(new Rectangle(0, 0, cirred.Width, cirred.Height),  ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            for (float i = 0; i < 361; i++)
            {
                if (i % 10 == 0 && minrad <= i && i <= maxrad)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            Color circleblu = BitmapDataEx.GetPixel(bluecircleData, x, y);
                            Color circlered = BitmapDataEx.GetPixel(redcircleData, x, y);
                            if (circleblu != Color.FromArgb(255, 0, 0, 0))
                            {
                                float xl = (float)(Math.Cos(Math.PI * 2 / 360 * i) * (n) + pt.X + x);
                                float yl = (float)(Math.Sin(Math.PI * 2 / 360 * i) * (n) + pt.Y + y);
                                if (del)
                                {
                                    if (isred)
                                        BitmapDataEx.SetPixel(render1bmpdat, (int)xl, (int)yl, Color.FromArgb(0));
                                    else
                                        BitmapDataEx.SetPixel(backbmpdat, (int)xl, (int)yl, Color.FromArgb(0));
                                }
                                else
                                {
                                    if (circleblu != Color.FromArgb(255, 0, 0, 0))
                                    {
                                        if (isred)
                                            BitmapDataEx.SetPixel(render1bmpdat, (int)xl, (int)yl, circlered);
                                        else
                                            BitmapDataEx.SetPixel(backbmpdat, (int)xl, (int)yl, circleblu);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            cirblue.UnlockBits(bluecircleData);
            _backbmp.UnlockBits(backbmpdat);
            _render1Bmp.UnlockBits(render1bmpdat);
            back.Image = _backbmp;
            _render1Bmp!.MakeTransparent(Color.Black);
            _render2Bmp!.MakeTransparent(Color.Black);
            _render3Bmp!.MakeTransparent(Color.Black);
            render1.Image = _render1Bmp;
            _cursorFlash.Start();
        }

        #endregion
        #region 描画

        private static Bitmap RotateImage(Bitmap bmp, float angle)
        {
            var alpha = angle;
            while (alpha < 0) alpha += 360;

            var gamma = 90;
            var beta = 180 - angle - gamma;

            float c1 = bmp.Height;
            var a1 = (float)(c1 * Math.Sin(alpha * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));
            var b1 = (float)(c1 * Math.Sin(beta * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));

            float c2 = bmp.Width;
            var a2 = (float)(c2 * Math.Sin(alpha * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));
            var b2 = (float)(c2 * Math.Sin(beta * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));

            var width = Convert.ToInt32(b2 + a1);
            var height = Convert.ToInt32(b1 + a2);

            Bitmap rotatedImage = new Bitmap(width, height);
            using Graphics g = Graphics.FromImage(rotatedImage);
            g.TranslateTransform(rotatedImage.Width / 2, rotatedImage.Height / 2);
            g.RotateTransform(angle);
            g.TranslateTransform(-rotatedImage.Width / 2, -rotatedImage.Height / 2);
            g.DrawImage(bmp, new Point((width - bmp.Width) / 2, (height - bmp.Height) / 2));
            return rotatedImage;
        }

        /// <summary>
        /// 指定された文字をコンソールに出力する
        /// </summary>
        /// <param name="str">出力する文字</param>
        private void Echo(string str)
        {
            foreach (var chr in str)
            {
                SetLetter(chr, Color.White, Color.Black);
                _cursorPosition++;
            }
        }

        /// <summary>
        /// 改行
        /// </summary>
        private void NewLine()
        {
            _outputLineCount++;
            _lineLetterCount = 0;
            templlc = 0;
            tempcur = 0;
            _cursorPosition = 0;
            _defaultInput = "";
            _defaultOutput = "";
            _isTempReturn = false;
        }

        private bool _isTempReturn;
        private int templlc;
        private int tempcur;
        /// <summary>
        /// 指定されたcharを出力する
        /// </summary>
        /// <param name="letter">出力するChar</param>
        /// <param name="LetterColor"></param>
        internal void SetLetter(char letter, Color LetterColor, Color backColor,int lx = -1,int ly = -1)
        {
            _cursorFlash?.Stop();
            //charをstringに変換
            var ltr = letter.ToString();
            _outputedLetters!.Add("");
            _outputedLetters[_outputLineCount] = _outputedLetters[_outputLineCount].Insert(0, ltr);
            //Letter.ReplaceLetters(ref ltr);
            if (_defaultInput.Length * LetterWidth > _backbmp!.Width - (LetterWidth / 2) && !_isTempReturn)
            {
                _outputLineCount++;
                templlc = 0;
                tempcur = 0;
                _isTempReturn = true;
            }
            BitmapData backbmpdat = _backbmp!.LockBits(new Rectangle(0, 0, _backbmp.Width, _backbmp.Height),  ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            switch (ltr)
            {
                //空欄だったらそのまま
                case " ":
                    {
                        for (var x = 0; x < LetterWidth; x++)
                        {
                            for (var y = 0; y < LetterHeight; y++)
                            {
                                BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * templlc) + 2, y + (LetterHeight * _outputLineCount), backColor);
                            }
                        }
                        _lineLetterCount++;
                        templlc++;
                        break;
                    }
                //改行は単純に行を増やすだけ
                case "\n":
                    {
                        NewLine();
                        break;
                    }
                //その他の文字は描画
                default:
                    {
                        //文字ファイルを読み込み
                        BitmapData fontdata = _fonts[Letter.ConvertLetterCode(ltr)];
                        //Bitmapを直接操作
                        for (var x = 0; x < fontdata.Width; x++)
                        {
                            for (var y = 0; y < fontdata.Height; y++)
                            {
                                BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * templlc),
                                    y + (LetterHeight * _outputLineCount),
                                    BitmapDataEx.GetPixel(fontdata, x, y) != Color.FromArgb(255, 0, 0, 0)
                                        ? LetterColor
                                        : backColor);
                            }
                        }
                        _lineLetterCount++;
                        templlc++;
                        break;
                    }
            }
            //標準出力に書き込み
            _defaultOutput += letter.ToString();
            //解放
            _backbmp.UnlockBits(backbmpdat);
            //描画
            back.Image = _backbmp;
            _cursorFlash?.Start();
        }
        #endregion
        #region イベント
        #region Main
        private void Main_Load(object sender, EventArgs e)
        {
            Start();
        }

        //終了処理をここに記入
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            //UnLoadDefaultFonts();
            Marshal.FreeHGlobal(mem);
        }
        #endregion

        #region キーボードイベント
        private async void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isConsole)
            {
                //点滅の一時停止
                _cursorFlash!.Stop();
                await Task.Run(() => Thread.Sleep(1));
                BitmapData? backbmpdat = null;
                try
                {
                    backbmpdat = _backbmp!.LockBits(new Rectangle(0, 0, _backbmp.Width, _backbmp.Height),  ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    //カーソルの削除
                    if (_lineLetterCount == tempcur || _defaultInput == "")
                    {
                        for (int x = 0; x < LetterWidth; x++)
                        {
                            for (int y = 0; y < LetterHeight; y++)
                            {
                                BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * tempcur), y + (LetterHeight * _outputLineCount), Color.FromArgb(0));
                            }
                        }
                    }
                    else
                    {
                        var ltr = _defaultInput[_cursorPosition].ToString();
                        //記号とかを置き換え
                        //Letter.ReplaceLetters(ref ltr);
                        BitmapData fontdata = _fonts[Letter.ConvertLetterCode(ltr)];
                        for (int x = 0; x < LetterWidth; x++)
                        {
                            for (int y = 0; y < LetterHeight; y++)
                            {
                                if (BitmapDataEx.GetPixel(fontdata, x, y) == Color.FromArgb(255, 255, 255, 255))
                                {
                                    BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * tempcur), y + (LetterHeight * _outputLineCount), Color.White);
                                }
                                else
                                {
                                    BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * tempcur), y + (LetterHeight * _outputLineCount), Color.Black);
                                }
                            }
                        }
                    }
                    _isFlashed = false;
                }
                finally
                {
                    if (backbmpdat != null)
                    {
                        _backbmp!.UnlockBits(backbmpdat);
                        back.Image = _backbmp;
                    }
                }
                var str = Letter.ConvertKeyCode(e.KeyCode, e.Shift);
                switch (str)
                {
                    case "enter":
                        {
                            if (_defaultInput != "")
                                RunCommand(_defaultInput);
                            NewLine();
                            break;
                        }
                    case "left" when _cursorPosition != 0:
                        _cursorPosition--;
                        tempcur--;
                        break;
                    case "right" when _cursorPosition != _lineLetterCount:
                        _cursorPosition++;
                        tempcur++;
                        break;
                    case "back":
                        _defaultInput = _defaultInput.Remove(_cursorPosition - 1, 1);
                        Debug.WriteLine(this._defaultInput);
                        var oldcurpt = _cursorPosition;
                        _lineLetterCount = 0;
                        templlc = 0;
                        tempcur = 0;
                        _cursorPosition = 0;
                        Echo(new string(' ',this._defaultInput.Length + 1));
                        _lineLetterCount = 0;
                        templlc = 0;
                        tempcur = 0;
                        _cursorPosition = 0;
                        Echo(_defaultInput);
                        _cursorPosition = oldcurpt - 1;
                        tempcur = _cursorPosition;
                        break;
                    default:
                        {
                            if (str.Length == 1)
                            {
                                char ltr = str[0];
                                _defaultInput = _defaultInput.Insert(_cursorPosition,ltr.ToString());
                                var oldCurP = _cursorPosition; 
                                _lineLetterCount = 0;
                                templlc = 0;
                                tempcur = 0;
                                _cursorPosition = 0;
                                Echo(_defaultInput);
                                _cursorPosition = oldCurP + 1;
                                tempcur = _cursorPosition;
                            }
                            break;
                        }
                }
                _cursorFlash.Start();
            }

            await Task.Run(() => Thread.Sleep(1));
        }

        private void Main_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void Main_KeyUp(object sender, KeyEventArgs e)
        {
        }
        #endregion

        #region カーソル
        //描画するときは必ず止めること
        private void CursorFlash_Elapsed(object sender, ElapsedEventArgs e)
        {
            BitmapData backbmpdat;
            //Bitmapを直接操作
            backbmpdat = _backbmp!.LockBits(new Rectangle(0, 0, _backbmp.Width, _backbmp.Height),  ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            if (_lineLetterCount == _cursorPosition || _defaultInput == "")
            {
                if (!_isFlashed)
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * _cursorPosition), y + ((LetterHeight) * _outputLineCount), Color.White);
                        }
                    }
                    _isFlashed = true;
                }
                else
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * _cursorPosition), y + ((LetterHeight) * _outputLineCount), Color.Black);
                        }
                    }
                    _isFlashed = false;
                }
            }
            else
            {
                var ltr = _defaultInput[_cursorPosition].ToString();
                //Letter.ReplaceLetters(ref ltr);
                BitmapData fontdata = _fonts[Letter.ConvertLetterCode(ltr)];
                if (!_isFlashed)
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            if (BitmapDataEx.GetPixel(fontdata, x, y) == Color.FromArgb(255, 255, 255, 255))
                            {
                                BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.Black);
                            }
                            else
                            {
                                BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.White);
                            }
                        }
                    }
                    _isFlashed = true;
                }
                else
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            if (BitmapDataEx.GetPixel(fontdata, x, y) == Color.FromArgb(255, 255, 255, 255))
                            {
                                BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.White);
                            }
                            else
                            {
                                BitmapDataEx.SetPixel(backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.Black);
                            }
                        }
                    }
                    _isFlashed = false;
                }
            }
            if (backbmpdat != null)
                _backbmp.UnlockBits(backbmpdat);
            back.Image = _backbmp;
        }
        #endregion
        #endregion
    }
}