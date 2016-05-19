namespace VsxFactory.Modeling.VisualStudio.Synchronization
{
 partial class TypeChooser
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
   this.label1 = new System.Windows.Forms.Label();
   this.okButton = new System.Windows.Forms.Button();
   this.cancelButton = new System.Windows.Forms.Button();
   this.listView = new System.Windows.Forms.ListView();
   this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
   this.SuspendLayout();
   // 
   // label1
   // 
   this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
               | System.Windows.Forms.AnchorStyles.Right)));
   this.label1.AutoSize = true;
   this.label1.Location = new System.Drawing.Point(1, 1);
   this.label1.Name = "label1";
   this.label1.Size = new System.Drawing.Size(109, 13);
   this.label1.TabIndex = 0;
   this.label1.Text = "Please choose a type";
   // 
   // okButton
   // 
   this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
   this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
   this.okButton.Location = new System.Drawing.Point(13, 231);
   this.okButton.Name = "okButton";
   this.okButton.Size = new System.Drawing.Size(75, 23);
   this.okButton.TabIndex = 1;
   this.okButton.Text = "Ok";
   this.okButton.UseVisualStyleBackColor = true;
   // 
   // cancelButton
   // 
   this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
   this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
   this.cancelButton.Location = new System.Drawing.Point(116, 231);
   this.cancelButton.Name = "cancelButton";
   this.cancelButton.Size = new System.Drawing.Size(75, 23);
   this.cancelButton.TabIndex = 2;
   this.cancelButton.Text = "Cancel";
   this.cancelButton.UseVisualStyleBackColor = true;
   // 
   // listView
   // 
   this.listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
               | System.Windows.Forms.AnchorStyles.Left)
               | System.Windows.Forms.AnchorStyles.Right)));
   this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
   this.listView.Location = new System.Drawing.Point(13, 18);
   this.listView.Name = "listView";
   this.listView.Size = new System.Drawing.Size(508, 207);
   this.listView.TabIndex = 3;
   this.listView.UseCompatibleStateImageBehavior = false;
   this.listView.View = System.Windows.Forms.View.Details;
   // 
   // columnHeader1
   // 
   this.columnHeader1.Text = "FullName";
   this.columnHeader1.Width = 500;
   // 
   // TypeChooser
   // 
   this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
   this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
   this.ClientSize = new System.Drawing.Size(533, 266);
   this.Controls.Add(this.listView);
   this.Controls.Add(this.cancelButton);
   this.Controls.Add(this.okButton);
   this.Controls.Add(this.label1);
   this.Name = "TypeChooser";
   this.Text = "Type Chooser";
   this.ResumeLayout(false);
   this.PerformLayout();

  }

  #endregion

  private System.Windows.Forms.Label label1;
  private System.Windows.Forms.Button okButton;
  private System.Windows.Forms.Button cancelButton;
  private System.Windows.Forms.ListView listView;
  private System.Windows.Forms.ColumnHeader columnHeader1;
 }
}