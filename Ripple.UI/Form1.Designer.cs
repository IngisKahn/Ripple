namespace Ripple.UI;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        var resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        this.pictureBox1 = new PictureBox();
        this.toolStrip1 = new ToolStrip();
        this.loadButton = new ToolStripButton();
        this.saveButton = new ToolStripButton();
        this.toolStripSeparator1 = new ToolStripSeparator();
        this.addButton = new ToolStripButton();
        this.connectButton = new ToolStripButton();
        this.moveButton = new ToolStripButton();
        ((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
        this.toolStrip1.SuspendLayout();
        this.SuspendLayout();
        // 
        // pictureBox1
        // 
        this.pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.pictureBox1.BackColor = SystemColors.Control;
        this.pictureBox1.Location = new Point(0, 25);
        this.pictureBox1.Name = "pictureBox1";
        this.pictureBox1.Size = new Size(800, 424);
        this.pictureBox1.TabIndex = 0;
        this.pictureBox1.TabStop = false;
        // 
        // toolStrip1
        // 
        this.toolStrip1.Items.AddRange(new ToolStripItem[] { this.loadButton, this.saveButton, this.toolStripSeparator1, this.addButton, this.connectButton, this.moveButton });
        this.toolStrip1.Location = new Point(0, 0);
        this.toolStrip1.Name = "toolStrip1";
        this.toolStrip1.Size = new Size(800, 25);
        this.toolStrip1.TabIndex = 1;
        this.toolStrip1.Text = "toolStrip1";
        // 
        // loadButton
        // 
        this.loadButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        this.loadButton.Image = (Image)resources.GetObject("loadButton.Image");
        this.loadButton.ImageTransparentColor = Color.Magenta;
        this.loadButton.Name = "loadButton";
        this.loadButton.Size = new Size(37, 22);
        this.loadButton.Text = "Load";
        this.loadButton.ToolTipText = "Load";
        this.loadButton.Click += this.loadButton_Click;
        // 
        // saveButton
        // 
        this.saveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        this.saveButton.Image = (Image)resources.GetObject("saveButton.Image");
        this.saveButton.ImageTransparentColor = Color.Magenta;
        this.saveButton.Name = "saveButton";
        this.saveButton.Size = new Size(35, 22);
        this.saveButton.Text = "Save";
        this.saveButton.Click += this.saveButton_Click;
        // 
        // toolStripSeparator1
        // 
        this.toolStripSeparator1.Name = "toolStripSeparator1";
        this.toolStripSeparator1.Size = new Size(6, 25);
        // 
        // addButton
        // 
        this.addButton.Checked = true;
        this.addButton.CheckState = CheckState.Checked;
        this.addButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        this.addButton.Image = (Image)resources.GetObject("addButton.Image");
        this.addButton.ImageTransparentColor = Color.Magenta;
        this.addButton.Name = "addButton";
        this.addButton.Size = new Size(33, 22);
        this.addButton.Text = "Add";
        // 
        // connectButton
        // 
        this.connectButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        this.connectButton.Image = (Image)resources.GetObject("connectButton.Image");
        this.connectButton.ImageTransparentColor = Color.Magenta;
        this.connectButton.Name = "connectButton";
        this.connectButton.Size = new Size(56, 22);
        this.connectButton.Text = "Connect";
        // 
        // moveButton
        // 
        this.moveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        this.moveButton.Image = (Image)resources.GetObject("moveButton.Image");
        this.moveButton.ImageTransparentColor = Color.Magenta;
        this.moveButton.Name = "moveButton";
        this.moveButton.Size = new Size(41, 22);
        this.moveButton.Text = "Move";
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(800, 450);
        this.Controls.Add(this.toolStrip1);
        this.Controls.Add(this.pictureBox1);
        this.Name = "Form1";
        this.Text = "Form1";
        ((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
        this.toolStrip1.ResumeLayout(false);
        this.toolStrip1.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private PictureBox pictureBox1;
    private ToolStrip toolStrip1;
    private ToolStripButton loadButton;
    private ToolStripButton saveButton;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripButton addButton;
    private ToolStripButton connectButton;
    private ToolStripButton moveButton;
}
