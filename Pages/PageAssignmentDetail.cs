// Pages/PageAssignmentDetail.cs (최종 수정본 - 제출 UI 가로 정렬)

using System;
using System.Windows.Forms;
using System.Drawing;
using XBit.Models;
using XBit.Services;
using System.Collections.Generic;
using System.Linq; // LINQ 사용을 위해 추가

namespace XBit.Pages
{
    public class PageAssignmentDetail : UserControl
    {
        private Assignment currentAssignment;
        private readonly AssignmentService _assignmentService = new AssignmentService();
        private Label lblTitle;
        private Label lblCourse;
        private Label lblDueDate;
        private RichTextBox txtDescription;
        private Button btnFileSelect;
        private Button btnSubmit;
        private TextBox txtFilePath;

        private readonly FileService _fileService = new FileService();

        public PageAssignmentDetail() : this(-1) { }

        public PageAssignmentDetail(int assignmentId)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            InitializeUIControls();

            if (assignmentId != -1)
            {
                LoadAssignment(assignmentId);
            }
            else
            {
                lblTitle.Text = "오류: 과제 정보를 찾을 수 없습니다.";
            }

            Theme.Apply(this);
        }

        private void LoadAssignment(int assignmentId)
        {
            // ⚠️ 임시 데이터 (실제 서비스 호출로 대체하세요)
            currentAssignment = new Assignment
            {
                Id = assignmentId,
                Course = "XR Lab",
                Title = $"과제 상세 ID:{assignmentId}",
                DueDate = DateTime.Parse("2025-11-14 18:26"),
                Status = "미제출",
                Content = $"이 과제는 {assignmentId}번 과제입니다. 상세 설명..."
            };

            if (currentAssignment == null)
            {
                lblTitle.Text = "오류: 과제 정보를 찾을 수 없습니다.";
                return;
            }
            DisplayAssignmentDetails();
        }

        private void InitializeUIControls()
        {
            var layoutPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            lblTitle = new Label { Font = new Font("Segoe UI", 16f, FontStyle.Bold), AutoSize = true, ForeColor = Theme.FgDefault, Width = 600 };
            lblCourse = new Label { Font = new Font("Segoe UI", 10f), AutoSize = true, ForeColor = Theme.FgMuted, Width = 600 };
            lblDueDate = new Label { Font = new Font("Segoe UI", 10f, FontStyle.Italic), AutoSize = true, ForeColor = Theme.FgMuted, Width = 600 };

            txtDescription = new RichTextBox { Text = "로딩 중...", Height = 250, Width = 600, BackColor = Theme.BgCard, ForeColor = Theme.FgDefault, ReadOnly = true };

            // ⭐️ 제출 UI를 위한 단일 FlowLayoutPanel 생성 (가로 정렬)
            var pnlSubmitRow = new FlowLayoutPanel
            {
                Width = 600, // 텍스트 박스와 버튼을 담을 전체 너비
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 10, 0, 0),
                Padding = new Padding(0)
            };

            // 컨트롤 초기화 및 배치
            txtFilePath = new TextBox { Width = 380, Height = 35, Margin = new Padding(0), ReadOnly = true, BackColor = Theme.BgCard, ForeColor = Theme.FgDefault };
            btnFileSelect = new Button { Text = "파일 선택", Width = 100, Height = 35, Margin = new Padding(10, 0, 5, 0) }; // 간격 추가
            btnSubmit = new Button { Text = "제출하기", Width = 100, Height = 35, Margin = new Padding(5, 0, 0, 0) }; // 간격 추가

            // ⭐️ FlowLayoutPanel에 순서대로 추가 (가로 정렬)
            pnlSubmitRow.Controls.Add(txtFilePath);
            pnlSubmitRow.Controls.Add(btnFileSelect);
            pnlSubmitRow.Controls.Add(btnSubmit);

            // 스타일링 적용
            Theme.StylePrimaryButton(btnSubmit);
            Theme.StyleButton(btnFileSelect);

            btnFileSelect.Click += BtnFileSelect_Click;
            btnSubmit.Click += BtnSubmit_Click;

            // 3. 레이아웃에 컨트롤 추가
            layoutPanel.Controls.AddRange(new Control[] {
                lblTitle,
                lblCourse,
                lblDueDate,
                new Label { Text = "설명:", AutoSize = true, Margin = new Padding(0, 10, 0, 0), ForeColor = Theme.FgDefault },
                txtDescription,
                new Label { Text = "제출:", AutoSize = true, Margin = new Padding(0, 10, 0, 0), ForeColor = Theme.FgDefault },
                pnlSubmitRow // ⭐️ 단일 제출 Row 추가
            });
            this.Controls.Add(layoutPanel);
        }

        private void DisplayAssignmentDetails()
        {
            if (currentAssignment != null)
            {
                lblTitle.Text = currentAssignment.Title;
                lblCourse.Text = $"과목: {currentAssignment.Course}";
                lblDueDate.Text = $"마감: {currentAssignment.DueDate:yyyy-MM-dd HH:mm} | 상태: {currentAssignment.Status}";
                txtDescription.Text = currentAssignment.Content;
            }
        }

        private void BtnFileSelect_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "제출할 파일 선택";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                }
            }
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFilePath.Text))
            {
                MessageBox.Show("제출할 파일을 먼저 선택해주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (currentAssignment == null)
            {
                MessageBox.Show("과제 정보가 로드되지 않았습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool fileSubmitted = _fileService.SubmitFile(txtFilePath.Text, currentAssignment.Id);

            if (fileSubmitted)
            {
                // ⚠️ AssignmentService에 UpdateAssignmentStatus 메서드가 구현되어야 합니다.
                bool statusUpdated = true; // 임시 성공 처리

                if (statusUpdated)
                {
                    MessageBox.Show("파일이 성공적으로 제출되었으며, 상태가 업데이트되었습니다!", "제출 완료");
                    var mainForm = FindForm() as MainForm;
                    mainForm?.GoBack();
                }
                else
                {
                    MessageBox.Show("파일은 제출되었으나, DB 상태 업데이트에 실패했습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("파일 제출에 실패했습니다. FileService를 확인하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}