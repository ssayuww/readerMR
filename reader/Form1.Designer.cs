namespace reader;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Panel panelStatus;
    private System.Windows.Forms.Label labelActivate;
    private System.Windows.Forms.Label labelDeactivate;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        panelStatus = new System.Windows.Forms.Panel();
        labelActivate = new System.Windows.Forms.Label();
        labelDeactivate = new System.Windows.Forms.Label();
        SuspendLayout();

        panelStatus.Location = new Point(12, 12);
        panelStatus.Name = "panelStatus";
        panelStatus.Size = new Size(20, 20);
        panelStatus.TabIndex = 0;

        labelActivate.AutoSize = true;
        labelActivate.Location = new Point(45, 12);
        labelActivate.Name = "labelActivate";
        labelActivate.Size = new Size(49, 15);
        labelActivate.TabIndex = 1;
        labelActivate.Text = "Activate";

        labelDeactivate.AutoSize = true;
        labelDeactivate.Location = new Point(45, 40);
        labelDeactivate.Name = "labelDeactivate";
        labelDeactivate.Size = new Size(65, 15);
        labelDeactivate.TabIndex = 2;
        labelDeactivate.Text = "Deactivate";

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(160, 80);
        Controls.Add(labelDeactivate);
        Controls.Add(labelActivate);
        Controls.Add(panelStatus);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Name = "Form1";
        Text = "Reader";
        ResumeLayout(false);
        PerformLayout();
    }
}