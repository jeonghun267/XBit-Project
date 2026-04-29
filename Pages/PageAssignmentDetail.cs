using System;
using System.Windows.Forms;
using System.Drawing;
using XBit.Models;
using XBit.Services;
using System.IO;
using System.Threading.Tasks;

namespace XBit.Pages
{
    public class PageAssignmentDetail : UserControl
    {
        private Assignment _assignment;
        private readonly AssignmentService _assignmentService = new AssignmentService();
        private readonly GitHubService _gitHubService = new GitHubService();
        private readonly FileService _fileService = new FileService();

        // UI refs needed after init
        private Label _lblTitle;
        private Label _lblStatusBadge;
        private Label _lblTimeRemaining;
        private RichTextBox _txtDescription;
        private TextBox _txtSubmissionNote;
        private TextBox _txtFilePath;
        private Label _lblFileInfo;
        private ProgressBar _prgSubmitting;
        private Button _btnSubmit;
        private Button _btnFileSelect;

        public PageAssignmentDetail() : this(-1) { }

        public PageAssignmentDetail(int assignmentId)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;
            BuildLayout();

            if (assignmentId > 0)
                LoadAssignment(assignmentId);

            Theme.ThemeChanged += () => { BackColor = Theme.BgMain; Theme.Apply(this); };
        }

        private void LoadAssignment(int id)
        {
            _assignment = _assignmentService.GetAssignmentById(id);
            if (_assignment == null)
            {
                MessageBox.Show("프로젝트 정보를 찾을 수 없습니다.", "오류");
                return;
            }
            PopulateFields();
        }

        // ─────────────────────────────────────────────────────
        // Layout
        // ─────────────────────────────────────────────────────
        private void BuildLayout()
        {
            // ── Top bar (back button + breadcrumb)
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.BgMain
            };

            var btnBack = new Button
            {
                Text = "← 뒤로",
                Width = 80,
                Height = 30,
                Location = new Point(16, 11),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9.5f),
                Cursor = Cursors.Hand
            };
            Theme.StyleButton(btnBack);
            btnBack.Click += (s, e) => (FindForm() as MainForm)?.GoBack();

            var lblBreadcrumb = new Label
            {
                Text = "프로젝트 / 상세",
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(108, 17)
            };

            pnlTop.Controls.Add(btnBack);
            pnlTop.Controls.Add(lblBreadcrumb);

            var topDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };

            // ── Scrollable content
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.BgMain,
                Padding = new Padding(0, 0, 0, 20)
            };

            var content = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(24, 16, 24, 0)
            };
            Theme.EnableDoubleBuffer(content);

            // Info card
            content.Controls.Add(BuildInfoCard());
            // Description card
            content.Controls.Add(BuildDescriptionCard());
            // Submission card
            content.Controls.Add(BuildSubmissionCard());

            scroll.Controls.Add(content);

            Controls.Add(scroll);
            Controls.Add(topDivider);
            Controls.Add(pnlTop);
        }

        // ── Info card (title, course, status, due date, time remaining)
        private Panel BuildInfoCard()
        {
            var card = MakeCard(122);
            card.Tag = "card";

            _lblStatusBadge = new Label
            {
                Text = "로딩 중...",
                Font = new Font("맑은 고딕", 8.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Theme.FgMuted,
                AutoSize = false,
                Width = 80,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 0),
                Tag = "no-theme"
            };

            _lblTitle = new Label
            {
                Text = "로딩 중...",
                Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(0, 30),
                MaximumSize = new Size(550, 0)
            };

            var lblCourseIcon = new Label
            {
                Text = "📚",
                Font = new Font("Segoe UI Emoji", 10f),
                AutoSize = true,
                Location = new Point(0, 68),
                Tag = "no-theme"
            };

            var lblCourseName = new Label
            {
                Name = "lblCourse",
                Text = "",
                Font = new Font("맑은 고딕", 10f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(22, 70)
            };

            var lblDueIcon = new Label
            {
                Text = "🗓",
                Font = new Font("Segoe UI Emoji", 10f),
                AutoSize = true,
                Location = new Point(0, 92),
                Tag = "no-theme"
            };

            var lblDueDate = new Label
            {
                Name = "lblDueDate",
                Text = "",
                Font = new Font("맑은 고딕", 10f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(22, 94)
            };

            _lblTimeRemaining = new Label
            {
                Text = "",
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(220, 94)
            };

            card.Controls.Add(_lblStatusBadge);
            card.Controls.Add(_lblTitle);
            card.Controls.Add(lblCourseIcon);
            card.Controls.Add(lblCourseName);
            card.Controls.Add(lblDueIcon);
            card.Controls.Add(lblDueDate);
            card.Controls.Add(_lblTimeRemaining);

            return card;
        }

        // ── Description card
        private Panel BuildDescriptionCard()
        {
            var card = MakeCard(0);
            card.Tag = "card";
            card.Margin = new Padding(0, 12, 0, 0);

            var lblSec = new Label
            {
                Text = "설명",
                Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            _txtDescription = new RichTextBox
            {
                Location = new Point(0, 28),
                Height = 200,
                BackColor = Theme.BgMain,
                ForeColor = Theme.FgDefault,
                BorderStyle = BorderStyle.None,
                ReadOnly = false,
                Font = new Font("맑은 고딕", 10f),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            _txtDescription.Width = 600;

            card.Height = 240;
            card.Controls.Add(lblSec);
            card.Controls.Add(_txtDescription);

            card.Resize += (s, e) =>
            {
                _txtDescription.Width = card.Width - 40;
            };

            return card;
        }

        // ── Submission card
        private Panel BuildSubmissionCard()
        {
            var card = MakeCard(0);
            card.Tag = "card";
            card.Margin = new Padding(0, 12, 0, 0);

            var lblSec = new Label
            {
                Text = "제출",
                Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(0, 0)
            };

            // Submission note
            var lblNote = new Label
            {
                Text = "제출 메모 (선택)",
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(0, 30)
            };

            _txtSubmissionNote = new TextBox
            {
                Multiline = true,
                Height = 70,
                Location = new Point(0, 50),
                BackColor = Theme.BgMain,
                ForeColor = Theme.FgDefault,
                BorderStyle = BorderStyle.FixedSingle,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("맑은 고딕", 9.5f)
            };
            _txtSubmissionNote.Width = 600;

            // File select row
            var lblFile = new Label
            {
                Text = "파일 선택",
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(0, 136)
            };

            _txtFilePath = new TextBox
            {
                Location = new Point(0, 156),
                Height = 32,
                ReadOnly = true,
                BackColor = Theme.BgMain,
                ForeColor = Theme.FgDefault,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("맑은 고딕", 9.5f)
            };
            _txtFilePath.Width = 480;

            _btnFileSelect = new Button { Text = "찾아보기", Width = 90, Height = 32, Location = new Point(488, 156) };
            Theme.StyleButton(_btnFileSelect);
            _btnFileSelect.Click += BtnFileSelect_Click;

            _lblFileInfo = new Label
            {
                Text = "",
                Font = new Font("맑은 고딕", 8.5f),
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Location = new Point(0, 196)
            };

            _prgSubmitting = new ProgressBar
            {
                Location = new Point(0, 218),
                Height = 4,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            _prgSubmitting.Width = 600;

            _btnSubmit = new Button { Text = "제출하기", Width = 120, Height = 36, Location = new Point(0, 230) };
            Theme.StylePrimaryButton(_btnSubmit);
            _btnSubmit.Click += BtnSubmit_Click;

            card.Height = 286;
            card.Controls.Add(lblSec);
            card.Controls.Add(lblNote);
            card.Controls.Add(_txtSubmissionNote);
            card.Controls.Add(lblFile);
            card.Controls.Add(_txtFilePath);
            card.Controls.Add(_btnFileSelect);
            card.Controls.Add(_lblFileInfo);
            card.Controls.Add(_prgSubmitting);
            card.Controls.Add(_btnSubmit);

            card.Resize += (s, e) =>
            {
                int w = card.Width - 40;
                _txtSubmissionNote.Width = w;
                _txtFilePath.Width = w - 108;
                _btnFileSelect.Location = new Point(_txtFilePath.Right + 8, 156);
                _prgSubmitting.Width = w;
            };

            return card;
        }

        // Helper: card Panel with consistent style
        private Panel MakeCard(int height)
        {
            var card = new Panel
            {
                BackColor = Theme.BgCard,
                Margin = new Padding(0),
                Padding = new Padding(20, 16, 20, 16),
                Tag = "card"
            };
            card.Width = 660;
            if (height > 0) card.Height = height;
            Theme.StyleCard(card);

            // Responsive width
            this.Resize += (s, e) =>
            {
                int w = Math.Min(760, this.ClientSize.Width - 48);
                card.Width = w;
            };

            return card;
        }

        // ─────────────────────────────────────────────────────
        // Data → UI
        // ─────────────────────────────────────────────────────
        private void PopulateFields()
        {
            var a = _assignment;

            _lblTitle.Text = a.Title;
            _txtDescription.Text = a.Content;

            // Find labels in info card
            var infoCard = Controls.Count > 0
                ? FindControlByName<Label>(this, "lblCourse")
                : null;
            if (infoCard != null) infoCard.Text = a.Course;

            var lblDue = FindControlByName<Label>(this, "lblDueDate");
            if (lblDue != null) lblDue.Text = a.DueDate.ToString("yyyy년 MM월 dd일 HH:mm");

            UpdateStatusBadge(a.Status);
            UpdateTimeRemaining(a.DueDate);
        }

        private static T FindControlByName<T>(Control root, string name) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c is T t && c.Name == name) return t;
                var found = FindControlByName<T>(c, name);
                if (found != null) return found;
            }
            return null;
        }

        private void UpdateStatusBadge(string status)
        {
            _lblStatusBadge.Text = status;
            _lblStatusBadge.BackColor =
                status == "제출 완료" ? Theme.Success :
                status != null && status.Contains("PR 제출됨") ? Color.FromArgb(66, 133, 244) :
                Color.FromArgb(255, 152, 0);
        }

        private void UpdateTimeRemaining(DateTime dueDate)
        {
            var remaining = dueDate - DateTime.Now;
            if (remaining.TotalHours < 0)
            {
                _lblTimeRemaining.Text = "⏰ 마감됨";
                _lblTimeRemaining.ForeColor = Color.FromArgb(244, 67, 54);
            }
            else if (remaining.TotalHours < 1)
            {
                _lblTimeRemaining.Text = $"⚠ {(int)remaining.TotalMinutes}분 남음";
                _lblTimeRemaining.ForeColor = Color.FromArgb(244, 67, 54);
            }
            else if (remaining.TotalHours < 24)
            {
                _lblTimeRemaining.Text = $"⏰ {(int)remaining.TotalHours}시간 {remaining.Minutes}분 남음";
                _lblTimeRemaining.ForeColor = Color.FromArgb(255, 152, 0);
            }
            else
            {
                _lblTimeRemaining.Text = $"📅 {(int)remaining.TotalDays}일 {remaining.Hours}시간 남음";
                _lblTimeRemaining.ForeColor = Theme.FgMuted;
            }
        }

        // ─────────────────────────────────────────────────────
        // Event handlers
        // ─────────────────────────────────────────────────────
        private void BtnFileSelect_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "제출할 파일 선택" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _txtFilePath.Text = dlg.FileName;
                var fi = new FileInfo(dlg.FileName);
                _lblFileInfo.Text = $"✓ {fi.Name}  ({FormatSize(fi.Length)})";
                _lblFileInfo.ForeColor = Theme.Success;
            }
        }

        private async void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtFilePath.Text))
            {
                MessageBox.Show("제출할 파일을 먼저 선택해주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_assignment == null)
            {
                MessageBox.Show("프로젝트 정보가 로드되지 않았습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string localPath = _txtFilePath.Text;
            string note = _txtSubmissionNote.Text?.Trim();

            _btnSubmit.Enabled = false;
            _btnFileSelect.Enabled = false;
            _prgSubmitting.Visible = true;

            try
            {
                if (!_fileService.SubmitFile(localPath, _assignment.Id))
                    throw new InvalidOperationException("파일 복사에 실패했습니다.");

                if (!string.IsNullOrEmpty(note))
                {
                    try { _fileService.SaveSubmissionNote(_assignment.Id, Path.GetFileName(localPath), note); } catch { }
                }

                string commitSha = await _gitHubService.CommitAndPushToClassroom(_assignment.Id, localPath);
                string submissionUrl = await _gitHubService.GetSubmissionUrl(_assignment.Id);

                if (!_assignmentService.UpdateAssignmentStatus(_assignment.Id, "제출 완료"))
                {
                    MessageBox.Show("상태 업데이트에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    var user = AuthService.CurrentUser;
                    var name = user != null && !string.IsNullOrWhiteSpace(user.Name) ? user.Name : user?.Username ?? "시스템";
                    NotificationService.Create(user.Id, "프로젝트 제출 완료",
                        $"{name}님이 '{_assignment.Title}' 프로젝트를 제출했습니다.", "Assignment", _assignment.Id);
                }
                catch { }

                (FindForm() as MainForm)?.UpdateNotificationBadge();

                var answer = MessageBox.Show(
                    $"제출이 완료되었습니다!\n\nCommit: {commitSha.Substring(0, 7)}\nGitHub 저장소로 이동하시겠습니까?",
                    "성공", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (answer == DialogResult.Yes)
                    try { System.Diagnostics.Process.Start(submissionUrl); } catch { }

                (FindForm() as MainForm)?.GoBack();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"제출 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnSubmit.Enabled = true;
                _btnFileSelect.Enabled = true;
                _prgSubmitting.Visible = false;
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
