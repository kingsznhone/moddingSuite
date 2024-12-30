using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BGManager;

public class TextViewForm : Form
{
	private IContainer components = null;

	private TextBox textBox1;

	public TextViewForm()
	{
		InitializeComponent();
	}

	public void AddLine(string s, int additionalNewLines = 0)
	{
		while (additionalNewLines < 0)
		{
			textBox1.Text += Environment.NewLine;
			additionalNewLines++;
		}
		textBox1.Text += s;
		textBox1.Text += Environment.NewLine;
		while (additionalNewLines > 0)
		{
			textBox1.Text += Environment.NewLine;
			additionalNewLines--;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.textBox1 = new System.Windows.Forms.TextBox();
		base.SuspendLayout();
		this.textBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.textBox1.Location = new System.Drawing.Point(0, 1);
		this.textBox1.Multiline = true;
		this.textBox1.Name = "textBox1";
		this.textBox1.ReadOnly = true;
		this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
		this.textBox1.Size = new System.Drawing.Size(838, 662);
		this.textBox1.TabIndex = 0;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(837, 662);
		base.Controls.Add(this.textBox1);
		base.Name = "TextViewForm";
		this.Text = "TextViewForm";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
