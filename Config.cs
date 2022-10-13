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
        }

        private void Config_Load(object sender, EventArgs e)
        {
        }
    }
}
