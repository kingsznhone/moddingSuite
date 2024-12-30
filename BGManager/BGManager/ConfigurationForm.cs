using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BGManager;

public class ConfigurationForm : Form
{
    private IContainer components = null;

    private Label label1;

    private TextBox NDF_DatFileTextBox;

    private Button button1;

    private Label label2;

    private TextBox DictionaryFileTextBox;

    private Button button2;

    private Label label3;

    private TextBox UnitDicTextBox;

    private TextBox BGDicTextBox;

    private Label label4;

    private TextBox InterfaceDicTextBox;

    private Label label5;

    private Button _validateButton;

    private Button button4;

    private CheckBox keepLogFilesCheckBox;

    private CheckBox fullSummaryCheckBox;
    private TextBox DictionaryFileTextBox_BG;
    private Label label6;
    private Button button3;
    private TextBox DictionaryFileTextBox_I;
    private Label label7;
    private Button button5;
    private CheckBox _exportAllPawnsCheckBox;

    public new bool Validated
    {
        get
        {
            return !_validateButton.Enabled;
        }
        set
        {
            _validateButton.Enabled = !value;
        }
    }

    public BGSettings Settings { get; set; }

    public ConfigurationForm(BGSettings settings)
    {
        InitializeComponent();
        Validated = false;
        Settings = settings;
        UnitDicTextBox.Text = Settings.UnitHashPattern;
        BGDicTextBox.Text = Settings.BGHashPattern;
        InterfaceDicTextBox.Text = Settings.InterfaceHashPattern;
        NDF_DatFileTextBox.Text = Settings.DataFilePath;
        DictionaryFileTextBox.Text = Settings.DictionaryFilePath;
        DictionaryFileTextBox_BG.Text = Settings.DictionaryFile_BG_Path;
        DictionaryFileTextBox_I.Text = Settings.DictionaryFile_I_Path;
        keepLogFilesCheckBox.Checked = Settings.KeepLogFiles;
        fullSummaryCheckBox.Checked = Settings.FullSummary;
        _exportAllPawnsCheckBox.Checked = Settings.ExportAllPawns;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        OpenFileDialog dlg = new OpenFileDialog();
        DialogResult dr = dlg.ShowDialog();
        if (dr == DialogResult.OK)
        {
            NDF_DatFileTextBox.Text = dlg.FileName;
        }
    }

    private void button2_Click(object sender, EventArgs e)
    {
        OpenFileDialog dlg = new OpenFileDialog();
        DialogResult dr = dlg.ShowDialog();
        if (dr == DialogResult.OK)
        {
            DictionaryFileTextBox.Text = dlg.FileName;
        }
    }

    private void button3_Click(object sender, EventArgs e)
    {
        string error = "";
        GetSettings();
        if (DataBase.Validate(Settings, out error))
        {
            MessageBox.Show("Everything seems fine");
            Validated = true;
        }
        else
        {
            MessageBox.Show(error, "Validation failed");
            Validated = false;
        }
    }

    private void button4_Click(object sender, EventArgs e)
    {
        GetSettings();
        if (!Validated)
        {
            string error = "";
            if (!DataBase.Validate(Settings, out error))
            {
                MessageBox.Show(error, "Validation failed - try again");
                return;
            }
            base.DialogResult = DialogResult.OK;
        }
        else
        {
            base.DialogResult = DialogResult.OK;
        }
        Close();
    }

    private void GetSettings()
    {
        Settings.DataFilePath = NDF_DatFileTextBox.Text;
        Settings.DictionaryFilePath = DictionaryFileTextBox.Text;
        Settings.DictionaryFile_BG_Path = DictionaryFileTextBox_BG.Text;
        Settings.DictionaryFile_I_Path = DictionaryFileTextBox_I.Text;
        Settings.UnitHashPattern = UnitDicTextBox.Text;
        Settings.BGHashPattern = BGDicTextBox.Text;
        Settings.InterfaceHashPattern = InterfaceDicTextBox.Text;
        Settings.KeepLogFiles = keepLogFilesCheckBox.Checked;
        Settings.FullSummary = fullSummaryCheckBox.Checked;
        Settings.ExportAllPawns = _exportAllPawnsCheckBox.Checked;
    }

    private void UnitDicTextBox_TextChanged(object sender, EventArgs e)
    {
        Validated = false;
    }

    private void BGDicTextBox_TextChanged(object sender, EventArgs e)
    {
        Validated = false;
    }

    private void InterfaceDicTextBox_TextChanged(object sender, EventArgs e)
    {
        Validated = false;
    }

    private void DictionaryFileTextBox_TextChanged(object sender, EventArgs e)
    {
        Validated = false;
    }

    private void NDF_DatFileTextBox_TextChanged(object sender, EventArgs e)
    {
        Validated = false;
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
        label1 = new Label();
        NDF_DatFileTextBox = new TextBox();
        button1 = new Button();
        label2 = new Label();
        DictionaryFileTextBox = new TextBox();
        button2 = new Button();
        label3 = new Label();
        UnitDicTextBox = new TextBox();
        BGDicTextBox = new TextBox();
        label4 = new Label();
        InterfaceDicTextBox = new TextBox();
        label5 = new Label();
        _validateButton = new Button();
        button4 = new Button();
        keepLogFilesCheckBox = new CheckBox();
        fullSummaryCheckBox = new CheckBox();
        _exportAllPawnsCheckBox = new CheckBox();
        DictionaryFileTextBox_BG = new TextBox();
        label6 = new Label();
        button3 = new Button();
        DictionaryFileTextBox_I = new TextBox();
        label7 = new Label();
        button5 = new Button();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(20, 11);
        label1.Margin = new Padding(6, 0, 6, 0);
        label1.Name = "label1";
        label1.Size = new Size(200, 24);
        label1.TabIndex = 0;
        label1.Text = "NDF_Win.dat file path";
        // 
        // NDF_DatFileTextBox
        // 
        NDF_DatFileTextBox.Location = new Point(16, 42);
        NDF_DatFileTextBox.Margin = new Padding(6);
        NDF_DatFileTextBox.Name = "NDF_DatFileTextBox";
        NDF_DatFileTextBox.Size = new Size(1113, 30);
        NDF_DatFileTextBox.TabIndex = 1;
        NDF_DatFileTextBox.TextChanged += NDF_DatFileTextBox_TextChanged;
        // 
        // button1
        // 
        button1.Location = new Point(1144, 37);
        button1.Margin = new Padding(6);
        button1.Name = "button1";
        button1.Size = new Size(62, 42);
        button1.TabIndex = 2;
        button1.Text = "...";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(26, 98);
        label2.Margin = new Padding(6, 0, 6, 0);
        label2.Name = "label2";
        label2.Size = new Size(284, 24);
        label2.TabIndex = 3;
        label2.Text = "ZZ_Win.dat file path (Unit Hash)";
        // 
        // DictionaryFileTextBox
        // 
        DictionaryFileTextBox.Location = new Point(22, 137);
        DictionaryFileTextBox.Margin = new Padding(6);
        DictionaryFileTextBox.Name = "DictionaryFileTextBox";
        DictionaryFileTextBox.Size = new Size(1108, 30);
        DictionaryFileTextBox.TabIndex = 4;
        DictionaryFileTextBox.TextChanged += DictionaryFileTextBox_TextChanged;
        // 
        // button2
        // 
        button2.Location = new Point(1144, 131);
        button2.Margin = new Padding(6);
        button2.Name = "button2";
        button2.Size = new Size(59, 42);
        button2.TabIndex = 5;
        button2.Text = "...";
        button2.UseVisualStyleBackColor = true;
        button2.Click += button2_Click;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(25, 396);
        label3.Margin = new Padding(6, 0, 6, 0);
        label3.Name = "label3";
        label3.Size = new Size(92, 24);
        label3.TabIndex = 6;
        label3.Text = "unites.dic";
        // 
        // UnitDicTextBox
        // 
        UnitDicTextBox.Location = new Point(263, 390);
        UnitDicTextBox.Margin = new Padding(6);
        UnitDicTextBox.Name = "UnitDicTextBox";
        UnitDicTextBox.Size = new Size(866, 30);
        UnitDicTextBox.TabIndex = 7;
        UnitDicTextBox.Text = "unites.dic";
        UnitDicTextBox.TextChanged += UnitDicTextBox_TextChanged;
        // 
        // BGDicTextBox
        // 
        BGDicTextBox.Location = new Point(263, 434);
        BGDicTextBox.Margin = new Padding(6);
        BGDicTextBox.Name = "BGDicTextBox";
        BGDicTextBox.Size = new Size(866, 30);
        BGDicTextBox.TabIndex = 9;
        BGDicTextBox.Text = "unites.dic";
        BGDicTextBox.TextChanged += BGDicTextBox_TextChanged;
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(25, 440);
        label4.Margin = new Padding(6, 0, 6, 0);
        label4.Name = "label4";
        label4.Size = new Size(144, 24);
        label4.TabIndex = 8;
        label4.Text = "battlegroup.dic";
        // 
        // InterfaceDicTextBox
        // 
        InterfaceDicTextBox.Location = new Point(263, 482);
        InterfaceDicTextBox.Margin = new Padding(6);
        InterfaceDicTextBox.Name = "InterfaceDicTextBox";
        InterfaceDicTextBox.Size = new Size(866, 30);
        InterfaceDicTextBox.TabIndex = 11;
        InterfaceDicTextBox.Text = "unites.dic";
        InterfaceDicTextBox.TextChanged += InterfaceDicTextBox_TextChanged;
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(25, 488);
        label5.Margin = new Padding(6, 0, 6, 0);
        label5.Name = "label5";
        label5.Size = new Size(201, 24);
        label5.TabIndex = 10;
        label5.Text = "interface_outgame.dic";
        // 
        // _validateButton
        // 
        _validateButton.Location = new Point(851, 540);
        _validateButton.Margin = new Padding(6);
        _validateButton.Name = "_validateButton";
        _validateButton.Size = new Size(138, 42);
        _validateButton.TabIndex = 12;
        _validateButton.Text = "Validate";
        _validateButton.UseVisualStyleBackColor = true;
        _validateButton.Click += button3_Click;
        // 
        // button4
        // 
        button4.Location = new Point(995, 540);
        button4.Margin = new Padding(6);
        button4.Name = "button4";
        button4.Size = new Size(138, 42);
        button4.TabIndex = 13;
        button4.Text = "Apply";
        button4.UseVisualStyleBackColor = true;
        button4.Click += button4_Click;
        // 
        // keepLogFilesCheckBox
        // 
        keepLogFilesCheckBox.AutoSize = true;
        keepLogFilesCheckBox.Location = new Point(30, 547);
        keepLogFilesCheckBox.Margin = new Padding(6);
        keepLogFilesCheckBox.Name = "keepLogFilesCheckBox";
        keepLogFilesCheckBox.Size = new Size(155, 28);
        keepLogFilesCheckBox.TabIndex = 14;
        keepLogFilesCheckBox.Text = "Keep Log files";
        keepLogFilesCheckBox.UseVisualStyleBackColor = true;
        // 
        // fullSummaryCheckBox
        // 
        fullSummaryCheckBox.AutoSize = true;
        fullSummaryCheckBox.Location = new Point(30, 589);
        fullSummaryCheckBox.Margin = new Padding(6);
        fullSummaryCheckBox.Name = "fullSummaryCheckBox";
        fullSummaryCheckBox.Size = new Size(324, 28);
        fullSummaryCheckBox.TabIndex = 15;
        fullSummaryCheckBox.Text = "Produce Full Campaign Summary";
        fullSummaryCheckBox.UseVisualStyleBackColor = true;
        // 
        // _exportAllPawnsCheckBox
        // 
        _exportAllPawnsCheckBox.AutoSize = true;
        _exportAllPawnsCheckBox.Location = new Point(30, 632);
        _exportAllPawnsCheckBox.Margin = new Padding(6);
        _exportAllPawnsCheckBox.Name = "_exportAllPawnsCheckBox";
        _exportAllPawnsCheckBox.Size = new Size(235, 28);
        _exportAllPawnsCheckBox.TabIndex = 16;
        _exportAllPawnsCheckBox.Text = "Global pawn export list";
        _exportAllPawnsCheckBox.UseVisualStyleBackColor = true;
        // 
        // DictionaryFileTextBox_BG
        // 
        DictionaryFileTextBox_BG.Location = new Point(21, 227);
        DictionaryFileTextBox_BG.Margin = new Padding(6);
        DictionaryFileTextBox_BG.Name = "DictionaryFileTextBox_BG";
        DictionaryFileTextBox_BG.Size = new Size(1108, 30);
        DictionaryFileTextBox_BG.TabIndex = 17;
        // 
        // label6
        // 
        label6.AutoSize = true;
        label6.Location = new Point(26, 187);
        label6.Margin = new Padding(6, 0, 6, 0);
        label6.Name = "label6";
        label6.Size = new Size(272, 24);
        label6.TabIndex = 18;
        label6.Text = "ZZ_Win.dat file path (BG Hash)";
        // 
        // button3
        // 
        button3.Location = new Point(1144, 221);
        button3.Margin = new Padding(6);
        button3.Name = "button3";
        button3.Size = new Size(59, 42);
        button3.TabIndex = 19;
        button3.Text = "...";
        button3.UseVisualStyleBackColor = true;
        button3.Click += Btn_BG_Select;
        // 
        // DictionaryFileTextBox_I
        // 
        DictionaryFileTextBox_I.Location = new Point(20, 323);
        DictionaryFileTextBox_I.Margin = new Padding(6);
        DictionaryFileTextBox_I.Name = "DictionaryFileTextBox_I";
        DictionaryFileTextBox_I.Size = new Size(1108, 30);
        DictionaryFileTextBox_I.TabIndex = 20;
        // 
        // label7
        // 
        label7.AutoSize = true;
        label7.Location = new Point(26, 279);
        label7.Margin = new Padding(6, 0, 6, 0);
        label7.Name = "label7";
        label7.Size = new Size(253, 24);
        label7.TabIndex = 21;
        label7.Text = "ZZ_Win.dat file path (I Hash)";
        // 
        // button5
        // 
        button5.Location = new Point(1144, 317);
        button5.Margin = new Padding(6);
        button5.Name = "button5";
        button5.Size = new Size(59, 42);
        button5.TabIndex = 22;
        button5.Text = "...";
        button5.UseVisualStyleBackColor = true;
        button5.Click += Btn_I_Select;
        // 
        // ConfigurationForm
        // 
        AutoScaleDimensions = new SizeF(11F, 24F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1232, 689);
        Controls.Add(button5);
        Controls.Add(label7);
        Controls.Add(DictionaryFileTextBox_I);
        Controls.Add(button3);
        Controls.Add(label6);
        Controls.Add(DictionaryFileTextBox_BG);
        Controls.Add(_exportAllPawnsCheckBox);
        Controls.Add(fullSummaryCheckBox);
        Controls.Add(keepLogFilesCheckBox);
        Controls.Add(button4);
        Controls.Add(_validateButton);
        Controls.Add(InterfaceDicTextBox);
        Controls.Add(label5);
        Controls.Add(BGDicTextBox);
        Controls.Add(label4);
        Controls.Add(UnitDicTextBox);
        Controls.Add(label3);
        Controls.Add(button2);
        Controls.Add(DictionaryFileTextBox);
        Controls.Add(label2);
        Controls.Add(button1);
        Controls.Add(NDF_DatFileTextBox);
        Controls.Add(label1);
        Margin = new Padding(6);
        Name = "ConfigurationForm";
        Text = "Settings";
        ResumeLayout(false);
        PerformLayout();
    }

    private void Btn_BG_Select(object sender, EventArgs e)
    {
        OpenFileDialog dlg = new OpenFileDialog();
        DialogResult dr = dlg.ShowDialog();
        if (dr == DialogResult.OK)
        {
            DictionaryFileTextBox_BG.Text = dlg.FileName;
        }
    }

    private void Btn_I_Select(object sender, EventArgs e)
    {
        OpenFileDialog dlg = new OpenFileDialog();
        DialogResult dr = dlg.ShowDialog();
        if (dr == DialogResult.OK)
        {
            DictionaryFileTextBox_I.Text = dlg.FileName;
        }
    }
}
