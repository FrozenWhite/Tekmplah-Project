// (c) Team FrozenWhite
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.Diagnostics;
using System.Text;
using System.Drawing.Imaging;
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
        一応C++から移植してるけど無理だったらライブラリになります

        render1.Parent = render2;
        render2.Parent = render3;
        render3.Parent = back;
        render1.Dock = DockStyle.Fill;
        render2.Dock = DockStyle.Fill;
        render3.Dock = DockStyle.Fill;
        back.Dock = DockStyle.Fill;
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
        private System.Timers.Timer? _cursorFlash;

        /// <summary>
        /// コンソール画面かどうか
        /// </summary>
        private bool _isConsole = false;

        /// <summary>
        /// フォントセット
        /// </summary>
        private Dictionary<string, Bitmap>? _fonts;

        private MethodInfo? Audio;

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

            return typBld.CreateType()?.GetMethod(invInfo.ProcName);
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
                ReturnType = typeof(Int32),
                ParameterTypes = new[] { typeof(float), typeof(int), typeof(int) },
                CallingConvention = CallingConvention.StdCall,
                CharSet = CharSet.Unicode
            };
            var tmp = Stopwatch.StartNew();
            await Task.Run(() => Thread.Sleep(10));
            tmp.Stop();
            Debug.WriteLine(tmp.ElapsedMilliseconds);
            //Invokeで実行
            Audio = CreateMethodInfo(invInfo);
            await Task.Run(() => Thread.Sleep(100));
            Audio?.Invoke(null, new object[] { 2000,100,7 });
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
                    Echo($"MEMORY {memchki / 1000}KB OK");
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

        /// <summary>
        /// システムデフォルトのフォントを読み込む
        /// </summary>
        private void LoadDefaultFonts()
        {
            _fonts = new Dictionary<string, Bitmap>
            {
                //Number
                { "0", Properties.fonts._0 },
                { "1", Properties.fonts._1 },
                { "2", Properties.fonts._2 },
                { "3", Properties.fonts._3 },
                { "4", Properties.fonts._4 },
                { "5", Properties.fonts._5 },
                { "6", Properties.fonts._6 },
                { "7", Properties.fonts._7 },
                { "8", Properties.fonts._8 },
                { "9", Properties.fonts._9 },
                //Large Alphabet
                { "A", Properties.fonts.A },
                { "B", Properties.fonts.B },
                { "C", Properties.fonts.C },
                { "D", Properties.fonts.D },
                { "E", Properties.fonts.E },
                { "F", Properties.fonts.F },
                { "G", Properties.fonts.G },
                { "H", Properties.fonts.H },
                { "I", Properties.fonts.I },
                { "J", Properties.fonts.J },
                { "K", Properties.fonts.K },
                { "L", Properties.fonts.L },
                { "M", Properties.fonts.M },
                { "N", Properties.fonts.N },
                { "O", Properties.fonts.O },
                { "P", Properties.fonts.P },
                { "Q", Properties.fonts.Q },
                { "R", Properties.fonts.R },
                { "S", Properties.fonts.S },
                { "T", Properties.fonts.T },
                { "U", Properties.fonts.U },
                { "V", Properties.fonts.V },
                { "W", Properties.fonts.W },
                { "X", Properties.fonts.X },
                { "Y", Properties.fonts.Y },
                { "Z", Properties.fonts.Z },
                //Small Alphabet
                { "a", Properties.fonts.a_s },
                { "b", Properties.fonts.b_s },
                { "c", Properties.fonts.c_s },
                { "d", Properties.fonts.d_s },
                { "e", Properties.fonts.e_s },
                { "f", Properties.fonts.f_s },
                { "g", Properties.fonts.g_s },
                { "h", Properties.fonts.h_s },
                { "i", Properties.fonts.i_s },
                { "j", Properties.fonts.j_s },
                { "k", Properties.fonts.k_s },
                { "l", Properties.fonts.l_s },
                { "m", Properties.fonts.m_s },
                { "n", Properties.fonts.n_s },
                { "o", Properties.fonts.o_s },
                { "p", Properties.fonts.p_s },
                { "q", Properties.fonts.q_s },
                { "r", Properties.fonts.r_s },
                { "s", Properties.fonts.s_s },
                { "t", Properties.fonts.t_s },
                { "u", Properties.fonts.u_s },
                { "v", Properties.fonts.v_s },
                { "w", Properties.fonts.w_s },
                { "x", Properties.fonts.x_s },
                { "y", Properties.fonts.y_s },
                { "z", Properties.fonts.z_s },
                //!
                { "exc", Properties.fonts.exc },
                //"
                { "quo", Properties.fonts.quo },
                //#
                { "has", Properties.fonts.has },
                //$
                { "dol", Properties.fonts.dol },
                //%
                { "per", Properties.fonts.per },
                //&
                { "and", Properties.fonts.and },
                //'
                { "sin", Properties.fonts.sin },
                //(
                { "brs", Properties.fonts.brs },
                //)
                { "bre", Properties.fonts.bre },
                //<
                { "gre", Properties.fonts.gre },
                //>
                { "les", Properties.fonts.les },
                //<
                { "cbs", Properties.fonts.cbs },
                //>
                { "cbe", Properties.fonts.cbe },
                //?
                { "que", Properties.fonts.que },
                // (space)
                { "spa", Properties.fonts.spa }
            };
        }

        /// <summary>
        /// OSのディスクファイルを検証
        /// </summary>
        private async Task CheckHeader()
        {
            //起動音
            Echo("Version Teknomli x4 Basic"); NewLine();
            Echo("Copyright 2022 FallWhite Team"); NewLine();
            //ファイルを開く
            Echo("checking 8bytes"); NewLine();
            Echo("Load 1st"); NewLine();
            char[] cs = new char[8];
            using (StreamReader sr = new StreamReader(@".\os.hdf", Encoding.ASCII))
            {
                int n = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (Encoding.ASCII.GetBytes(cs)[i] == Encoding.ASCII.GetBytes("THISISAPPC;")[i])
                    {
                        await Task.Run(() => Thread.Sleep(10));
                        n++;
                        Echo("###");
                    }
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
#if DEBUG
            Echo("Horai Debug OS v001"); NewLine();
#else
            Echo("Horai OS v001"); NewLine();
#endif
            _isConsole = true;
            //カーソルの点滅
            _cursorFlash = new System.Timers.Timer(500);
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
            string[] cmd = commands.Split(' ');
            switch (cmd[0].ToLower())
            {
                case "run":
                    RunApplication(cmd[cmd.Length - 1]);
                    break;
                case "echo":
                    NewLine();
                    string str = "";
                    for (int i = 1; i < cmd.Length; i++)
                        str += cmd[i] + " ";
                    Echo(str);
                    break;
                case "set":
                    for (int i = 0; i < 30; i++)
                    {
                        drawCircle(new Point(150, 150), i * 20, false, 30, 120);
                        drawCircle(new Point(350, 150), i * 20, false, /*50, 140 */ 0, 360, true);
                        await Task.Run(() => Thread.Sleep(1));
                        drawCircle(new Point(150, 150), i * 20, true/*, 30, 120*/);
                        drawCircle(new Point(350, 150), i * 20, true,/* 50, 140*/0, 360, true);
                    }
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
                    }
                    break;
                case "clean":
                    _cursorFlash!.Stop();
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
                    _cursorFlash.Start();
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
            BitmapData render1bmpdat = _render1Bmp!.LockBits(new Rectangle(0, 0, _render1Bmp.Width, _render1Bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData backbmpdat = _backbmp!.LockBits(new Rectangle(0, 0, _backbmp.Width, _backbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            BitmapData bluecircleData = cirblue.LockBits(new Rectangle(0, 0, cirblue.Width, cirblue.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData redcircleData = cirred.LockBits(new Rectangle(0, 0, cirred.Width, cirred.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            for (float i = 0; i < 361; i++)
            {
                if (i % 10 == 0 && minrad <= i && i <= maxrad)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            Color circleblu = BitmapDataEx.GetPixel(cirblue, bluecircleData, x, y);
                            Color circlered = BitmapDataEx.GetPixel(cirred, redcircleData, x, y);
                            if (circleblu != Color.FromArgb(255, 0, 0, 0))
                            {
                                float xl = (float)(Math.Cos(Math.PI * 2 / 360 * i) * (n) + pt.X + x);
                                float yl = (float)(Math.Sin(Math.PI * 2 / 360 * i) * (n) + pt.Y + y);
                                if (del)
                                {
                                    if (isred)
                                        BitmapDataEx.SetPixel(_render1Bmp, render1bmpdat, (int)xl, (int)yl, Color.FromArgb(0));
                                    else
                                        BitmapDataEx.SetPixel(_backbmp, backbmpdat, (int)xl, (int)yl, Color.FromArgb(0));
                                }
                                else
                                {
                                    if (circleblu != Color.FromArgb(255, 0, 0, 0))
                                    {
                                        if (isred)
                                            BitmapDataEx.SetPixel(_render1Bmp, render1bmpdat, (int)xl, (int)yl, circlered);
                                        else
                                            BitmapDataEx.SetPixel(_backbmp, backbmpdat, (int)xl, (int)yl, circleblu);
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
        private static void ReplaceLetters(ref string ltr)
        {
            /*

                //%
                { "per", Properties.fonts.per },
                //&
                { "and", Properties.fonts.and },
                //'
                { "sin", Properties.fonts.sin },
                //(
                { "brs", Properties.fonts.brs },
                //)
                { "bre", Properties.fonts.bre },
                //<
                { "gre", Properties.fonts.gre },
                //>
                { "les", Properties.fonts.les },
                //<
                { "cbs", Properties.fonts.cbs },
                //>
                { "cbe", Properties.fonts.cbe },
                //?
                { "que", Properties.fonts.que },
                // (space)
                { "spa", Properties.fonts.spa }
             */
            switch (ltr)
            {
                case "?":
                    ltr = "que";
                    break;
                case "!":
                    ltr = "exc";
                    break;
                case "\"":
                    ltr = "quo";
                    break;
                case "#":
                    ltr = "has";
                    break;
                case "$":
                    ltr = "dol";
                    break;
                case "%":
                    ltr = "per";
                    break;
                case "&":
                    ltr = "and";
                    break;
                case "'":
                    ltr = "sin";
                    break;
                case "(":
                    ltr = "brs";
                    break;
                case ")":
                    ltr = "bre";
                    break;
                case " ":
                    ltr = "spa";
                    break;
                case ">":
                    ltr = "gre";
                    break;
                case "<":
                    ltr = "les";
                    break;
            }
        }

        private static Bitmap RotateImage(Bitmap bmp, float angle)
        {
            float alpha = angle;
            while (alpha < 0) alpha += 360;

            float gamma = 90;
            float beta = 180 - angle - gamma;

            float c1 = bmp.Height;
            float a1 = (float)(c1 * Math.Sin(alpha * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));
            float b1 = (float)(c1 * Math.Sin(beta * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));

            float c2 = bmp.Width;
            float a2 = (float)(c2 * Math.Sin(alpha * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));
            float b2 = (float)(c2 * Math.Sin(beta * Math.PI / 180) / Math.Sin(gamma * Math.PI / 180));

            int width = Convert.ToInt32(b2 + a1);
            int height = Convert.ToInt32(b1 + a2);

            Bitmap rotatedImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.TranslateTransform(rotatedImage.Width / 2, rotatedImage.Height / 2); //set the rotation point as the center into the matrix
                g.RotateTransform(angle); //rotate
                g.TranslateTransform(-rotatedImage.Width / 2, -rotatedImage.Height / 2); //restore rotation point into the matrix
                g.DrawImage(bmp, new Point((width - bmp.Width) / 2, (height - bmp.Height) / 2)); //draw the image on the new bitmap
            }
            return rotatedImage;
        }

        /// <summary>
        /// 指定された文字をコンソールに出力する
        /// </summary>
        /// <param name="str">出力する文字</param>
        /// <param name="useDefaultFont">本体のROM内蔵のフォントを使うかどうか</param>
        private void Echo(string str, bool useDefaultFont = true)
        {
            foreach (var chr in str)
            {
                SetLetter(chr, Color.White, Color.Black, useDefaultFont);
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
            _cursorPosition = 0;
            _defaultInput = "";
            _defaultOutput = "";
        }

        private void RedrawLine(int line)
        {

        }

        /// <summary>
        /// 指定されたcharを出力する
        /// </summary>
        /// <param name="letter">出力するChar</param>
        /// <param name="LetterColor"></param>
        internal void SetLetter(char letter, Color LetterColor, Color backColor, bool UseDefaultFont = true, string fontPath = "")
        {
            _cursorFlash?.Stop();
            //charをstringに変換
            var ltr = letter.ToString();
            _outputedLetters!.Add("");
            _outputedLetters[_outputLineCount] = _outputedLetters[_outputLineCount].Insert(0, ltr);
            ReplaceLetters(ref ltr);

            var llcbak = _lineLetterCount;
            var cpbak = _cursorPosition;
            if (_defaultInput.Length * LetterWidth > _backbmp!.Width - (LetterWidth / 2))
            {
                _outputLineCount++;
                _lineLetterCount = 0;
                _cursorPosition = 0;
            }

            if (!_fonts!.ContainsKey(ltr))
                return;
            BitmapData backbmpdat = _backbmp!.LockBits(new Rectangle(0, 0, _backbmp.Width, _backbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            switch (ltr)
            {
                //空欄だったらそのまま
                case " ":
                    {
                        for (var x = 0; x < LetterWidth; x++)
                        {
                            for (var y = 0; y < LetterHeight; y++)
                            {
                                BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _lineLetterCount) + 2, y + (LetterHeight * _outputLineCount), backColor);
                            }
                        }
                        _lineLetterCount++;
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
                        Bitmap letterbmp = _fonts[ltr];
                        //Bitmapを直接操作
                        BitmapData fontdata = letterbmp.LockBits(new Rectangle(0, 0, letterbmp.Width, letterbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                        for (var x = 0; x < LetterWidth; x++)
                        {
                            for (var y = 0; y < LetterHeight; y++)
                            {
                                BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _lineLetterCount),
                                    y + (LetterHeight * _outputLineCount),
                                    BitmapDataEx.GetPixel(letterbmp, fontdata, x, y) != Color.FromArgb(255, 0, 0, 0)
                                        ? LetterColor
                                        : backColor);
                            }
                        }
                        _lineLetterCount++;
                        //解放

                        letterbmp.UnlockBits(fontdata);
                        break;
                    }
            }

            //標準出力に書き込み
            _defaultOutput += letter.ToString();
            if (_defaultInput.Length * LetterWidth > _backbmp.Width - (LetterWidth / 2))
            {
                _outputLineCount--;
                _lineLetterCount = llcbak;
                _cursorPosition = cpbak;
            }

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
                    backbmpdat = _backbmp!.LockBits(new Rectangle(0, 0, _backbmp.Width, _backbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    //カーソルの削除
                    if (_lineLetterCount == _cursorPosition || _defaultInput == "")
                    {
                        for (int x = 0; x < LetterWidth; x++)
                        {
                            for (int y = 0; y < LetterHeight; y++)
                            {
                                BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.FromArgb(0));
                            }
                        }
                    }
                    else
                    {
                        var ltr = _defaultInput[_cursorPosition].ToString();
                        //記号とかを置き換え
                        ReplaceLetters(ref ltr);
                        Bitmap letterbmp = _fonts![ltr];
                        //Bitmapを直接操作
                        BitmapData fontdata = letterbmp.LockBits(new Rectangle(0, 0, letterbmp.Width, letterbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                        for (int x = 0; x < LetterWidth; x++)
                        {
                            for (int y = 0; y < LetterHeight; y++)
                            {
                                if (BitmapDataEx.GetPixel(letterbmp, fontdata, x, y) == Color.FromArgb(255, 255, 255, 255))
                                {
                                    BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.White);
                                }
                                else
                                {
                                    BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.Black);
                                }
                            }
                        }
                        if (fontdata != null)
                            letterbmp.UnlockBits(fontdata);
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
                if (e.Shift)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.A:
                            _defaultInput += "A";
                            SetLetter('A', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.B:
                            _defaultInput += "B";
                            SetLetter('B', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.C:
                            _defaultInput += "C";
                            SetLetter('C', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D:
                            _defaultInput += "D";
                            SetLetter('D', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.E:
                            _defaultInput += "E";
                            SetLetter('E', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.F:
                            _defaultInput += "F";
                            SetLetter('F', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.G:
                            _defaultInput += "G";
                            SetLetter('G', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.H:
                            _defaultInput += "H";
                            SetLetter('H', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.I:
                            _defaultInput += "I";
                            SetLetter('I', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.J:
                            _defaultInput += "J";
                            SetLetter('J', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.K:
                            _defaultInput += "K";
                            SetLetter('K', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.L:
                            _defaultInput += "L";
                            SetLetter('L', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.M:
                            _defaultInput += "M";
                            SetLetter('M', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.N:
                            _defaultInput += "N";
                            SetLetter('N', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.O:
                            _defaultInput += "O";
                            SetLetter('O', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.P:
                            _defaultInput += "P";
                            SetLetter('P', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Q:
                            _defaultInput += "Q";
                            SetLetter('Q', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.R:
                            _defaultInput += "R";
                            SetLetter('R', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.S:
                            _defaultInput += "S";
                            SetLetter('S', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.T:
                            _defaultInput += "T";
                            SetLetter('T', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.U:
                            _defaultInput += "U";
                            SetLetter('U', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.V:
                            _defaultInput += "V";
                            SetLetter('V', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.W:
                            _defaultInput += "W";
                            SetLetter('W', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.X:
                            _defaultInput += "X";
                            SetLetter('X', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Y:
                            _defaultInput += "Y";
                            SetLetter('Y', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Z:
                            _defaultInput += "Z";
                            SetLetter('Z', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D0:
                            break;
                        case Keys.D1:
                            _defaultInput += "!";
                            SetLetter('!', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D2:
                            _defaultInput += "\"";
                            SetLetter('"', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D3:
                            _defaultInput += "#";
                            SetLetter('#', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D4:
                            _defaultInput += "$";
                            SetLetter('$', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D5:
                            _defaultInput += "%";
                            SetLetter('%', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D6:
                            _defaultInput += "&";
                            SetLetter('&', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D7:
                            _defaultInput += "'";
                            SetLetter('\'', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D8:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "(");
                            SetLetter('(', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D9:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, ")");
                            SetLetter(')', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Space:
                            _defaultInput += " ";
                            SetLetter(' ', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Enter:
                            RunCommand(_defaultInput);
                            NewLine();
                            break;
                        case Keys.Control:
                            break;
                        case Keys.OemPeriod:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, ">");
                            SetLetter('>', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Oemcomma:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "<");
                            SetLetter('<', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Back:
                            break;
                        case Keys.Up:
                            break;
                        case Keys.Down:
                            break;
                        case Keys.Left:
                            if (_cursorPosition != 0)
                                _cursorPosition--;
                            break;
                        case Keys.Right:
                            if (_cursorPosition != _lineLetterCount)
                                _cursorPosition++;
                            break;
                    }
                }
                else
                {
                    switch (e.KeyCode)
                    {
                        case Keys.A:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "a");
                            SetLetter('a', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.B:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "b");
                            SetLetter('b', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.C:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "c");
                            SetLetter('c', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "d");
                            SetLetter('d', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.E:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "e");
                            SetLetter('e', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.F:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "f");
                            SetLetter('f', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.G:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "g");
                            SetLetter('g', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.H:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "h");
                            SetLetter('h', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.I:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "i");
                            SetLetter('i', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.J:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "j");
                            SetLetter('j', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.K:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "k");
                            SetLetter('k', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.L:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "l");
                            SetLetter('l', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.M:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "m");
                            SetLetter('m', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.N:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "n");
                            SetLetter('n', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.O:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "o");
                            SetLetter('o', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.P:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "p");
                            SetLetter('p', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Q:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "q");
                            SetLetter('q', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.R:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "r");
                            SetLetter('r', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.S:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "s");
                            SetLetter('s', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.T:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "t");
                            SetLetter('t', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.U:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "u");
                            SetLetter('u', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.V:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "v");
                            SetLetter('v', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.W:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "w");
                            SetLetter('w', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.X:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "x");
                            SetLetter('x', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Y:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "y");
                            SetLetter('y', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Z:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "z");
                            SetLetter('z', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D0:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "0");
                            SetLetter('0', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D1:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "1");
                            SetLetter('1', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D2:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "2");
                            SetLetter('2', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D3:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "3");
                            SetLetter('3', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D4:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "4");
                            SetLetter('4', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D5:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "5");
                            SetLetter('5', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D6:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "6");
                            SetLetter('6', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D7:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "7");
                            SetLetter('7', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D8:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "8");
                            SetLetter('8', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.D9:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, "9");
                            SetLetter('9', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Space:
                            _defaultInput = _defaultInput.Insert(_cursorPosition, " ");
                            SetLetter(' ', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Enter:
                            RunCommand(_defaultInput);
                            NewLine();
                            break;
                        case Keys.Control:
                            break;
                        case Keys.OemPeriod:
                            _defaultInput += _defaultInput.Insert(_cursorPosition, ">");
                            SetLetter('>', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Oemcomma:
                            _defaultInput += _defaultInput.Insert(_cursorPosition, "<");
                            SetLetter('<', Color.White, Color.Black);
                            _cursorPosition++;
                            break;
                        case Keys.Back:
                            break;
                        case Keys.Up:
                            break;
                        case Keys.Down:
                            break;
                        case Keys.Left:
                            _cursorFlash.Stop();
                            if (_cursorPosition != 0)
                                _cursorPosition--;
                            break;
                        case Keys.Right:
                            _cursorFlash.Stop();
                            if (_cursorPosition != _lineLetterCount)
                                _cursorPosition++;
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
            backbmpdat = _backbmp!.LockBits(new Rectangle(0, 0, _backbmp.Width, _backbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            if (_lineLetterCount == _cursorPosition || _defaultInput == "")
            {
                if (!_isFlashed)
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + ((LetterHeight) * _outputLineCount), Color.White);
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
                            BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + ((LetterHeight) * _outputLineCount), Color.Black);
                        }
                    }
                    _isFlashed = false;
                }
            }
            else
            {
                var ltr = _defaultInput[_cursorPosition].ToString();
                ReplaceLetters(ref ltr);
                //文字ファイルを読み込み
                Bitmap letterbmp = _fonts![ltr];
                //Bitmapを直接操作
                BitmapData fontdata = letterbmp.LockBits(new Rectangle(0, 0, letterbmp.Width, letterbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                if (!_isFlashed)
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            if (BitmapDataEx.GetPixel(letterbmp, fontdata, x, y) == Color.FromArgb(255, 255, 255, 255))
                            {
                                BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.Black);
                            }
                            else
                            {
                                BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.White);
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
                            if (BitmapDataEx.GetPixel(letterbmp, fontdata, x, y) == Color.FromArgb(255, 255, 255, 255))
                            {
                                BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.White);
                            }
                            else
                            {
                                BitmapDataEx.SetPixel(_backbmp, backbmpdat, x + (LetterWidth * _cursorPosition), y + (LetterHeight * _outputLineCount), Color.Black);
                            }
                        }
                    }
                    _isFlashed = false;
                }
                if (fontdata != null)
                    letterbmp.UnlockBits(fontdata);
            }
            if (backbmpdat != null)
                _backbmp.UnlockBits(backbmpdat);
            back.Image = _backbmp;
        }
        #endregion
        #endregion
    }
}