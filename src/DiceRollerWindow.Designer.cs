namespace Gurpenator
{
    partial class DiceRollerWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.rollingTable = new System.Windows.Forms.TableLayoutPanel();
            this.consoleText = new System.Windows.Forms.TextBox();
            this.addButton = new System.Windows.Forms.Button();
            this.dummy4 = new System.Windows.Forms.Button();
            this.dummy3 = new System.Windows.Forms.NumericUpDown();
            this.dummy2 = new System.Windows.Forms.Label();
            this.dummy1 = new System.Windows.Forms.NumericUpDown();
            this.tableLayoutPanel1.SuspendLayout();
            this.rollingTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dummy3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dummy1)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.rollingTable, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.consoleText, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(362, 262);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // rollingTable
            // 
            this.rollingTable.AutoSize = true;
            this.rollingTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.rollingTable.ColumnCount = 4;
            this.rollingTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.rollingTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.rollingTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.rollingTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.rollingTable.Controls.Add(this.dummy2, 1, 0);
            this.rollingTable.Controls.Add(this.addButton, 0, 0);
            this.rollingTable.Controls.Add(this.dummy1, 0, 1);
            this.rollingTable.Controls.Add(this.dummy3, 2, 1);
            this.rollingTable.Controls.Add(this.dummy4, 3, 1);
            this.rollingTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rollingTable.Location = new System.Drawing.Point(3, 3);
            this.rollingTable.Name = "rollingTable";
            this.rollingTable.RowCount = 2;
            this.rollingTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rollingTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rollingTable.Size = new System.Drawing.Size(154, 256);
            this.rollingTable.TabIndex = 0;
            // 
            // consoleText
            // 
            this.consoleText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.consoleText.Location = new System.Drawing.Point(163, 3);
            this.consoleText.Multiline = true;
            this.consoleText.Name = "consoleText";
            this.consoleText.Size = new System.Drawing.Size(196, 256);
            this.consoleText.TabIndex = 1;
            // 
            // addButton
            // 
            this.addButton.AutoSize = true;
            this.addButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.addButton.Location = new System.Drawing.Point(3, 3);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(35, 23);
            this.addButton.TabIndex = 4;
            this.addButton.Text = "add";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // dummy4
            // 
            this.dummy4.AutoSize = true;
            this.dummy4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.dummy4.Location = new System.Drawing.Point(116, 32);
            this.dummy4.Name = "dummy4";
            this.dummy4.Size = new System.Drawing.Size(35, 23);
            this.dummy4.TabIndex = 3;
            this.dummy4.Text = "Roll";
            this.dummy4.UseVisualStyleBackColor = true;
            // 
            // dummy3
            // 
            this.dummy3.AutoSize = true;
            this.dummy3.Location = new System.Drawing.Point(69, 32);
            this.dummy3.Name = "dummy3";
            this.dummy3.Size = new System.Drawing.Size(41, 20);
            this.dummy3.TabIndex = 2;
            // 
            // dummy2
            // 
            this.dummy2.AutoSize = true;
            this.dummy2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dummy2.Location = new System.Drawing.Point(50, 0);
            this.dummy2.Name = "dummy2";
            this.dummy2.Size = new System.Drawing.Size(13, 29);
            this.dummy2.TabIndex = 1;
            this.dummy2.Text = "d";
            this.dummy2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dummy1
            // 
            this.dummy1.AutoSize = true;
            this.dummy1.Location = new System.Drawing.Point(3, 32);
            this.dummy1.Name = "dummy1";
            this.dummy1.Size = new System.Drawing.Size(41, 20);
            this.dummy1.TabIndex = 0;
            // 
            // DiceRollerWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(362, 262);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "DiceRollerWindow";
            this.Text = "DiceRollerWindow";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.rollingTable.ResumeLayout(false);
            this.rollingTable.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dummy3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dummy1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel rollingTable;
        private System.Windows.Forms.TextBox consoleText;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.NumericUpDown dummy1;
        private System.Windows.Forms.Label dummy2;
        private System.Windows.Forms.NumericUpDown dummy3;
        private System.Windows.Forms.Button dummy4;
    }
}