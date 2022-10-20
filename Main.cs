// Created by FrozenWhite
using System.Runtime.InteropServices;
using System.Timers;
using System.Diagnostics;
using System.Text;
using System.Drawing.Imaging;
using Teknomli.Properties;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Xml.Linq;

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
        /// メモリ(容量は忘れた)
        /// こんなにいるかなぁ()
        /// </summary>
        private IntPtr mem;
        #endregion
        #region 描画する時に使用するもの
        /// <summary>
        /// Back用のBitmap
        /// </summary>
        private Bitmap? _backBmpTmp;

        /// <summary>
        /// Render1用のBitmapの一時確保
        /// </summary>
        private Bitmap? _render1BmpTmp;

        /// <summary>
        /// Render2用のBitmapの一時確保
        /// </summary>
        private Bitmap? _render2BmpTmp;

        /// <summary>
        /// Render3用のBitmapの一時確保
        /// </summary>
        private Bitmap? _render3BmpTmp;

        /// <summary>
        /// Back用のBitmap
        /// </summary>
        private Bitmap? _backBmp;

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
        /// Back用のBitmapData
        /// </summary>
        private BitmapData? _backBmpData;

        /// <summary>
        /// Render1用のBitmapData
        /// </summary>
        private BitmapData? _render1BmpData;

        /// <summary>
        /// Render2用のBitmapData
        /// </summary>
        private BitmapData? _render2BmpData;

        /// <summary>
        /// Render3用のBitmapData
        /// </summary>
        private BitmapData? _render3BmpData;

        /// <summary>
        /// 一行にある文字数
        /// </summary>
        private int _lineLetterCount;

        /// <summary>
        /// 出力された行数
        /// </summary>
        private int _outputLineCount;

        /// <summary>
        /// カーソルがある場所
        /// </summary>
        private int _cursorPosition;

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

        private List<string>? _outputedLetters;

        private int returncnt = 0;
        #endregion
        #endregion
        #region C++のインポート
        [DllImport("winmm.dll")]
        public static extern int timeBeginPeriod(int uuPeriod);

        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        private Timer updater;
        #endregion
        #region コンストラクタ
        public Main()
        {
            InitializeComponent();
            this.Text = $@"{ProductName} v{ProductVersion}";
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
                fonts.equ.LockBits(new Rectangle(0, 0, fonts.equ.Width, fonts.equ.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // ^
                fonts.car.LockBits(new Rectangle(0, 0, fonts.car.Width, fonts.car.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // ~
                fonts.til.LockBits(new Rectangle(0, 0, fonts.til.Width, fonts.til.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // [
                fonts.sbs.LockBits(new Rectangle(0, 0, fonts.sbs.Width, fonts.sbs.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // ]
                fonts.sbe.LockBits(new Rectangle(0, 0, fonts.sbe.Width, fonts.sbe.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // |
                fonts.pip.LockBits(new Rectangle(0, 0, fonts.pip.Width, fonts.pip.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // @
                fonts.atm.LockBits(new Rectangle(0, 0, fonts.atm.Width, fonts.atm.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // :
                fonts.col.LockBits(new Rectangle(0, 0, fonts.col.Width, fonts.col.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // ;
                fonts.smc.LockBits(new Rectangle(0, 0, fonts.smc.Width, fonts.smc.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // _
                fonts.und.LockBits(new Rectangle(0, 0, fonts.und.Width, fonts.und.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
                // 彩
                fonts.sai.LockBits(new Rectangle(0, 0, fonts.sai.Width, fonts.sai.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb),
				#endregion
            };

            this.render1.Parent = render2;
            this.render2.Parent = render3;
            this.render3.Parent = back;
            this.render1.Dock = DockStyle.Fill;
            this.render2.Dock = DockStyle.Fill;
            this.render3.Dock = DockStyle.Fill;
            this.back.Dock = DockStyle.Fill;
            this.updater = new Timer
            {
                Interval = 80
            };
            this.updater.Elapsed += Update;

        }

        public sealed override string Text
        {
            get => base.Text;
            set => base.Text = value;
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
            //Bitmapを作成
            this._backBmp = new Bitmap(back.Width, back.Height);
            this._render1Bmp = new Bitmap(render1.Width, render1.Height);
            this._render2Bmp = new Bitmap(render2.Width, render2.Height);
            this._render3Bmp = new Bitmap(render3.Width, render3.Height);

            this._backBmpTmp = new Bitmap(back.Width, back.Height);
            this._render1BmpTmp = new Bitmap(render1.Width, render1.Height);
            this._render2BmpTmp = new Bitmap(render2.Width, render2.Height);
            this._render3BmpTmp = new Bitmap(render3.Width, render3.Height);

            this._backBmpData = this._backBmpTmp.LockBits(new Rectangle(0, 0, this._backBmp.Width, this._backBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            this._render1BmpData = this._render1BmpTmp.LockBits(new Rectangle(0, 0, this._render1Bmp.Width, this._render1Bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            this._render2BmpData = this._render2BmpTmp.LockBits(new Rectangle(0, 0, this._render2Bmp.Width, this._render2Bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            this._render3BmpData = this._render3BmpTmp.LockBits(new Rectangle(0, 0, this._render3Bmp.Width, this._render3Bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            this._render1Bmp.MakeTransparent(Color.Black);
            this._render2Bmp.MakeTransparent(Color.Black);
            this._render3Bmp.MakeTransparent(Color.Black);
            //セット
            this.back.Image = _backBmp;
            this.render1.Image = _render1Bmp;
            this.render2.Image = _render2Bmp;
            this.render3.Image = _render3Bmp;
            this._outputedLetters = new List<string>();
            this._outputedLetters.Add("");
            Audio audio1 = new("YukiLib.dll");
            await Task.Run(() => audio1.Play(2000, 150, 100, 7));
            await Task.Run(() => audio1.Play(1000, 150, 100, 7));
            this.updater.Start();
            await MemoryCheck();
            this.Echo("memory check OK"); NewLine();
            await CheckHeader();
            this.Init();
        }

        private async Task MemoryCheck()
        {
            int memchki;
            UInt32 allmemcnt = 655360 + 4063232;
            mem = Marshal.AllocHGlobal((int)allmemcnt);
            var sw = Stopwatch.StartNew();
            for (memchki = 0; memchki < allmemcnt; memchki++)
            {
                Marshal.WriteByte(mem, memchki, Convert.ToByte(255));
                if (sw.ElapsedMilliseconds >= 10)
                {
                    this.CleanScreen();
                    Echo($"MEMORY {memchki / 1024}KB OK");
                    templlc = 0;
                    _lineLetterCount = 0;
                    _defaultOutput = "";
                    sw.Restart();
                }
            }
            sw.Stop();
            Echo($"MEMORY {memchki / 1024}KB OK");
            NewLine();
        }

        /// <summary>
        /// OSのディスクファイルを検証
        /// </summary>
        private async Task CheckHeader()
        {
            Echo("Version Teknomli x4 Basic"); NewLine();
            Echo("Copyright (c) FrozenWhite.net"); NewLine();
            //ファイルを開く
            Echo("checking 8bytes"); NewLine();
            Echo("Load 1st drive"); NewLine();
            char[] cs = new char[8];
            using (StreamReader sr = new(@".\os.hdf", Encoding.ASCII))
            {
                int n = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (Encoding.ASCII.GetBytes(cs)[i] != Encoding.ASCII.GetBytes("THISISAPPC;")[i]) continue;
                    await Task.Run(() => Thread.Sleep(10));
                    n++;
                }
                NewLine();
                if (n == 8)
                    Echo("Completed check"); NewLine();
                Echo("Checking disk size"); NewLine();
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
            CleanScreen();
        }

        /// <summary>
        /// 起動
        /// </summary>6
        private void Init()
        {
            _outputLineCount = 0;
            _cursorPosition = 0;
            _defaultOutput = "";
            Echo("------------------------------------------------------------"); NewLine();
            Echo("Horai OS 彩"); NewLine();
            Echo("Copyright (c) FrozenWhite.net (BlueAlice)"); NewLine();
            Echo("GraAp DOS ver0.0.1"); NewLine();
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
        private void RunCommand(string commands)
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
                    var rsl = str;

                    rsl = rsl.Remove(0, 1);
                    rsl = rsl.Remove(rsl.Length - 2, 1);
                    Debug.WriteLine(rsl);
                    Echo(rsl);
                    break;
                case "draw":
                    switch (cmd[1].ToLower())
                    {
                        case "pie":
                            GraphicsManager.FillPie(this._render1BmpData, Color.White, 0, 0, 50, 70, 0, 2);
                            break;
                        case "rect":
                            GraphicsManager.FillRectangle(this._render1BmpData, Color.White, 0, 0, 50, 70);
                            break;
                        case "line":
                            GraphicsManager.DrawLine(this._render1BmpData, Color.White, 100, 100, 200, 250);
                            break;
                        case "circle":
                            GraphicsManager.FillEllipse(this._render1BmpData, Color.White, 0, 0, 50, 50);
                            break;
                        case "onmyou":
                            for (int x = 0; x < _render3Bmp.Width; x++)
                            {
                                for (int y = 0; y < _render3Bmp.Height; y++)
                                {
                                    BitmapDataEx.SetPixel(this._render3BmpData, x, y, Color.Green);
                                }
                            }

                            int r = 35;
                            GraphicsManager.FillEllipse(this._render3BmpData, Color.Black, 0, 0, r, r);
                            GraphicsManager.FillPie(this._render3BmpData, Color.White, 0, 0, r, r, 180, 360);
                            GraphicsManager.FillEllipse(this._render3BmpData, Color.White, r / 2, 0, r / 2 + 1, r / 2 + 1);
                            GraphicsManager.FillEllipse(this._render3BmpData, Color.Black, r / 2, r + 2, r / 2, r / 2);
                            GraphicsManager.FillEllipse(this._render3BmpData, Color.Black, (int)(r / 1.3), r / 3, r / 5, r / 5);
                            GraphicsManager.FillEllipse(this._render3BmpData, Color.White, (int)(r / 1.3), (int)(r / 0.75), r / 5, r / 5);
                            this._render3Bmp.UnlockBits(this._render3BmpData);
                            this.render3.Image = _render3Bmp;
                            SetEvent("clean;console=true", "key down 13");
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

        #region スクリプト
        private void SetEvent(string runScript, string trigger)
        {

        }

        private void RemoveEvent(string trigger)
        {

        }
        #endregion


        private void RunApplication(string name)
        {
            _cursorFlash!.Stop();
        }

        public void Reset()
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

            for (int x = 0; x < _backBmpData.Width; x++)
            {
                for (int y = 0; y < _backBmpData.Height; y++)
                {
                    BitmapDataEx.SetPixel(this._backBmpData, x, y, Color.FromArgb(0, 0, 0, 0));
                }
            }
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
                case 4:
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
            BitmapData render1bmpdat = _render1Bmp!.LockBits(new Rectangle(0, 0, _render1Bmp.Width, _render1Bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData backbmpdat = _backBmp!.LockBits(new Rectangle(0, 0, _backBmp.Width, _backBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            BitmapData bluecircleData = cirblue.LockBits(new Rectangle(0, 0, cirblue.Width, cirblue.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData redcircleData = cirred.LockBits(new Rectangle(0, 0, cirred.Width, cirred.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            for (float i = 0; i < 361; i++)
            {
                if (i % 10 != 0 || !(minrad <= i) || !(i <= maxrad))
                {
                    continue;
                }

                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        Color circleblu = BitmapDataEx.GetPixel(bluecircleData, x, y);
                        Color circlered = BitmapDataEx.GetPixel(redcircleData, x, y);
                        if (circleblu == Color.FromArgb(255, 0, 0, 0))
                        {
                            continue;
                        }

                        float xl = (float)(Math.Cos(Math.PI * 2 / 360 * i) * (n) + pt.X + x);
                        float yl = (float)(Math.Sin(Math.PI * 2 / 360 * i) * (n) + pt.Y + y);
                        if (del)
                        {
                            BitmapDataEx.SetPixel(isred ? render1bmpdat : backbmpdat, (int)xl, (int)yl,
                                Color.FromArgb(0));
                        }
                        else
                        {
                            if (circleblu == Color.FromArgb(255, 0, 0, 0))
                            {
                                continue;
                            }

                            if (isred)
                                BitmapDataEx.SetPixel(render1bmpdat, (int)xl, (int)yl, circlered);
                            else
                                BitmapDataEx.SetPixel(backbmpdat, (int)xl, (int)yl, circleblu);
                        }
                    }
                }
            }
            cirblue.UnlockBits(bluecircleData);
            _backBmp.UnlockBits(backbmpdat);
            _render1Bmp.UnlockBits(render1bmpdat);
            back.Image = _backBmp;
            _render1Bmp!.MakeTransparent(Color.Black);
            _render2Bmp!.MakeTransparent(Color.Black);
            _render3Bmp!.MakeTransparent(Color.Black);
            render1.Image = _render1Bmp;
            _cursorFlash.Start();
        }

        #endregion
        #region 描画

        private bool _isBackBmpLocked = false;
        private void Update(object? sender, ElapsedEventArgs e)
        {
            if (this._backBmpData == null || this._backBmp == null)
            {
                return;
            }

            if (_isBackBmpLocked)
            {
                return;
            }
            BitmapData backbmpdat = this._backBmp.LockBits(new Rectangle(0, 0, this._backBmp.Width, this._backBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            _isBackBmpLocked = true;
            for (int y = 0; y < backbmpdat.Height; y++)
            {
                for (int x = 0; x < backbmpdat.Width; x++)
                {
                    unsafe
                    {
                        byte* src = (byte*)this._backBmpData.Scan0;
                        int srcPos = x * 4 + this._backBmpData.Stride * y;
                        byte* dst = (byte*)backbmpdat.Scan0;
                        int dstPos = x * 4 + backbmpdat.Stride * y;
                        dst[dstPos + 0] = src[srcPos + 0];
                        dst[dstPos + 1] = src[srcPos + 1];
                        dst[dstPos + 2] = src[srcPos + 2];
                        dst[dstPos + 3] = src[srcPos + 3];
                    }
                }
            }
            this._backBmp.UnlockBits(backbmpdat);
            _isBackBmpLocked = false;
            this.back.Image = this._backBmp;
        }

        /// <summary>
        /// 指定された文字をコンソールに出力する
        /// </summary>
        /// <param name="str">出力する文字</param>
        private void Echo(string str)
        {
            _cursorFlash?.Stop();
            foreach (char chr in str)
            {
                switch (chr)
                {
                    //空欄だったらそのまま
                    case ' ':
                        {
                            for (int x = 0; x < LetterWidth; x++)
                            {
                                for (int y = 0; y < LetterHeight; y++)
                                {
                                    if (this._backBmpData != null)
                                    {
                                        BitmapDataEx.SetPixel(this._backBmpData, x + (LetterWidth * templlc) + 2, y + (LetterHeight * _outputLineCount), Color.Black);
                                    }
                                }
                            }
                            _lineLetterCount++;
                            templlc++;
                            break;
                        }
                    //改行は単純に行を増やすだけ
                    case '\n':
                        {
                            NewLine();
                            break;
                        }
                    //その他の文字は描画
                    default:
                        {
                            //文字ファイルを読み込み
                            BitmapData fontdata = _fonts[IFont.ConvertHoraiFontCode(chr)];
                            //Bitmapを直接操作
                            for (int x = 0; x < fontdata.Width; x++)
                            {
                                for (int y = 0; y < fontdata.Height; y++)
                                {
                                    if (this._backBmpData != null)
                                    {
                                        BitmapDataEx.SetPixel(this._backBmpData, x + (LetterWidth * templlc),
                                            y + (LetterHeight * _outputLineCount),
                                            BitmapDataEx.GetPixel(fontdata, x, y) != Color.FromArgb(255, 0, 0, 0)
                                                ? Color.White
                                                : Color.Black);
                                    }
                                }
                            }
                            _lineLetterCount++;
                            templlc++;
                            break;
                        }
                }
                //標準出力に書き込み
                _defaultOutput += chr.ToString();
                _cursorPosition++;
            }
            _cursorFlash?.Start();
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
        #endregion
        #region イベント
        #region Main
        private void Main_Load(object sender, EventArgs e)
        {
            this.TopMost = true;
            this.BringToFront();
            this.TopMost = false;
            this.Focus();
            Start();
        }

        //終了処理をここに記入
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            this._backBmpTmp.UnlockBits(this._backBmpData);
            this._render1BmpTmp.UnlockBits(this._render1BmpData);
            this._render2BmpTmp.UnlockBits(this._render2BmpData);
            this._render3BmpTmp.UnlockBits(this._render3BmpData);
            Marshal.FreeHGlobal(mem);
        }
        #endregion

        #region キーボードイベント

        private int curPos = 0;
        private async void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isConsole)
            {
                //点滅の一時停止
                _cursorFlash!.Stop();
                await Task.Run(() => Thread.Sleep(1));
                if (this._isFlashed)
                {
                    //カーソルの削除
                    if (this._lineLetterCount == this.tempcur || this._defaultInput == "")
                    {
                        for (int x = 0; x < LetterWidth; x++)
                        {
                            for (int y = 0; y < LetterHeight; y++)
                            {
                                if (this._backBmpData != null)
                                {
                                    BitmapDataEx.SetPixel(this._backBmpData, x + (LetterWidth * (this.tempcur)),
                                        y + (LetterHeight * this._outputLineCount), Color.FromArgb(0));
                                }
                            }
                        }
                    }
                    else
                    {
                        char ltr = this._defaultInput[this._cursorPosition];
                        int code = IFont.ConvertHoraiFontCode(ltr);
                        if (code <= 0)
                        {
                            BitmapData fontdata = this._fonts[code];
                            for (int x = 0; x < LetterWidth; x++)
                            {
                                for (int y = 0; y < LetterHeight; y++)
                                {
                                    if (this._backBmpData == null)
                                    {
                                        continue;
                                    }

                                    BitmapDataEx.SetPixel(this._backBmpData, x + this.curPos,
                                        y + (LetterHeight * this._outputLineCount),
                                        BitmapDataEx.GetPixel(fontdata, x, y) ==
                                        Color.FromArgb(255, 255, 255, 255)
                                            ? Color.White
                                            : Color.Black);
                                }
                            }
                        }
                    }
                    this._isFlashed = false;
                }

                byte? keyCode = Keyboard.Translate(e.KeyCode, false, e.Shift, e.Control, e.Alt);
                switch (keyCode)
                {
                    case 3 when this._cursorPosition != 0:
                        this._cursorPosition--;
                        this.tempcur--;
                        break;
                    case 4 when this._cursorPosition != this._lineLetterCount:
                        this._cursorPosition++;
                        this.tempcur++;
                        break;
                    case 11:
                        {
                            if (this._defaultInput != "")
                                this.RunCommand(this._defaultInput);
                            this.NewLine();
                            break;
                        }
                }

                if (keyCode == 14 && _cursorPosition != 0)
                {
                    _defaultInput = _defaultInput.Remove(_cursorPosition - 1, 1);
                    var oldcurpt = _cursorPosition;
                    _lineLetterCount = 0;
                    templlc = 0;
                    tempcur = 0;
                    _cursorPosition = 0;
                    Echo(new string(' ', this._defaultInput.Length + 2));
                    _lineLetterCount = 0;
                    templlc = 0;
                    tempcur = 0;
                    _cursorPosition = 0;
                    Echo(_defaultInput);
                    _cursorPosition = oldcurpt - 1;
                    tempcur = _cursorPosition;
                }
                else
                {
                    var horaiFontCode = IFont.TranslateKeyCode(keyCode);
                    if (horaiFontCode != null)
                    {
                        _defaultInput = _defaultInput.Insert(_cursorPosition, IFont.ConvertChar((int)horaiFontCode).ToString());
                        var oldCurP = _cursorPosition;
                        _lineLetterCount = 0;
                        templlc = 0;
                        tempcur = 0;
                        _cursorPosition = 0;
                        Echo(_defaultInput);
                        _cursorPosition = oldCurP + 1;
                        tempcur = _cursorPosition;
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
            if (!this._isConsole)
            {
                return;
            }

            if (this._lineLetterCount == this._cursorPosition || this._defaultInput == "")
            {
                if (!this._isFlashed)
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            if (this._backBmpData != null)
                            {
                                BitmapDataEx.SetPixel(this._backBmpData, x + (LetterWidth * this.tempcur), y + ((LetterHeight) * this._outputLineCount), Color.White);
                            }
                        }
                    }
                    this._isFlashed = true;
                }
                else
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            if (this._backBmpData != null)
                            {
                                BitmapDataEx.SetPixel(this._backBmpData, x + (LetterWidth * this.tempcur),
                                    y + ((LetterHeight) * this._outputLineCount), Color.Black);
                            }
                        }
                    }
                    this._isFlashed = false;
                }
            }
            else
            {
                char ltr = this._defaultInput[this.tempcur];
                BitmapData fontdata = this._fonts[IFont.ConvertHoraiFontCode(ltr)];
                if (!this._isFlashed)
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            if (this._backBmpData != null)
                            {
                                BitmapDataEx.SetPixel(this._backBmpData, x + (LetterWidth * this.tempcur),
                                    y + (LetterHeight * this._outputLineCount),
                                    BitmapDataEx.GetPixel(fontdata, x, y) == Color.FromArgb(255, 255, 255, 255)
                                        ? Color.Black
                                        : Color.White);
                            }
                        }
                    }

                    this.curPos = (LetterWidth * this.tempcur);
                    this._isFlashed = true;
                }
                else
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            if (this._backBmpData != null)
                            {
                                BitmapDataEx.SetPixel(this._backBmpData, x + (LetterWidth * this.tempcur),
                                    y + (LetterHeight * this._outputLineCount),
                                    BitmapDataEx.GetPixel(fontdata, x, y) == Color.FromArgb(255, 255, 255, 255)
                                        ? Color.White
                                        : Color.Black);
                            }
                        }
                    }
                    this._isFlashed = false;
                }
            }
        }
        #endregion
        #endregion
    }
}