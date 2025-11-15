// XBit/Dialogs/TaskAddDialog.cs

using System;
using System.Drawing;
using System.Windows.Forms;
using XBit;

namespace XBit.Pages
{
    public class TaskAddDialog : Form
    {
        private TextBox txtTitle;
        private TextBox txtAssignee;
        private ComboBox cmbPriority;
        private Button btnOk;
        private Button btnCancel;

        public string TaskTitle => txtTitle.Text.Trim();
        public string Assignee => txtAssignee.Text.Trim();
        public int Priority => cmbPriority.SelectedIndex + 1; // 1=긴급, 2=높음, 3=보통, 4=낮음

        public TaskAddDialog()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "새 작업 추가";
            Width = 450;
            Height = 280;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.BgMain;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(20)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 제목
            var lblTitle = new Label { Text = "작업 제목:", AutoSize = true, ForeColor = Theme.FgDefault };
            txtTitle = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgCard, ForeColor = Theme.FgDefault };

            // 담당자
            var lblAssignee = new Label { Text = "담당자:", AutoSize = true, ForeColor = Theme.FgDefault };
            txtAssignee = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgCard, ForeColor = Theme.FgDefault };

            // 우선순위
            var lblPriority = new Label { Text = "우선순위:", AutoSize = true, ForeColor = Theme.FgDefault };
            cmbPriority = new ComboBox 
            { 
                Dock = DockStyle.Fill, 
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgCard,
                ForeColor = Theme.FgDefault
            };
            cmbPriority.Items.AddRange(new string[] { "[긴급]", "[높음]", "[보통]", "[낮음]" });
            cmbPriority.SelectedIndex = 2; // 기본값: 보통

            // 버튼
            btnOk = new Button { Text = "추가", Width = 80, Height = 35, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "취소", Width = 80, Height = 35, DialogResult = DialogResult.Cancel };
            
            Theme.StylePrimaryButton(btnOk);
            Theme.StyleButton(btnCancel);

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            pnlButtons.Controls.Add(btnOk);
            pnlButtons.Controls.Add(btnCancel);

            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(txtTitle, 1, 0);
            layout.Controls.Add(lblAssignee, 0, 1);
            layout.Controls.Add(txtAssignee, 1, 1);
            layout.Controls.Add(lblPriority, 0, 2);
            layout.Controls.Add(cmbPriority, 1, 2);
            layout.Controls.Add(new Label(), 0, 3); // Spacer
            layout.Controls.Add(pnlButtons, 1, 4);

            Controls.Add(layout);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}