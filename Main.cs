// (c) Team FrozenWhite
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.Diagnostics;
using System.Text;
using System.Drawing.Imaging;

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
        private Bitmap backbmp;

        /// <summary>
        /// Render1用のBitmap
        /// </summary>
        private Bitmap render1bmp;

        /// <summary>
        /// Render2用のBitmap
        /// </summary>
        private Bitmap render2bmp;

        /// <summary>
        /// Render3用のBitmap
        /// </summary>
        private Bitmap render3bmp;

        /// <summary>
        /// 一行にある文字数
        /// </summary>
        private int line_letter_count = 0;

        /// <summary>
        /// 出力された行数
        /// </summary>
        private int output_line_count = 0;

        /// <summary>
        /// カーソルがある場所
        /// </summary>
        private int cursor_position = 0;

        /// <summary>
        /// 入力された文字列
        /// </summary>
        private string default_input = "";

        /// <summary>
        /// 出力された文字列
        /// </summary>
        private string default_output = "";

        /// <summary>
        /// カーソルが点滅中か
        /// </summary>
        private bool _isFlashed = false;

        /// <summary>
        /// カーソルを点滅させる用
        /// </summary>
        private System.Timers.Timer cursorFlash;

        /// <summary>
        /// コンソール画面かどうか
        /// </summary>
        private bool isConsole = false;

        /// <summary>
        /// フォントセット
        /// </summary>
        private Dictionary<string, Bitmap> fonts = new Dictionary<string, Bitmap> { };

        private MethodInfo audio;

        private List<string> _outputedLetters = new List<string>();
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
        public static MethodInfo CreateMethodInfo(PInvoke invInfo)
        {
            string moduleName = Path.GetFileNameWithoutExtension(invInfo.ModuleFile).ToUpper();
            AssemblyBuilder asmBld = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("Asm" + moduleName), AssemblyBuilderAccess.Run);

            ModuleBuilder modBld = asmBld.DefineDynamicModule(
                "Mod" + moduleName);

            TypeBuilder typBld = modBld.DefineType(
                "Class" + moduleName,
                TypeAttributes.Public | TypeAttributes.Class);

            MethodBuilder methodBuilder = typBld.DefinePInvokeMethod(
                invInfo.ProcName,
                invInfo.ModuleFile,
                invInfo.EntryPoint,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl | MethodAttributes.HideBySig,
                CallingConventions.Standard,
                invInfo.ReturnType,
                invInfo.ParameterTypes.ToArray(),
                invInfo.CallingConvention,
                invInfo.CharSet);
            methodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig);

            return typBld.CreateType().GetMethod(invInfo.ProcName);
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

        #endregion
        #region ブートローダー
        
        /// <summary>
        /// 起動
        /// </summary>
        private async void Start()
        {
            timeBeginPeriod(1);
            await Task.Delay(500);
            LoadDefaultFonts();
            //Bitmapを作成
            backbmp = new Bitmap(back.Width, back.Height);
            render1bmp = new Bitmap(render1.Width, render1.Height);
            render2bmp = new Bitmap(render2.Width, render2.Height);
            render3bmp = new Bitmap(render3.Width, render3.Height);
            render1bmp.MakeTransparent(Color.Black);
            render2bmp.MakeTransparent(Color.Black);
            render3bmp.MakeTransparent(Color.Black);
            //セット
            back.Image = backbmp;
            render1.Image = render1bmp;
            render2.Image = render2bmp;
            render3.Image = render3bmp;
            _outputedLetters.Add("");
            if (File.Exists("FuramAudioBoard.dll"))
            {
                var invInfo = new PInvoke()
                {
                    ProcName = "Play",
                    EntryPoint = "Play",
                    ModuleFile = "FuramAudioBoard.dll",
                    ReturnType = typeof(int),
                    ParameterTypes = new Type[] { typeof(float), typeof(float), typeof(float), typeof(int) },
                    CallingConvention = CallingConvention.Cdecl,
                    CharSet = CharSet.Unicode
                };
                //Invokeで実行
                audio = CreateMethodInfo(invInfo);
                //audio.Invoke(null, new object[] { 8f, 2000, 10f ,0  });
            }
            Console.Beep(2000, 100);
            await MemoryCheck();
            Echo("memory check OK"); NewLine();
            await CheckHeader();
            Init();
        }

        private async Task MemoryCheck()
        {
            mem = Marshal.AllocHGlobal(640001);
            for (int i = 0; i < 640001; i++)
            {
                Marshal.ReadByte(mem, i);
                Marshal.WriteByte(mem, i, Convert.ToByte(255));
                //
                if (i % 20000 != 0) continue;
                Echo($"MEMORY {i / 1000}KB OK");
                line_letter_count = 0;
                default_output = "";
                await Task.Run(() => Thread.Sleep(1));
            }
            NewLine();
        }

        /// <summary>
        /// システムデフォルトのフォントを読み込む
        /// </summary>
        private void LoadDefaultFonts()
        {
            //Number
            fonts.Add("0", Properties.fonts._0);
            fonts.Add("1", Properties.fonts._1);
            fonts.Add("2", Properties.fonts._2);
            fonts.Add("3", Properties.fonts._3);
            fonts.Add("4", Properties.fonts._4);
            fonts.Add("5", Properties.fonts._5);
            fonts.Add("6", Properties.fonts._6);
            fonts.Add("7", Properties.fonts._7);
            fonts.Add("8", Properties.fonts._8);
            fonts.Add("9", Properties.fonts._9);

            //Large Alphabet
            fonts.Add("A", Properties.fonts.A);
            fonts.Add("B", Properties.fonts.B);
            fonts.Add("C", Properties.fonts.C);
            fonts.Add("D", Properties.fonts.D);
            fonts.Add("E", Properties.fonts.E);
            fonts.Add("F", Properties.fonts.F);
            fonts.Add("G", Properties.fonts.G);
            fonts.Add("H", Properties.fonts.H);
            fonts.Add("I", Properties.fonts.I);
            fonts.Add("J", Properties.fonts.J);
            fonts.Add("K", Properties.fonts.K);
            fonts.Add("L", Properties.fonts.L);
            fonts.Add("M", Properties.fonts.M);
            fonts.Add("N", Properties.fonts.N);
            fonts.Add("O", Properties.fonts.O);
            fonts.Add("P", Properties.fonts.P);
            fonts.Add("Q", Properties.fonts.Q);
            fonts.Add("R", Properties.fonts.R);
            fonts.Add("S", Properties.fonts.S);
            fonts.Add("T", Properties.fonts.T);
            fonts.Add("U", Properties.fonts.U);
            fonts.Add("V", Properties.fonts.V);
            fonts.Add("W", Properties.fonts.W);
            fonts.Add("X", Properties.fonts.X);
            fonts.Add("Y", Properties.fonts.Y);
            fonts.Add("Z", Properties.fonts.Z);

            //Small Alphabet
            fonts.Add("a", Properties.fonts.a_s);
            fonts.Add("b", Properties.fonts.b_s);
            fonts.Add("c", Properties.fonts.c_s);
            fonts.Add("d", Properties.fonts.d_s);
            fonts.Add("e", Properties.fonts.e_s);
            fonts.Add("f", Properties.fonts.f_s);
            fonts.Add("g", Properties.fonts.g_s);
            fonts.Add("h", Properties.fonts.h_s);
            fonts.Add("i", Properties.fonts.i_s);
            fonts.Add("j", Properties.fonts.j_s);
            fonts.Add("k", Properties.fonts.k_s);
            fonts.Add("l", Properties.fonts.l_s);
            fonts.Add("m", Properties.fonts.m_s);
            fonts.Add("n", Properties.fonts.n_s);
            fonts.Add("o", Properties.fonts.o_s);
            fonts.Add("p", Properties.fonts.p_s);
            fonts.Add("q", Properties.fonts.q_s);
            fonts.Add("r", Properties.fonts.r_s);
            fonts.Add("s", Properties.fonts.s_s);
            fonts.Add("t", Properties.fonts.t_s);
            fonts.Add("u", Properties.fonts.u_s);
            fonts.Add("v", Properties.fonts.v_s);
            fonts.Add("w", Properties.fonts.w_s);
            fonts.Add("x", Properties.fonts.x_s);
            fonts.Add("y", Properties.fonts.y_s);
            fonts.Add("z", Properties.fonts.z_s);

            //Mark
            //!
            fonts.Add("exc", Properties.fonts.exc);
            //"
            fonts.Add("quo", Properties.fonts.quo);
            //#
            fonts.Add("has", Properties.fonts.has);
            //$
            fonts.Add("dol", Properties.fonts.dol);
            //%
            fonts.Add("per", Properties.fonts.per);
            //&
            
            fonts.Add("and", Properties.fonts.and);
            //'
            fonts.Add("sin", Properties.fonts.sin);
            //(
            fonts.Add("brs", Properties.fonts.brs);
            //)
            fonts.Add("bre", Properties.fonts.bre);
            //<
            fonts.Add("gre", Properties.fonts.gre);
            //>
            fonts.Add("les", Properties.fonts.les);
            //?
            fonts.Add("que", Properties.fonts.que);
            // (space)
            fonts.Add("spa", Properties.fonts.spa);
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
                        await Task.Delay(10);
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
            await Task.Delay(1000);
            _outputedLetters = new List<string>();
            _outputedLetters.Add("");
            output_line_count = 0;
            line_letter_count = 0;
            cursor_position = 0;
            default_input = "";
            default_output = "";
            //Bitmapを作成
            backbmp = new Bitmap(back.Width, back.Height);
            //セット
            back.Image = backbmp;
        }

        /// <summary>
        /// 起動
        /// </summary>
        private void Init()
        {
            
            output_line_count = 0;
            cursor_position = 0;
            default_output = "";
#if DEBUG
            Echo("Horai Debug OS v001"); NewLine();
#else
            Echo("Horai OS v001"); NewLine();
#endif
            isConsole = true;
            //カーソルの点滅
            cursorFlash = new System.Timers.Timer(500);
            cursorFlash.Elapsed += CursorFlash_Elapsed;
            cursorFlash.Start();
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
                        await Task.Delay(1);
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
                    cursorFlash.Stop();
                    isConsole = false;
                    line_letter_count = 0;
                    output_line_count = -1;
                    cursor_position = 0;
                    default_input = "";
                    default_output = "";
                    //Bitmapを作成
                    backbmp = new Bitmap(back.Width, back.Height);
                    render1bmp = new Bitmap(render1.Width, render1.Height);
                    render2bmp = new Bitmap(render2.Width, render2.Height);
                    render3bmp = new Bitmap(render3.Width, render3.Height);
                    //セット
                    back.Image = backbmp;
                    render1.Image = render1bmp;
                    render2.Image = render2bmp;
                    render3.Image = render3bmp;
                    isConsole = true;
                    cursorFlash.Start();
                    break;
                default:
                    NewLine();
                    Echo("'"+ cmd[0] + "' is not recognized as an internal or external command");
                    break;
            }
        }

        private void RunApplication(string name)
        {
            cursorFlash.Stop();
        }

        private void reset()
        {

        }

        private void SetPixel(Point point,Color color,int layer)
        {
            cursorFlash.Stop();
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
            cursorFlash.Stop();
            Bitmap cirblue = new Bitmap(@"D:\Downloads\circleblue.bmp");
            Bitmap cirred = new Bitmap(@"D:\Downloads\circlered.bmp");
            BitmapData render1bmpdat = render1bmp.LockBits(new Rectangle(0, 0, render1bmp.Width, render1bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData backbmpdat = backbmp.LockBits(new Rectangle(0, 0, backbmp.Width, backbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

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
                                        BitmapDataEx.SetPixel(render1bmp, render1bmpdat, (int)xl, (int)yl, Color.FromArgb(0));
                                    else
                                        BitmapDataEx.SetPixel(backbmp, backbmpdat, (int)xl, (int)yl, Color.FromArgb(0));
                                }
                                else
                                {
                                    if (circleblu != Color.FromArgb(255, 0, 0, 0))
                                    {
                                        if (isred)
                                            BitmapDataEx.SetPixel(render1bmp, render1bmpdat, (int)xl, (int)yl, circlered);
                                        else
                                            BitmapDataEx.SetPixel(backbmp, backbmpdat, (int)xl, (int)yl, circleblu);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            cirblue.UnlockBits(bluecircleData);
            backbmp.UnlockBits(backbmpdat);
            render1bmp.UnlockBits(render1bmpdat);
            back.Image = backbmp;
            render1bmp.MakeTransparent(Color.Black);
            render2bmp.MakeTransparent(Color.Black);
            render3bmp.MakeTransparent(Color.Black);
            render1.Image = render1bmp;
            cursorFlash.Start();
        }

        #endregion
        #region 描画
        private static Bitmap RotateImage(Bitmap bmp, float angle)
        {
            float alpha = angle;

            //edit: negative angle +360
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
                cursor_position++;
            }
        }

        /// <summary>
        /// 改行
        /// </summary>
        private void NewLine()
        {
            output_line_count++;
            line_letter_count = 0;
            cursor_position = 0;
            default_input = "";
            default_output = "";
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
            cursorFlash?.Stop();
            //charをstringに変換
            string ltr = letter.ToString();
            _outputedLetters.Add("");
            _outputedLetters[output_line_count] = _outputedLetters[output_line_count].Insert(0,ltr);
            switch (ltr)
            {
                //記号とかを置き換え
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

            int llcbak = line_letter_count;
            int cpbak = cursor_position;
            if (default_input.Length * LetterWidth > backbmp.Width - (LetterWidth / 2))
            {
                output_line_count++;
                line_letter_count = 0;
                cursor_position = 0;
            }

            if (!fonts.ContainsKey(ltr))
                return;
            BitmapData backbmpdat = backbmp.LockBits(new Rectangle(0, 0, backbmp.Width, backbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            switch (ltr)
            {
                //空欄だったらそのまま
                case " ":
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * line_letter_count) + 2, y + (LetterHeight * output_line_count), backColor);
                        }
                    }
                    line_letter_count++;
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
                    Bitmap letterbmp = fonts[ltr];
                    //Bitmapを直接操作
                    BitmapData fontdata = letterbmp.LockBits(new Rectangle(0, 0, letterbmp.Width, letterbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * line_letter_count),
                                y + (LetterHeight * output_line_count),
                                BitmapDataEx.GetPixel(letterbmp, fontdata, x, y) != Color.FromArgb(255, 0, 0, 0)
                                    ? LetterColor
                                    : backColor);
                        }
                    }
                    line_letter_count++;
                    //解放

                    if (fontdata != null)
                        letterbmp.UnlockBits(fontdata);
                    break;
                }
            }

            
            //標準出力に書き込み
            default_output += letter.ToString();
            if (default_input.Length * LetterWidth > backbmp.Width - (LetterWidth / 2))
            {
                output_line_count--;
                line_letter_count = llcbak;
                cursor_position = cpbak;
            }

            //解放
            if (backbmpdat != null)
                backbmp.UnlockBits(backbmpdat);
            //描画
            back.Image = backbmp;
            cursorFlash?.Start();
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
            if (isConsole)
            {
                //点滅の一時停止
                cursorFlash.Stop();
                await Task.Delay(1);
                BitmapData backbmpdat = null;
                try
                {
                    backbmpdat = backbmp.LockBits(new Rectangle(0, 0, backbmp.Width, backbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    //カーソルの削除
                    if (line_letter_count == cursor_position || default_input == "")
                    {
                        for (int x = 0; x < LetterWidth; x++)
                        {
                            for (int y = 0; y < LetterHeight; y++)
                            {
                                BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + (LetterHeight * output_line_count), Color.FromArgb(0));
                            }
                        }
                    }
                    else
                    {
                        string ltr = default_input[cursor_position].ToString();
                        //記号とかを置き換え
                        if (ltr == "?")
                            ltr = "que";
                        else if (ltr == "!")
                            ltr = "exc";
                        else if (ltr == "\"")
                            ltr = "quo";
                        else if (ltr == "#")
                            ltr = "has";
                        else if (ltr == " ")
                            ltr = "spa";
                        else if (ltr == ">")
                            ltr = "gre";
                        else if (ltr == "<")
                            ltr = "les";
                        Bitmap letterbmp = fonts[ltr];
                        //Bitmapを直接操作
                        BitmapData fontdata = letterbmp.LockBits(new Rectangle(0, 0, letterbmp.Width, letterbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                        for (int x = 0; x < LetterWidth; x++)
                        {
                            for (int y = 0; y < LetterHeight; y++)
                            {
                                if (BitmapDataEx.GetPixel(letterbmp, fontdata, x, y) == Color.FromArgb(255, 255, 255, 255))
                                {
                                    BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + (LetterHeight * output_line_count), Color.White);
                                }
                                else
                                {
                                    BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + (LetterHeight * output_line_count), Color.Black);
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
                        backbmp.UnlockBits(backbmpdat);
                        back.Image = backbmp;
                    }
                }
                if (e.Shift)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.A:
                            default_input += "A";
                            SetLetter('A', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.B:
                            default_input += "B";
                            SetLetter('B', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.C:
                            default_input += "C";
                            SetLetter('C', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D:
                            default_input += "D";
                            SetLetter('D', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.E:
                            default_input += "E";
                            SetLetter('E', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.F:
                            default_input += "F";
                            SetLetter('F', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.G:
                            default_input += "G";
                            SetLetter('G', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.H:
                            default_input += "H";
                            SetLetter('H', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.I:
                            default_input += "I";
                            SetLetter('I', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.J:
                            default_input += "J";
                            SetLetter('J', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.K:
                            default_input += "K";
                            SetLetter('K', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.L:
                            default_input += "L";
                            SetLetter('L', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.M:
                            default_input += "M";
                            SetLetter('M', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.N:
                            default_input += "N";
                            SetLetter('N', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.O:
                            default_input += "O";
                            SetLetter('O', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.P:
                            default_input += "P";
                            SetLetter('P', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Q:
                            default_input += "Q";
                            SetLetter('Q', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.R:
                            default_input += "R";
                            SetLetter('R', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.S:
                            default_input += "S";
                            SetLetter('S', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.T:
                            default_input += "T";
                            SetLetter('T', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.U:
                            default_input += "U";
                            SetLetter('U', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.V:
                            default_input += "V";
                            SetLetter('V', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.W:
                            default_input += "W";
                            SetLetter('W', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.X:
                            default_input += "X";
                            SetLetter('X', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Y:
                            default_input += "Y";
                            SetLetter('Y', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Z:
                            default_input += "Z";
                            SetLetter('Z', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D0:
                            break;
                        case Keys.D1:
                            default_input += "!";
                            SetLetter('!', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D2:
                            default_input += "\"";
                            SetLetter('"', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D3:
                            default_input += "#";
                            SetLetter('#', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D4:
                            default_input += "$";
                            SetLetter('$', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D5:
                            default_input += "%";
                            SetLetter('%', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D6:
                            default_input += "&";
                            SetLetter('&', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D7:
                            default_input += "'";
                            SetLetter('\'', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D8:
                            default_input = default_input.Insert(cursor_position, "(");
                            SetLetter('(', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D9:
                            default_input = default_input.Insert(cursor_position, ")");
                            SetLetter(')', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Space:
                            default_input += " ";
                            SetLetter(' ', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Enter:
                            RunCommand(default_input);
                            NewLine();
                            break;
                        case Keys.Control:
                            break;
                        case Keys.OemPeriod:
                            default_input = default_input.Insert(cursor_position, ">");
                            SetLetter('>', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Oemcomma:
                            default_input = default_input.Insert(cursor_position, "<");
                            SetLetter('<', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Back:
                            break;
                        case Keys.Up:
                            break;
                        case Keys.Down:
                            break;
                        case Keys.Left:
                            if (cursor_position != 0)
                                cursor_position--;
                            break;
                        case Keys.Right:
                            if (cursor_position != line_letter_count)
                                cursor_position++;
                            break;
                    }
                }
                else
                {
                    switch (e.KeyCode)
                    {
                        case Keys.A:
                            default_input = default_input.Insert(cursor_position, "a");
                            SetLetter('a', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.B:
                            default_input = default_input.Insert(cursor_position, "b");
                            SetLetter('b', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.C:
                            default_input = default_input.Insert(cursor_position, "c");
                            SetLetter('c', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D:
                            default_input = default_input.Insert(cursor_position, "d");
                            SetLetter('d', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.E:
                            default_input = default_input.Insert(cursor_position, "e");
                            SetLetter('e', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.F:
                            default_input = default_input.Insert(cursor_position, "f");
                            SetLetter('f', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.G:
                            default_input = default_input.Insert(cursor_position, "g");
                            SetLetter('g', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.H:
                            default_input = default_input.Insert(cursor_position, "h");
                            SetLetter('h', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.I:
                            default_input = default_input.Insert(cursor_position, "i");
                            SetLetter('i', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.J:
                            default_input = default_input.Insert(cursor_position, "j");
                            SetLetter('j', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.K:
                            default_input = default_input.Insert(cursor_position, "k");
                            SetLetter('k', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.L:
                            default_input = default_input.Insert(cursor_position, "l");
                            SetLetter('l', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.M:
                            default_input = default_input.Insert(cursor_position, "m");
                            SetLetter('m', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.N:
                            default_input = default_input.Insert(cursor_position, "n");
                            SetLetter('n', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.O:
                            default_input = default_input.Insert(cursor_position, "o");
                            SetLetter('o', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.P:
                            default_input = default_input.Insert(cursor_position, "p");
                            SetLetter('p', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Q:
                            default_input = default_input.Insert(cursor_position, "q");
                            SetLetter('q', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.R:
                            default_input = default_input.Insert(cursor_position, "r");
                            SetLetter('r', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.S:
                            default_input = default_input.Insert(cursor_position, "s");
                            SetLetter('s', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.T:
                            default_input = default_input.Insert(cursor_position, "t");
                            SetLetter('t', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.U:
                            default_input = default_input.Insert(cursor_position, "u");
                            SetLetter('u', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.V:
                            default_input = default_input.Insert(cursor_position, "v");
                            SetLetter('v', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.W:
                            default_input = default_input.Insert(cursor_position, "w");
                            SetLetter('w', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.X:
                            default_input = default_input.Insert(cursor_position, "x");
                            SetLetter('x', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Y:
                            default_input = default_input.Insert(cursor_position, "y");
                            SetLetter('y', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Z:
                            default_input = default_input.Insert(cursor_position, "z");
                            SetLetter('z', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D0:
                            default_input = default_input.Insert(cursor_position, "0");
                            SetLetter('0', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D1:
                            default_input = default_input.Insert(cursor_position, "1");
                            SetLetter('1', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D2:
                            default_input = default_input.Insert(cursor_position, "2");
                            SetLetter('2', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D3:
                            default_input = default_input.Insert(cursor_position, "3");
                            SetLetter('3', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D4:
                            default_input = default_input.Insert(cursor_position, "4");
                            SetLetter('4', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D5:
                            default_input = default_input.Insert(cursor_position, "5");
                            SetLetter('5', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D6:
                            default_input = default_input.Insert(cursor_position, "6");
                            SetLetter('6', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D7:
                            default_input = default_input.Insert(cursor_position, "7");
                            SetLetter('7', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D8:
                            default_input = default_input.Insert(cursor_position, "8");
                            SetLetter('8', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.D9:
                            default_input = default_input.Insert(cursor_position, "9");
                            SetLetter('9', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Space:
                            default_input = default_input.Insert(cursor_position, " ");
                            SetLetter(' ', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Enter:
                            RunCommand(default_input);
                            NewLine();
                            break;
                        case Keys.Control:
                            break;
                        case Keys.OemPeriod:
                            default_input += default_input.Insert(cursor_position, ">");
                            SetLetter('>', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Oemcomma:
                            default_input += default_input.Insert(cursor_position, "<");
                            SetLetter('<', Color.White, Color.Black);
                            cursor_position++;
                            break;
                        case Keys.Back:
                            break;
                        case Keys.Up:
                            break;
                        case Keys.Down:
                            break;
                        case Keys.Left:
                            cursorFlash.Stop();
                            if (cursor_position != 0)
                                cursor_position--;
                            break;
                        case Keys.Right:
                            cursorFlash.Stop();
                            if (cursor_position != line_letter_count)
                                cursor_position++;
                            break;
                    }
                }
                cursorFlash.Start();
            }

            await Task.Delay(1);
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
            backbmpdat = backbmp.LockBits(new Rectangle(0, 0, backbmp.Width, backbmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            if (line_letter_count == cursor_position || default_input == "")
            {
                if (!_isFlashed)
                {
                    for (int x = 0; x < LetterWidth; x++)
                    {
                        for (int y = 0; y < LetterHeight; y++)
                        {
                            BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + ((LetterHeight) * output_line_count), Color.White);
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
                            BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + ((LetterHeight) * output_line_count), Color.Black);
                        }
                    }
                    _isFlashed = false;
                }
            }
            else
            {
                var ltr = default_input[cursor_position].ToString();
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
                //文字ファイルを読み込み
                Bitmap letterbmp = fonts[ltr];
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
                                BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + (LetterHeight * output_line_count), Color.Black);
                            }
                            else
                            {
                                BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + (LetterHeight * output_line_count), Color.White);
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
                                BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + (LetterHeight * output_line_count), Color.White);
                            }
                            else
                            {
                                BitmapDataEx.SetPixel(backbmp, backbmpdat, x + (LetterWidth * cursor_position), y + (LetterHeight * output_line_count), Color.Black);
                            }
                        }
                    }
                    _isFlashed = false;
                }
                if (fontdata != null)
                    letterbmp.UnlockBits(fontdata);
            }
            if (backbmpdat != null)
                backbmp.UnlockBits(backbmpdat);
            back.Image = backbmp;
        }
        #endregion
        #endregion
    }
}