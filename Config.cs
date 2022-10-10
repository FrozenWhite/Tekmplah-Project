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
            Main main = new Main();
            this.Enabled = false;
            main.ShowDialog();
            this.Enabled = true;
        }

        private void Config_Load(object sender, EventArgs e)
        {

        }
    }
}
