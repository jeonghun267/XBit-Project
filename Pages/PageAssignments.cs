// Pages/PageAssignments.cs (검색창 위치 수정 버전)

using System;
using System.Drawing;
using System.Windows.Forms;
using XBit.Services;
using XBit.Models;
using System.Collections.Generic;
using System.Linq;
using XBit.Dialogs;

namespace XBit.Pages
{
    public class PageAssignments : UserControl
    {
        private DataGridView grid;
        private AssignmentService _assignmentService = new AssignmentService();
        private List<Assignment> allAssignments;

        // UI controls
        private Button btnAddAssignment;
        private Button btnRefresh;
        private TextBox txtSearch;

        // 현재 필터 상태(문자열 기반)
        private string currentFilter = "ALL";

        public PageAssignments()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            LoadAllAssignments();
            InitializeLayout();
            FilterData("ALL");
        }

        private void InitializeLayout()
        {
            // Header & controls
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.BgMain,
                Padding = new Padding(12)
            };

            var lblTitle = new Label
            {
                Text = "[프로젝트 목록]",
                Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(12, 16)
            };

            // Search box (✅ 위치 왼쪽으로 이동: 250px)
            txtSearch = new TextBox
            {   
                Width = 220,
                Height = 24,
                Location = new Point(250, 20),
                BackColor = Theme.BgCard,
                ForeColor = Theme.FgDefault
            };
            txtSearch.SetPlaceholder("검색: 제목");
            txtSearch.TextChanged += (s, e) => ApplySearch();

            // 새로고침 버튼 (✅ 위치 조정: 480px)
            btnRefresh = new Button
            {
                Text = "새로고침",
                Width = 90,
                Height = 36,
                Location = new Point(480, 14)
            };
            Theme.StyleButton(btnRefresh);
            btnRefresh.Click += (s, e) => { LoadAllAssignments(); FilterData(currentFilter); };

            // 추가 버튼
            btnAddAssignment = new Button
            {
                Text = "+ 프로젝트 생성",
                Width = 140,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Theme.StylePrimaryButton(btnAddAssignment);
            btnAddAssignment.Click += BtnAddAssignment_Click;

            // 위치 계산 on resize (✅ 동적 배치 개선)
            pnlHeader.Resize += (s, e) =>
            {
                btnAddAssignment.Location = new Point(pnlHeader.Width - btnAddAssignment.Width - 12, 14);
                btnRefresh.Location = new Point(pnlHeader.Width - btnAddAssignment.Width - btnRefresh.Width - 24, 14);
                txtSearch.Location = new Point(pnlHeader.Width - btnAddAssignment.Width - btnRefresh.Width - txtSearch.Width - 50, 20);
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(txtSearch);
            pnlHeader.Controls.Add(btnRefresh);
            pnlHeader.Controls.Add(btnAddAssignment);

            // Grid
            InitializeGrid();

            Controls.Add(grid);
            Controls.Add(pnlHeader);
        }

        private void InitializeGrid()
        {
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowTemplate = { Height = 40 }
            };

            grid.Columns.Clear();

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "과목",
                DataPropertyName = "Course",
                Width = 160
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "제목",
                DataPropertyName = "Title",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "마감일",
                DataPropertyName = "DueDate",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "상태",
                DataPropertyName = "Status",
                Width = 100
            });

            grid.CellDoubleClick += Grid_CellDoubleClick;
            grid.CellFormatting += Grid_CellFormatting;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].HeaderText == "상태")
            {
                string status = e.Value?.ToString();
                if (status == "미제출")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(244, 67, 54);
                    e.CellStyle.Font = new Font(grid.Font, FontStyle.Bold);
                }
                else if (status == "제출" || status == "제출 완료")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(76, 175, 80);
                }
                else
                {
                    e.CellStyle.ForeColor = Theme.FgDefault;
                }
            }
        }

        private void LoadAllAssignments()
        {
            System.Diagnostics.Debug.WriteLine($"[PageAssignments] 현재 사용자 ID: {AuthService.CurrentUser.Id}");

            allAssignments = _assignment_service_safe();
            System.Diagnostics.Debug.WriteLine($"[PageAssignments] 가져온 프로젝트 수: {allAssignments?.Count ?? 0}");
        }

        // 안전하게 서비스 호출 (예외 방지/로그)
        private List<Assignment> _assignment_service_safe()
        {
            try
            {
                return _assignmentService.GetAssignmentsForUser(AuthService.CurrentUser.Id) ?? new List<Assignment>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PageAssignments] GetAssignmentsForUser 예외: {ex.Message}");
                return new List<Assignment>();
            }
        }

        public void FilterData(string filter)
        {
            currentFilter = filter ?? "ALL";

            if (allAssignments == null) LoadAllAssignments();

            IEnumerable<Assignment> filteredList = allAssignments;
            DateTime now = DateTime.Now;

            System.Diagnostics.Debug.WriteLine($"[PageAssignments] FilterData 호출: {filter}");

            switch (filter)
            {
                case "DueToday":
                case "DUE_SOON":
                    filteredList = allAssignments.Where(a =>
                        (a.DueDate - now).TotalHours <= 24 &&
                        (a.DueDate - now).TotalHours > 0 &&
                        a.Status != "제출 완료"
                    );
                    break;

                case "DueThisWeek":
                case "WEEKLY":
                    DateTime startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    DateTime endOfWeek = startOfWeek.AddDays(7);
                    filteredList = allAssignments.Where(a => a.DueDate >= startOfWeek && a.DueDate < endOfWeek);
                    break;

                case "ALL":
                default:
                    filteredList = allAssignments;
                    break;
            }

            // 검색 텍스트 적용
            var q = txtSearch.GetActualText();
            if (!string.IsNullOrEmpty(q))
            {
                filteredList = filteredList.Where(a => (a.Title ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            grid.DataSource = filteredList.ToList();
            grid.Refresh();
        }

        private void ApplySearch()
        {
            // 실시간 검색: 현재 필터 유지
            FilterData(currentFilter);
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var selectedAssignment = grid.Rows[e.RowIndex].DataBoundItem as Assignment;

                if (selectedAssignment != null)
                {
                    var mainForm = FindForm() as MainForm;
                    mainForm?.NavigateTo<PageAssignmentDetail>(selectedAssignment.Id);
                }
            }
        }

        private void BtnAddAssignment_Click(object sender, EventArgs e)
        {
            using (var dialog = new AssignmentAddDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    bool success = _assignmentService.AddAssignment(
                        dialog.Course,
                        dialog.Title,
                        dialog.DueDate,
                        AuthService.CurrentUser.Id
                    );

                    if (success)
                    {
                        MessageBox.Show("프로젝트가 추가되었습니다!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadAllAssignments();
                        FilterData("ALL");
                    }
                    else
                    {
                        MessageBox.Show("프로젝트 추가 실패!", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    // ✅ 개선된 PlaceholderText 확장 (겹침 문제 해결)
    static class TextBoxExtensions
    {
        private const string PlaceholderKey = "_placeholder";
        private const string IsPlaceholderKey = "_isPlaceholder";

        public static void SetPlaceholder(this TextBox tb, string placeholder)
        {
            if (tb == null) return;

            // Tag에 placeholder 저장
            tb.Tag = new Dictionary<string, object>
            {
                { PlaceholderKey, placeholder },
                { IsPlaceholderKey, true }
            };

            tb.Text = placeholder;
            tb.ForeColor = SystemColors.GrayText;

            tb.GotFocus -= OnGotFocus;
            tb.LostFocus -= OnLostFocus;
            tb.GotFocus += OnGotFocus;
            tb.LostFocus += OnLostFocus;
        }

        public static string GetActualText(this TextBox tb)
        {
            if (tb == null) return "";
            
            var dict = tb.Tag as Dictionary<string, object>;
            if (dict != null && dict.ContainsKey(IsPlaceholderKey) && (bool)dict[IsPlaceholderKey])
            {
                return "";
            }
            
            return tb.Text?.Trim() ?? "";
        }

        private static void OnGotFocus(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            var dict = tb.Tag as Dictionary<string, object>;
            if (dict != null && dict.ContainsKey(IsPlaceholderKey) && (bool)dict[IsPlaceholderKey])
            {
                tb.Text = "";
                tb.ForeColor = Theme.FgDefault;
                dict[IsPlaceholderKey] = false;
            }
        }

        private static void OnLostFocus(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            var dict = tb.Tag as Dictionary<string, object>;
            if (dict != null && string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = dict[PlaceholderKey] as string;
                tb.ForeColor = SystemColors.GrayText;
                dict[IsPlaceholderKey] = true;
            }
        }
    }
}

// Dialogs/AssignmentAddDialog.cs (프로젝트 상세 및 추가 대화 상자 개선)

namespace XBit.Dialogs
{
    public class AssignmentAddDialog : Form
    {
        private TextBox txtCourse;
        private TextBox txtTitle;
        private DateTimePicker dtpDueDate;
        private Button btnOk;
        private Button btnCancel;

        public string Course => txtCourse.Text.Trim();
        public string Title => txtTitle.Text.Trim();
        public DateTime DueDate => dtpDueDate.Value;

        public AssignmentAddDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "프로젝트 추가";
            this.Size = new Size(450, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // Course Label
            var lblCourse = new Label
            {
                Text = "과목:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9f)
            };

            txtCourse = new TextBox
            {
                Location = new Point(20, 45),
                Width = 390,
                Font = new Font("맑은 고딕", 10f)
            };

            // Title Label
            var lblTitle = new Label
            {
                Text = "제목:",
                Location = new Point(20, 80),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9f)
            };

            txtTitle = new TextBox
            {
                Location = new Point(20, 105),
                Width = 390,
                Font = new Font("맑은 고딕", 10f)
            };

            // DueDate Label
            var lblDueDate = new Label
            {
                Text = "마감일:",
                Location = new Point(20, 140),
                AutoSize = true,
                Font = new Font("맑은 고딕", 9f)
            };

            dtpDueDate = new DateTimePicker
            {
                Location = new Point(20, 165),
                Width = 390,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                ShowUpDown = false,
                Value = DateTime.Now.AddDays(7)
            };

            // OK Button
            btnOk = new Button
            {
                Text = "확인",
                Location = new Point(230, 205),
                Width = 90,
                Height = 32,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += BtnOk_Click;

            // Cancel Button
            btnCancel = new Button
            {
                Text = "취소",
                Location = new Point(330, 205),
                Width = 90,
                Height = 32,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblCourse);
            this.Controls.Add(txtCourse);
            this.Controls.Add(lblTitle);
            this.Controls.Add(txtTitle);
            this.Controls.Add(lblDueDate);
            this.Controls.Add(dtpDueDate);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCourse.Text))
            {
                MessageBox.Show("과목을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCourse.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("제목을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            if (dtpDueDate.Value <= DateTime.Now)
            {
                MessageBox.Show("마감일은 현재 시간 이후여야 합니다.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpDueDate.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }
        }
    }
}