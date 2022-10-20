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
            this.Enabled = false;
            this.WindowState = FormWindowState.Minimized;
            try
            {
                main.ShowDialog();
            }
            catch
            {
                main.Reset();
            }
            finally
            {
                this.Enabled = true;
            }

            this.WindowState = FormWindowState.Normal;
        }

        private void Config_Load(object sender, EventArgs e)
        {
        }
    }
}
