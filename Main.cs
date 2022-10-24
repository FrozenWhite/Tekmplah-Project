// Created by FrozenWhite
using System.Runtime.InteropServices;
using System.Timers;
using System.Diagnostics;
using System.Text;
using System.Drawing.Imaging;
using Teknomli.Properties;
using Timer = System.Timers.Timer;
using System.Runtime.CompilerServices;

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
        private readonly List<Color> pallet = new();
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
        private Bitmap _backBmpTmp;

        /// <summary>
        /// Render1用のBitmapの一時確保
        /// </summary>
        private Bitmap _render1BmpTmp;

        /// <summary>
        /// Render2用のBitmapの一時確保
        /// </summary>
        private Bitmap _render2BmpTmp;

        /// <summary>
        /// Render3用のBitmapの一時確保
        /// </summary>
        private Bitmap _render3BmpTmp;

        /// <summary>
        /// Back用のBitmap
        /// </summary>
        private Bitmap _backBmp;

        /// <summary>
        /// Render1用のBitmap
        /// </summary>
        private Bitmap _render1Bmp;

        /// <summary>
        /// Render2用のBitmap
        /// </summary>
        private Bitmap _render2Bmp;

        /// <summary>
        /// Render3用のBitmap
        /// </summary>
        private Bitmap _render3Bmp;

        /// <summary>
        /// Back用のBitmapData
        /// </summary>
        private BitmapData _backBmpData;

        /// <summary>
        /// Render1用のBitmapData
        /// </summary>
        private BitmapData _render1BmpData;

        /// <summary>
        /// Render2用のBitmapData
        /// </summary>
        private BitmapData _render2BmpData;

        /// <summary>
        /// Render3用のBitmapData
        /// </summary>
        private BitmapData _render3BmpData;

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
        private Timer _cursorFlash;

        /// <summary>
        /// コンソール画面かどうか
        /// </summary>
        private bool _isConsole = false;

        /// <summary>
        /// フォントセット
        /// </summary>
        private BitmapData[] _fonts;

        private List<string>? _outputedLetters;

        #endregion
        #endregion
        #region C++のインポート
        [DllImport("winmm.dll")]
        public static extern int timeBeginPeriod(int uuPeriod);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }
        [DllImport("User32.dll")]
        static extern bool GetCursorPos(out POINT lppoint);

        #endregion
        private Timer _updater;
        private Timer _watcher;
        #region コンストラクタ
        public Main()
        {
            this.InitializeComponent();
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
            //Bitmapを作成
            this._backBmp = new Bitmap(back.Width, back.Height);
            this._render1Bmp = new Bitmap(render1.Width, render1.Height);
            this._render2Bmp = new Bitmap(render2.Width, render2.Height);
            this._render3Bmp = new Bitmap(render3.Width, render3.Height);

            #region BitmapData
            this._backBmpTmp = new Bitmap(back.Width, back.Height);
            this._render1BmpTmp = new Bitmap(render1.Width, render1.Height);
            this._render2BmpTmp = new Bitmap(render2.Width, render2.Height);
            this._render3BmpTmp = new Bitmap(render3.Width, render3.Height);

            this._backBmpData = this._backBmpTmp.LockBits(new Rectangle(0, 0, this._backBmp.Width, this._backBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            this._render1BmpData = this._render1BmpTmp.LockBits(new Rectangle(0, 0, this._render1Bmp.Width, this._render1Bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            this._render2BmpData = this._render2BmpTmp.LockBits(new Rectangle(0, 0, this._render2Bmp.Width, this._render2Bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            this._render3BmpData = this._render3BmpTmp.LockBits(new Rectangle(0, 0, this._render3Bmp.Width, this._render3Bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            #endregion

            //セット
            this.back.Image = this._backBmp;
            this.render1.Image = this._render1Bmp;
            this.render2.Image = this._render2Bmp;
            this.render3.Image = this._render3Bmp;

            this._watcher = new Timer(1000);
            this._watcher.Elapsed += this._watcher_Elapsed;
            this._watcher.Start();
            this._updater = new Timer(50);
            this._updater.Elapsed += Update;
            //カーソルの点滅
            this._cursorFlash = new Timer(500);
            this._cursorFlash.Elapsed += CursorFlash_Elapsed!;
            this._cursorFlash.Start();
        }

        private void _watcher_Elapsed(object? sender, ElapsedEventArgs e)
        {

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
            this.TopMost = true;
            this.BringToFront();
            this.Activate();
            this.TopMost = false;
            this.Text = $@"{ProductName} v{ProductVersion}";
            RunScript("echo('test \\ hello');");
            this.render1.Parent = render2;
            this.render2.Parent = render3;
            this.render3.Parent = back;
            this.render1.Dock = DockStyle.Fill;
            this.render2.Dock = DockStyle.Fill;
            this.render3.Dock = DockStyle.Fill;
            this.back.Dock = DockStyle.Fill;
            this._outputedLetters = new List<string>();
            this._outputedLetters.Add("");
            Audio audio1 = new("YukiLib.dll");
            Audio audio2 = new("YukiLib.dll");
            audio1.Play(1046, 150, 1000, 7);
            audio2.Play(1760, 150, 1000, 7);

            Debug.WriteLine(audio2.hdr.lpData);
            this._updater.Start();
            MemoryCheck();
            Echo($"MEMORY {memchki / 1024}KB OK");
            NewLine();
            this.Echo("memory check OK"); NewLine();
            await CheckHeader();
            this.Init();
        }

        int memchki;
        private void MemoryCheck()
        {
            UInt32 allmemcnt = 4063232;
            mem = Marshal.AllocHGlobal((int)allmemcnt);
            for (memchki = 0; memchki < allmemcnt; memchki++)
            {
                Marshal.WriteByte(mem, memchki, Convert.ToByte(255));
            }
        }

        /// <summary>
        /// OSのディスクファイルを検証
        /// </summary>
        private async Task CheckHeader()
        {
            Echo("Teknomli x4 Basic"); NewLine();
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
        /// HoraiOSを起動
        /// </summary>
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
            string[] cmd = commands.Split(' ');
            switch (cmd[0].ToLower())
            {
                case "run":
                    RunApplication(cmd[^1]);
                    break;
                case "echo":
                    NewLine();
                    string str = "";
                    for (int i = 1; i < cmd.Length; i++)
                        str += cmd[i] + " ";
                    string rsl = str;
                    rsl = rsl.Remove(0, 1);
                    rsl = rsl.Remove(rsl.Length - 2, 1);
                    this.Echo(rsl);
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
                            if (cmd.Length - 1 <= 2)
                            {
                                string[] args = cmd[2].Split(';');
                                string getProperty = args[0].Substring(args[0].IndexOf("(", StringComparison.Ordinal) + 1, args[0].IndexOf(")", StringComparison.Ordinal) - args[0].IndexOf("(", StringComparison.Ordinal) - 1);
                                string[] points = getProperty.Split(',');
                                int.TryParse(points[0], out int xc);
                                int.TryParse(points[1], out int yc);
                                int.TryParse(args[1], out int csize);
                                GraphicsManager.FillEllipse(this._render1BmpData, Color.White, xc, yc, csize, csize);
                            }
                            break;
                        case "rand":
                            for (int x = 0; x < this._backBmpData.Width; x++)
                            {
                                for (int y = 0; y < this._backBmpData.Height; y++)
                                {
                                    BitmapPlus.SetPixel(this._backBmpData, x, y, Color.FromArgb(this.rand.Next(255), this.rand.Next(255), this.rand.Next(255)));
                                }
                                await Task.Run(() => Thread.Sleep(1));
                            }
                            break;
                        case "onmyou":
                            for (int x = 0; x < _render3Bmp.Width; x++)
                            {
                                for (int y = 0; y < _render3Bmp.Height; y++)
                                {
                                    BitmapPlus.SetPixel(this._render3BmpData, x, y, Color.Green);
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
        private void RunApplication(string name)
        {
            _cursorFlash!.Stop();
        }

        public void Reset()
        {

        }

        #endregion
        #region スクリプト
        private void SetEvent(string runScript, string trigger)
        {

        }

        private void RemoveEvent(string trigger)
        {

        }

        private void RunScripts(string path)
        {
            /*
            //ファイルをインポートできるようにする
            #import('std.fp');
            #import('sound.fp');
            #import('graphics.fp');
            #import('vector.fp');
            import('my_math.fp');

            //関数は上に書く
            int:func test(int:a,int:b){
              return(a*b);
            }

            //最後にmainを書く
            void:func main()
            {
              bool test_bool = false;
              if(test_bool == false)
              {
                print_l('test_bool ez' + string([test_bool]));
              }
              for (int i; i < 10;i += 2;)
              {
                print('i ez >> ' + string([i]));
              }
              //くくったときはこうする
              //インデントはスペースx2
              print(string([test(2,3)]) + 'test');
              //非同期処理はasyncをつける
              async Sound.Play(2000,100);
              //四角を書く
              async Graphics.FillRect(1,10,10,100,200);
              //画像を読み込む
              async Graphics.SetImage(4,'.\path\test.hif',0,0);
            }
            */
        }

        private void CallLibrary()
        {

        }

        private void RunScript(string script)
        {
            string[] methods = { };
            string getMethod = script.Substring(0, script.IndexOf("(", StringComparison.Ordinal));
            switch (getMethod)
            {
                case "echo":
                case "print":
                    {
                        string getProperty = script.Substring(script.IndexOf("(", StringComparison.Ordinal) + 1, script.IndexOf(")", StringComparison.Ordinal) - script.IndexOf("(", StringComparison.Ordinal));
                        string value = "";
                        for (int i = 0; i < getProperty.Length - 1; i++)
                        {
                            if (getProperty[i] == '\\')
                            {
                                if (getProperty[i + 1] == '\'')
                                {
                                    value += "'";
                                }

                                if (getProperty[i + 1] == '\\')
                                {
                                    value += "\\";
                                }
                                i++;
                                continue;
                            }

                            value += getProperty[i];
                        }
                        Debug.WriteLine(value);
                        break;
                    }
            }

        }
        #endregion
        #region 描画
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
            for (int x = 0; x < this._backBmpData.Width; x++)
            {
                for (int y = 0; y < this._backBmpData.Height; y++)
                {
                    BitmapPlus.SetPixel(this._backBmpData, x, y, Color.FromArgb(0, 0, 0));
                }
            }
            _isConsole = true;
            if (_cursorFlash != null)
                _cursorFlash.Start();
        }

        private void SetPixel(Point point, Color color, int layer)
        {
            _cursorFlash.Stop();
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


        private unsafe void Update(object? sender, ElapsedEventArgs e)
        {
            #region Back
            if (this._backBmpData == null || this._backBmp == null) return;
            using (BitmapDataEx backBitmapDataEx = BitmapDataEx.LockBits(this._backBmp))
            {
                BitmapData backBitmapData = backBitmapDataEx.BitmapData;
                Unsafe.CopyBlock(backBitmapData.Scan0.ToPointer(), this._backBmpData.Scan0.ToPointer(),
                    (uint)(this._backBmpData.Stride * this._backBmpData.Height));
            }
            this.back.Image = this._backBmp;
            #endregion

            #region Render1
            if (this._render1BmpData == null || this._render1Bmp == null) return;
            using (BitmapDataEx render1BitmapDataEx = BitmapDataEx.LockBits(this._render1Bmp))
            {
                BitmapData render1BitmapData = render1BitmapDataEx.BitmapData;
                Unsafe.CopyBlock(render1BitmapData.Scan0.ToPointer(), this._render1BmpData.Scan0.ToPointer(), (uint)(this._render1BmpData.Stride * this._render1BmpData.Height));
            }
            this.render1.Image = this._render1Bmp;
            #endregion

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
                                        BitmapPlus.SetPixel(this._backBmpData, x + (LetterWidth * templlc) + 2, y + (LetterHeight * _outputLineCount), Color.Black);
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
                                        BitmapPlus.SetPixel(this._backBmpData, x + (LetterWidth * templlc),
                                            y + (LetterHeight * _outputLineCount),
                                            BitmapPlus.GetPixel(fontdata, x, y) != Color.FromArgb(255, 0, 0, 0)
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
        }

        private int templlc;
        private int tempcur;
        #endregion
        #region イベント
        #region Main
        private void Main_Load(object sender, EventArgs e)
        {
            this.Start();
        }

        private void Main_Activated(object sender, EventArgs e)
        {
            this._updater.Start();
        }

        private void Main_Deactivate(object sender, EventArgs e)
        {
            this._updater.Stop();
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._updater.Stop();
            this._cursorFlash.Stop();
            this._backBmpTmp.UnlockBits(this._backBmpData);
            this._render1BmpTmp.UnlockBits(this._render1BmpData);
            this._render2BmpTmp.UnlockBits(this._render2BmpData);
            this._render3BmpTmp.UnlockBits(this._render3BmpData);
            this._backBmpTmp.Dispose();
            this._render1BmpTmp.Dispose();
            this._render2BmpTmp.Dispose();
            this._render3BmpTmp.Dispose();
            this._backBmp.Dispose();
            this._render1Bmp.Dispose();
            this._render2Bmp.Dispose();
            this._render3Bmp.Dispose();
            Marshal.FreeHGlobal(this.mem);
        }
        #endregion

        #region キーボードイベント

        private int _curPos;
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
                                    BitmapPlus.SetPixel(this._backBmpData, x + (LetterWidth * (this.tempcur)),
                                        y + (LetterHeight * this._outputLineCount), Color.Black);
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

                                    BitmapPlus.SetPixel(this._backBmpData, x + this._curPos,
                                        y + (LetterHeight * this._outputLineCount),
                                        BitmapPlus.GetPixel(fontdata, x, y) ==
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
                                BitmapPlus.SetPixel(this._backBmpData, x + (LetterWidth * this.tempcur), y + ((LetterHeight) * this._outputLineCount), Color.White);
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
                                BitmapPlus.SetPixel(this._backBmpData, x + (LetterWidth * this.tempcur),
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
                                BitmapPlus.SetPixel(this._backBmpData, x + (LetterWidth * this.tempcur),
                                    y + (LetterHeight * this._outputLineCount),
                                    BitmapPlus.GetPixel(fontdata, x, y) == Color.FromArgb(255, 255, 255, 255)
                                        ? Color.Black
                                        : Color.White);
                            }
                        }
                    }

                    this._curPos = (LetterWidth * this.tempcur);
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
                                BitmapPlus.SetPixel(this._backBmpData, x + (LetterWidth * this.tempcur),
                                    y + (LetterHeight * this._outputLineCount),
                                    BitmapPlus.GetPixel(fontdata, x, y) == Color.FromArgb(255, 255, 255, 255)
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
        #region 仮
        private static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }
        #endregion
    }
}