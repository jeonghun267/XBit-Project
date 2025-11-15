// Pages/PageAssignmentDetail.cs (최종 완성본 - DB 연동 + UI 개선 + C# 7.3 호환)

using System;
using System.Windows.Forms;
using System.Drawing;
using XBit.Models;
using XBit.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Octokit;

namespace XBit.Pages
{
    public class PageAssignmentDetail : UserControl
    {
        private Assignment currentAssignment;
        private readonly AssignmentService _assignmentService = new AssignmentService();
        private readonly GitHubService _gitHubService = new GitHubService();

        // ⭐️ Label 충돌 해결: WinForms의 Label을 명시적으로 사용
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblCourse;
        private System.Windows.Forms.Label lblDueDate;
        private System.Windows.Forms.Label lblStatusBadge;      // ✅ 상태 배지
        private System.Windows.Forms.Label lblTimeRemaining;    // ✅ 남은 시간

        private RichTextBox txtDescription;
        private Button btnFileSelect;
        private Button btnSubmit;
        private TextBox txtFilePath;
        private System.Windows.Forms.Label lblFileInfo;         // ✅ 파일 정보
        private ProgressBar prgSubmitting;                       // ✅ 진행 표시

        private readonly FileService _fileService = new FileService();

        private const int ContentWidth = 600;

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
                System.Windows.Forms.MessageBox.Show("오류: 과제 정보를 찾을 수 없습니다.");
            }

            Theme.Apply(this);
        }

        private void LoadAssignment(int assignmentId)
        {
            // ✅ 수정: 실제 DB 호출로 변경
            currentAssignment = _assignmentService.GetAssignmentById(assignmentId);

            if (currentAssignment == null)
            {
                System.Windows.Forms.MessageBox.Show("오류: 과제 정보를 찾을 수 없습니다.");
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
                Padding = new System.Windows.Forms.Padding(20)
            };

            // 제목
            lblTitle = new System.Windows.Forms.Label
            {
                Font = new System.Drawing.Font("Segoe UI", 16f, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                ForeColor = Theme.FgDefault,
                Width = ContentWidth
            };

            // 과목
            lblCourse = new System.Windows.Forms.Label
            {
                Font = new System.Drawing.Font("Segoe UI", 10f),
                AutoSize = true,
                ForeColor = Theme.FgMuted,
                Width = ContentWidth
            };

            // 마감일
            lblDueDate = new System.Windows.Forms.Label
            {
                Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Italic),
                AutoSize = true,
                ForeColor = Theme.FgMuted,
                Width = ContentWidth
            };

            // ✅ 상태 배지
            lblStatusBadge = new System.Windows.Forms.Label
            {
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Padding = new System.Windows.Forms.Padding(8, 4, 8, 4),
                Margin = new System.Windows.Forms.Padding(0, 5, 0, 0),
                BackColor = Theme.BgCard,
                ForeColor = Color.White
            };

            // ✅ 남은 시간 표시
            lblTimeRemaining = new System.Windows.Forms.Label
            {
                Font = new System.Drawing.Font("Segoe UI", 9f),
                AutoSize = true,
                ForeColor = Theme.FgMuted,
                Margin = new System.Windows.Forms.Padding(0, 5, 0, 0)
            };

            // 설명
            txtDescription = new RichTextBox
            {
                Text = "로딩 중...",
                Height = 250,
                Width = ContentWidth,
                BackColor = Theme.BgCard,
                ForeColor = Theme.FgDefault,
                ReadOnly = true
            };

            // 파일 선택 영역
            var pnlSubmitRow = new FlowLayoutPanel
            {
                Width = ContentWidth,
                Height = 40,
                FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight,
                Margin = new System.Windows.Forms.Padding(0, 10, 0, 0),
                Padding = new System.Windows.Forms.Padding(0)
            };

            txtFilePath = new TextBox
            {
                Width = 380,
                Height = 35,
                Margin = new System.Windows.Forms.Padding(0),
                ReadOnly = true,
                BackColor = Theme.BgCard,
                ForeColor = Theme.FgDefault
            };

            btnFileSelect = new Button
            {
                Text = "파일 선택",
                Width = 100,
                Height = 35,
                Margin = new System.Windows.Forms.Padding(10, 0, 5, 0)
            };

            btnSubmit = new Button
            {
                Text = "제출하기",
                Width = 100,
                Height = 35,
                Margin = new System.Windows.Forms.Padding(5, 0, 0, 0)
            };

            pnlSubmitRow.Controls.Add(txtFilePath);
            pnlSubmitRow.Controls.Add(btnFileSelect);
            pnlSubmitRow.Controls.Add(btnSubmit);

            Theme.StylePrimaryButton(btnSubmit);
            Theme.StyleButton(btnFileSelect);

            btnFileSelect.Click += BtnFileSelect_Click;
            btnSubmit.Click += BtnSubmit_Click;

            // ✅ 파일 정보 라벨
            lblFileInfo = new System.Windows.Forms.Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Theme.FgMuted,
                Font = new System.Drawing.Font("Segoe UI", 9f),
                Margin = new System.Windows.Forms.Padding(0, 5, 0, 0)
            };

            // ✅ 진행 표시기
            prgSubmitting = new ProgressBar
            {
                Width = ContentWidth,
                Height = 4,
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                Margin = new System.Windows.Forms.Padding(0, 10, 0, 0)
            };

            layoutPanel.Controls.AddRange(new Control[] {
                lblTitle,
                lblCourse,
                lblDueDate,
                lblStatusBadge,
                lblTimeRemaining,
                new System.Windows.Forms.Label { Text = "설명:", AutoSize = true, Margin = new System.Windows.Forms.Padding(0, 10, 0, 0), ForeColor = Theme.FgDefault },
                txtDescription,
                new System.Windows.Forms.Label { Text = "제출:", AutoSize = true, Margin = new System.Windows.Forms.Padding(0, 10, 0, 0), ForeColor = Theme.FgDefault },
                pnlSubmitRow,
                lblFileInfo,
                prgSubmitting
            });
            this.Controls.Add(layoutPanel);
        }

        private void DisplayAssignmentDetails()
        {
            if (currentAssignment != null)
            {
                lblTitle.Text = currentAssignment.Title;
                lblCourse.Text = $"과목: {currentAssignment.Course}";
                lblDueDate.Text = $"마감: {currentAssignment.DueDate:yyyy-MM-dd HH:mm}";
                txtDescription.Text = currentAssignment.Content;

                // ✅ 상태 배지 업데이트
                UpdateStatusBadge(currentAssignment.Status);

                // ✅ 남은 시간 계산
                UpdateTimeRemaining(currentAssignment.DueDate);
            }
            else
            {
                lblTitle.Text = "오류: 과제 정보 로드 실패";
            }
        }

        // ✅ 상태 배지 색상 업데이트 (C# 7.3 호환 - if/else 사용)
        private void UpdateStatusBadge(string status)
        {
            lblStatusBadge.Text = status;

            if (status == "미제출")
            {
                lblStatusBadge.BackColor = Color.FromArgb(255, 152, 0);
            }
            else if (status == "제출 완료")
            {
                lblStatusBadge.BackColor = Color.FromArgb(76, 175, 80);
            }
            else if (status.Contains("PR 제출됨"))
            {
                lblStatusBadge.BackColor = Color.FromArgb(66, 133, 244);
            }
            else
            {
                lblStatusBadge.BackColor = Theme.BgCard;
            }

            lblStatusBadge.ForeColor = Color.White;
        }

        // ✅ 남은 시간 계산 (C# 7.3 호환 - if/else 사용)
        private void UpdateTimeRemaining(DateTime dueDate)
        {
            TimeSpan remaining = dueDate - DateTime.Now;

            if (remaining.TotalHours < 0)
            {
                lblTimeRemaining.Text = "⏰ 마감 완료";
                lblTimeRemaining.ForeColor = Color.FromArgb(244, 67, 54);
            }
            else if (remaining.TotalHours < 1)
            {
                lblTimeRemaining.Text = $"⚠️ {(int)remaining.TotalMinutes}분 남음";
                lblTimeRemaining.ForeColor = Color.FromArgb(244, 67, 54);
            }
            else if (remaining.TotalHours < 24)
            {
                lblTimeRemaining.Text = $"⏰ {(int)remaining.TotalHours}시간 {remaining.Minutes}분 남음";
                lblTimeRemaining.ForeColor = Color.FromArgb(255, 152, 0);
            }
            else
            {
                lblTimeRemaining.Text = $"📅 {(int)remaining.TotalDays}일 {remaining.Hours}시간 남음";
                lblTimeRemaining.ForeColor = Theme.FgMuted;
            }
        }

        private void BtnFileSelect_Click(object sender, System.EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "제출할 파일 선택";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;

                    // ✅ 파일 정보 표시
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    string sizeText = FormatFileSize(fileInfo.Length);
                    lblFileInfo.Text = $"✓ {Path.GetFileName(openFileDialog.FileName)} ({sizeText})";
                    lblFileInfo.ForeColor = Color.FromArgb(76, 175, 80);
                }
            }
        }

        // ✅ 파일 크기 포맷
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void BtnSubmit_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFilePath.Text))
            {
                System.Windows.Forms.MessageBox.Show("제출할 파일을 먼저 선택해주세요.", "경고", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            if (currentAssignment == null)
            {
                System.Windows.Forms.MessageBox.Show("과제 정보가 로드되지 않았습니다.", "오류", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            string prTitle = $"Project #{currentAssignment.Id}: {currentAssignment.Title}";
            string localFilePath = txtFilePath.Text;

            // ✅ 제출 중 UI 상태 변경
            btnSubmit.Enabled = false;
            btnFileSelect.Enabled = false;
            prgSubmitting.Visible = true;

            try
            {
                _fileService.SubmitFile(localFilePath, currentAssignment.Id);
                string branchUsed = await _gitHubService.CommitAndPush(currentAssignment.Id, localFilePath);

                string statusUpdateMessage;
                bool isFirstSubmission = !currentAssignment.Status.Contains("PR 제출됨");

                if (isFirstSubmission)
                {
                    int prNumber = await _gitHubService.CreatePullRequest(prTitle, branchUsed);
                    statusUpdateMessage = $"PR 제출됨 (#{prNumber})";

                    System.Windows.Forms.MessageBox.Show($"GitHub에 PR이 성공적으로 생성되었습니다! PR 번호: #{prNumber}", "제출 완료", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    statusUpdateMessage = currentAssignment.Status;
                    System.Windows.Forms.MessageBox.Show($"제출 내용이 기존 PR에 성공적으로 업데이트되었습니다.", "업데이트 완료", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }

                _assignmentService.UpdateAssignmentStatus(currentAssignment.Id, statusUpdateMessage);

                var mainForm = FindForm() as MainForm;
                mainForm?.GoBack();
            }
            catch (FileNotFoundException ex)
            {
                System.Windows.Forms.MessageBox.Show($"파일을 찾을 수 없습니다: {ex.Message}", "오류", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Windows.Forms.MessageBox.Show($"파일 접근 권한 오류: {ex.Message}", "오류", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch (DirectoryNotFoundException ex)
            {
                System.Windows.Forms.MessageBox.Show($"Git 저장소 경로 오류: {ex.Message}", "오류", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch (InvalidOperationException ex)
            {
                System.Windows.Forms.MessageBox.Show($"설정 오류: {ex.Message}", "오류", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch (Octokit.ApiException ex)
            {
                System.Windows.Forms.MessageBox.Show($"GitHub API 오류 (PR 생성 실패): {ex.Message}", "오류", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"예상치 못한 오류가 발생했습니다: {ex.Message}", "오류", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            finally
            {
                // ✅ 제출 완료 후 UI 복원
                btnSubmit.Enabled = true;
                btnFileSelect.Enabled = true;
                prgSubmitting.Visible = false;
            }
        }
    }
}