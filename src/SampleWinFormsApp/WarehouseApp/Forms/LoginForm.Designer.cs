namespace WarehouseApp.Forms;

partial class LoginForm
{
    private System.ComponentModel.IContainer components = null!;
    private PictureBox logoPictureBox = null!;
    private Label titleLabel = null!;
    private Label subtitleLabel = null!;
    private Label usernameLabel = null!;
    private TextBox usernameTextBox = null!;
    private Label passwordLabel = null!;
    private TextBox passwordTextBox = null!;
    private CheckBox rememberMeCheckBox = null!;
    private Button loginButton = null!;
    private ProgressBar loginProgressBar = null!;
    private WarehouseApp.Controls.LoadingSpinnerControl loadingSpinner = null!;
    private Label statusLabel = null!;
    private ErrorProvider errorProvider = null!;
    private TableLayoutPanel layoutPanel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.logoPictureBox = new System.Windows.Forms.PictureBox();
        this.titleLabel = new System.Windows.Forms.Label();
        this.subtitleLabel = new System.Windows.Forms.Label();
        this.usernameLabel = new System.Windows.Forms.Label();
        this.usernameTextBox = new System.Windows.Forms.TextBox();
        this.passwordLabel = new System.Windows.Forms.Label();
        this.passwordTextBox = new System.Windows.Forms.TextBox();
        this.rememberMeCheckBox = new System.Windows.Forms.CheckBox();
        this.loginButton = new System.Windows.Forms.Button();
        this.loginProgressBar = new System.Windows.Forms.ProgressBar();
        this.loadingSpinner = new WarehouseApp.Controls.LoadingSpinnerControl();
        this.statusLabel = new System.Windows.Forms.Label();
        this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
        this.layoutPanel = new System.Windows.Forms.TableLayoutPanel();

        ((System.ComponentModel.ISupportInitialize)this.logoPictureBox).BeginInit();
        this.SuspendLayout();

        this.logoPictureBox.Image = Common.AppIcons.CreateLogo(72);
        this.logoPictureBox.Size = new System.Drawing.Size(72, 72);
        this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.logoPictureBox.Location = new System.Drawing.Point(164, 24);

        this.titleLabel.Text = "Warehouse Management System";
        this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold);
        this.titleLabel.AutoSize = true;
        this.titleLabel.Location = new System.Drawing.Point(60, 104);

        this.subtitleLabel.Text = "Sign in to continue";
        this.subtitleLabel.ForeColor = System.Drawing.Color.Gray;
        this.subtitleLabel.AutoSize = true;
        this.subtitleLabel.Location = new System.Drawing.Point(160, 132);

        this.layoutPanel.ColumnCount = 2;
        this.layoutPanel.RowCount = 2;
        this.layoutPanel.Location = new System.Drawing.Point(70, 170);
        this.layoutPanel.Size = new System.Drawing.Size(260, 64);
        this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80));
        this.layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        this.layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30));
        this.layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30));

        this.usernameLabel.Text = "Username:";
        this.usernameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.usernameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.usernameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.usernameTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);

        this.passwordLabel.Text = "Password:";
        this.passwordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.passwordLabel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.passwordTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        this.passwordTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
        this.passwordTextBox.UseSystemPasswordChar = true;

        this.layoutPanel.Controls.Add(this.usernameLabel, 0, 0);
        this.layoutPanel.Controls.Add(this.usernameTextBox, 1, 0);
        this.layoutPanel.Controls.Add(this.passwordLabel, 0, 1);
        this.layoutPanel.Controls.Add(this.passwordTextBox, 1, 1);

        this.rememberMeCheckBox.Text = "Remember me";
        this.rememberMeCheckBox.AutoSize = true;
        this.rememberMeCheckBox.Location = new System.Drawing.Point(70, 240);

        this.loginButton.Text = "Log In";
        this.loginButton.Size = new System.Drawing.Size(100, 32);
        this.loginButton.Location = new System.Drawing.Point(230, 236);
        this.loginButton.UseVisualStyleBackColor = true;
        this.loginButton.Click += this.loginButton_Click;

        this.loginProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
        this.loginProgressBar.MarqueeAnimationSpeed = 30;
        this.loginProgressBar.Location = new System.Drawing.Point(70, 280);
        this.loginProgressBar.Size = new System.Drawing.Size(260, 10);
        this.loginProgressBar.Visible = false;

        this.loadingSpinner.Location = new System.Drawing.Point(340, 234);
        this.loadingSpinner.Size = new System.Drawing.Size(28, 28);
        this.loadingSpinner.Spinning = false;

        this.statusLabel.AutoSize = true;
        this.statusLabel.ForeColor = System.Drawing.Color.Firebrick;
        this.statusLabel.Location = new System.Drawing.Point(70, 296);
        this.statusLabel.Size = new System.Drawing.Size(260, 20);

        this.errorProvider.ContainerControl = this;

        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(400, 340);
        this.Controls.Add(this.logoPictureBox);
        this.Controls.Add(this.titleLabel);
        this.Controls.Add(this.subtitleLabel);
        this.Controls.Add(this.layoutPanel);
        this.Controls.Add(this.rememberMeCheckBox);
        this.Controls.Add(this.loginButton);
        this.Controls.Add(this.loginProgressBar);
        this.Controls.Add(this.loadingSpinner);
        this.Controls.Add(this.statusLabel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Sign In — WarehouseApp";
        this.AcceptButton = this.loginButton;

        ((System.ComponentModel.ISupportInitialize)this.logoPictureBox).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
