﻿namespace Gurpenator
{
    partial class MainWindow
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
            System.Windows.Forms.Label placeHolder1;
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.attributesGroup = new System.Windows.Forms.GroupBox();
            placeHolder1 = new System.Windows.Forms.Label();
            this.attributesGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // placeHolder1
            // 
            placeHolder1.AutoSize = true;
            placeHolder1.Location = new System.Drawing.Point(6, 16);
            placeHolder1.Name = "placeHolder1";
            placeHolder1.Size = new System.Drawing.Size(95, 13);
            placeHolder1.TabIndex = 1;
            placeHolder1.Text = "[place holder]--------";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(67, 12);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "Slartybartfast";
            // 
            // attributesGroup
            // 
            this.attributesGroup.AutoSize = true;
            this.attributesGroup.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.attributesGroup.Controls.Add(placeHolder1);
            this.attributesGroup.Location = new System.Drawing.Point(12, 38);
            this.attributesGroup.Name = "attributesGroup";
            this.attributesGroup.Size = new System.Drawing.Size(107, 45);
            this.attributesGroup.TabIndex = 12;
            this.attributesGroup.TabStop = false;
            this.attributesGroup.Text = "Attributes";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(555, 426);
            this.Controls.Add(this.attributesGroup);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Name = "MainWindow";
            this.Text = "Gurpenator";
            this.attributesGroup.ResumeLayout(false);
            this.attributesGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.GroupBox attributesGroup;
    }
}