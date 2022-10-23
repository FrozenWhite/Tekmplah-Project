namespace Teknomli
{
    public partial class Config : Form
    {
        public Config()
        {
            InitializeComponent();
        }

        private void startbtn_Click(object sender, EventArgs e)
        {
            Main main = new();
            try
            {
                this.Enabled = false;
                this.WindowState = FormWindowState.Minimized;
                main.ShowDialog();
            }
            finally
            {
                this.Enabled = true;
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void Config_Load(object sender, EventArgs e)
        {
        }
    }
}
