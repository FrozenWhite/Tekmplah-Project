using System.Windows.Forms;

namespace Teknomli
{
    partial class Main
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.back = new System.Windows.Forms.PictureBox();
            this.render1 = new System.Windows.Forms.PictureBox();
            this.render2 = new System.Windows.Forms.PictureBox();
            this.render3 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.back)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.render1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.render2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.render3)).BeginInit();
            this.SuspendLayout();
            // 
            // back
            // 
            this.back.BackColor = System.Drawing.Color.Black;
            this.back.Location = new System.Drawing.Point(0, 0);
            this.back.Margin = new System.Windows.Forms.Padding(4);
            this.back.Name = "back";
            this.back.Size = new System.Drawing.Size(640, 400);
            this.back.TabIndex = 0;
            this.back.TabStop = false;
            // 
            // render1
            // 
            this.render1.BackColor = System.Drawing.Color.Transparent;
            this.render1.Location = new System.Drawing.Point(0, 0);
            this.render1.Margin = new System.Windows.Forms.Padding(4);
            this.render1.Name = "render1";
            this.render1.Size = new System.Drawing.Size(640, 400);
            this.render1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.render1.TabIndex = 3;
            this.render1.TabStop = false;
            // 
            // render2
            // 
            this.render2.BackColor = System.Drawing.Color.Transparent;
            this.render2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.render2.Location = new System.Drawing.Point(0, 0);
            this.render2.Margin = new System.Windows.Forms.Padding(4);
            this.render2.Name = "render2";
            this.render2.Size = new System.Drawing.Size(640, 400);
            this.render2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.render2.TabIndex = 4;
            this.render2.TabStop = false;
            // 
            // render3
            // 
            this.render3.BackColor = System.Drawing.Color.Transparent;
            this.render3.Location = new System.Drawing.Point(0, 0);
            this.render3.Margin = new System.Windows.Forms.Padding(4);
            this.render3.Name = "render3";
            this.render3.Size = new System.Drawing.Size(640, 400);
            this.render3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.render3.TabIndex = 5;
            this.render3.TabStop = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 400);
            this.Controls.Add(this.render1);
            this.Controls.Add(this.render3);
            this.Controls.Add(this.render2);
            this.Controls.Add(this.back);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(656, 439);
            this.MinimumSize = new System.Drawing.Size(656, 439);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Teknomli";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.Main_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Main_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Main_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Main_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.back)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.render1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.render2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.render3)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox back;
        private System.Windows.Forms.PictureBox render1;
        private System.Windows.Forms.PictureBox render2;
        private System.Windows.Forms.PictureBox render3;
    }
}

