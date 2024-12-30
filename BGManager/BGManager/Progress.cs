using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BGManager;

public class Progress : Form
{
	private int _max = 0;

	private IContainer components = null;

	private ProgressBar progressBar;

	public Progress()
	{
		InitializeComponent();
	}

	public void SetJobTitle(string title)
	{
		Text = title;
	}

	public void UpdateProgress(int i, int max)
	{
		if (_max != max)
		{
			_max = max;
			progressBar.Value = 0;
			progressBar.Maximum = max;
		}
		progressBar.Value = i;
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
		this.progressBar = new System.Windows.Forms.ProgressBar();
		base.SuspendLayout();
		this.progressBar.Location = new System.Drawing.Point(12, 12);
		this.progressBar.Name = "progressBar";
		this.progressBar.Size = new System.Drawing.Size(383, 30);
		this.progressBar.TabIndex = 0;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(407, 52);
		base.ControlBox = false;
		base.Controls.Add(this.progressBar);
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "Progress";
		this.Text = "Progress";
		base.TopMost = true;
		base.ResumeLayout(false);
	}
}
