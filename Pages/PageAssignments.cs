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
        private readonly AssignmentService _assignmentService = new AssignmentService();
        private List<Assignment> _allAssignments = new List<Assignment>();
        private FlowLayoutPanel _cardList;
        private TextBox _txtSearch;
        private string _searchQuery = "";
        private string _currentFilter = "ALL";
        private Panel _filterBar;

        public PageAssignments()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;
            BuildLayout();
            LoadAssignments();
            this.VisibleChanged += (s, e) => { if (this.Visible) LoadAssignments(); };
            Theme.ThemeChanged += () =>
            {
                BackColor = Theme.BgMain;
                Theme.Apply(this);
                RefreshFilterTabs();
                RenderCards();
            };
        }

        private void BuildLayout()
        {
            // ── Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.BgMain
            };
            var lblTitle = new Label
            {
                Text = "프로젝트",
                Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(20, 16)
            };
            _txtSearch = new TextBox
            {
                Width = 200,
                Height = 30,
                Font = new Font("맑은 고딕", 10f),
                BackColor = Theme.BgCard,
                ForeColor = Theme.FgDefault,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.SetPlaceholder("검색...");
            _txtSearch.TextChanged += (s, e) => { _searchQuery = _txtSearch.GetActualText(); RenderCards(); };

            var btnAdd = new Button { Text = "+ 프로젝트 생성", Width = 130, Height = 34 };
            Theme.StylePrimaryButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(_txtSearch);
            pnlHeader.Controls.Add(btnAdd);
            pnlHeader.Resize += (s, e) =>
            {
                btnAdd.Location = new Point(pnlHeader.Width - btnAdd.Width - 20, 15);
                _txtSearch.Location = new Point(btnAdd.Left - _txtSearch.Width - 10, 17);
            };

            // ── Filter tab bar
            _filterBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Theme.BgMain };
            var filters = new[] { ("전체", "ALL"), ("진행중", "IN_PROGRESS"), ("완료", "COMPLETED"), ("마감임박", "DUE_SOON") };
            int fx = 20;
            foreach (var (lbl, key) in filters)
            {
                var k = key;
                var btn = new Button
                {
                    Text = lbl,
                    Width = 82,
                    Height = 30,
                    Location = new Point(fx, 8),
                    Tag = "filter-" + k,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("맑은 고딕", 9.5f),
                    Cursor = Cursors.Hand
                };
                ApplyTabStyle(btn, k == _currentFilter);
                btn.Click += (s, e) => { _currentFilter = k; RefreshFilterTabs(); RenderCards(); };
                _filterBar.Controls.Add(btn);
                fx += 90;
            }

            // ── Divider + card list
            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };
            _cardList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(20, 12, 20, 20)
            };
            Theme.EnableDoubleBuffer(_cardList);

            Controls.Add(_cardList);
            Controls.Add(divider);
            Controls.Add(_filterBar);
            Controls.Add(pnlHeader);
        }

        private void ApplyTabStyle(Button btn, bool active)
        {
            btn.FlatAppearance.BorderSize = 1;
            if (active)
            {
                btn.BackColor = Theme.Primary;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderColor = Theme.Primary;
            }
            else
            {
                btn.BackColor = Theme.BgCard;
                btn.ForeColor = Theme.FgMuted;
                btn.FlatAppearance.BorderColor = Theme.Border;
            }
        }

        private void RefreshFilterTabs()
        {
            foreach (Control c in _filterBar.Controls)
                if (c is Button b && (b.Tag as string ?? "").StartsWith("filter-"))
                    ApplyTabStyle(b, b.Tag.ToString().Substring(7) == _currentFilter);
        }

        private void LoadAssignments()
        {
            try
            {
                _allAssignments = _assignmentService.GetAssignmentsForUser(AuthService.CurrentUser.Id) ?? new List<Assignment>();
                RenderCards();
            }
            catch { }
        }

        private void RenderCards()
        {
            _cardList.SuspendLayout();
            _cardList.Controls.Clear();

            var filtered = ApplyFilters().ToList();
            if (filtered.Count == 0)
            {
                _cardList.Controls.Add(new Label
                {
                    Text = "해당하는 프로젝트가 없습니다.",
                    Font = new Font("맑은 고딕", 11f),
                    ForeColor = Theme.FgMuted,
                    AutoSize = true,
                    Padding = new Padding(0, 20, 0, 0)
                });
            }
            else
            {
                foreach (var a in filtered)
                    _cardList.Controls.Add(MakeCard(a));
            }
            _cardList.ResumeLayout();
        }

        private IEnumerable<Assignment> ApplyFilters()
        {
            var now = DateTime.Now;
            IEnumerable<Assignment> list = _allAssignments;

            switch (_currentFilter)
            {
                case "IN_PROGRESS":
                    list = list.Where(a => a.Status != "제출 완료" && a.DueDate > now);
                    break;
                case "COMPLETED":
                    list = list.Where(a => a.Status == "제출 완료");
                    break;
                case "DUE_SOON":
                    list = list.Where(a => a.Status != "제출 완료" && (a.DueDate - now).TotalHours <= 48 && a.DueDate > now);
                    break;
            }

            if (!string.IsNullOrEmpty(_searchQuery))
                list = list.Where(a =>
                    (a.Title ?? "").IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (a.Course ?? "").IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);

            return list.OrderBy(a => a.DueDate);
        }

        private Panel MakeCard(Assignment a)
        {
            var now = DateTime.Now;
            var remaining = a.DueDate - now;
            bool isOverdue = remaining.TotalHours < 0;
            bool isDueSoon = !isOverdue && remaining.TotalHours <= 24;
            bool isDone = a.Status == "제출 완료";

            var card = new Panel
            {
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(20, 14, 20, 14),
                Cursor = Cursors.Hand,
                Tag = a
            };
            card.Width = _cardList.ClientSize.Width - 40;
            Theme.StyleCard(card);

            // Course badge (top-left)
            var courseBadge = new Label
            {
                Text = a.Course ?? "과목",
                Font = new Font("맑은 고딕", 8f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                AutoSize = true,
                Location = new Point(0, 0),
                Tag = "no-theme"
            };

            // Title
            var lblTitle = new Label
            {
                Text = a.Title,
                Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(0, 20),
                MaximumSize = new Size(card.Width - 120, 0)
            };

            // Status badge (top-right)
            Color statusBg = isDone ? Theme.Success
                           : isDueSoon ? Color.FromArgb(255, 152, 0)
                           : isOverdue ? Color.FromArgb(244, 67, 54)
                           : Color.FromArgb(90, 90, 105);
            string statusText = isDone ? "제출완료" : isOverdue ? "기한만료" : a.Status ?? "미제출";

            var lblStatus = new Label
            {
                Text = statusText,
                Font = new Font("맑은 고딕", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = statusBg,
                AutoSize = false,
                Width = 70,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "no-theme"
            };

            // Due date + remaining chips
            string remText = isOverdue ? "마감됨"
                           : remaining.TotalHours < 24 ? $"⚡ {(int)remaining.TotalHours}h {remaining.Minutes}m 남음"
                           : $"📅 {(int)remaining.TotalDays}일 {remaining.Hours}h 남음";
            Color remColor = isOverdue ? Color.FromArgb(244, 67, 54)
                           : isDueSoon ? Color.FromArgb(255, 152, 0)
                           : Theme.FgMuted;

            var pnlMeta = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Location = new Point(0, 50),
                Padding = new Padding(0)
            };
            pnlMeta.Controls.Add(MakeChip("🗓 " + a.DueDate.ToString("MM/dd HH:mm"), Theme.FgMuted));
            var remChip = MakeChip(remText, remColor);
            remChip.Tag = "no-theme";
            pnlMeta.Controls.Add(remChip);

            card.Height = 88;
            card.Controls.Add(courseBadge);
            card.Controls.Add(lblTitle);
            card.Controls.Add(pnlMeta);
            card.Controls.Add(lblStatus);

            // Position status badge top-right
            void PlaceBadge() => lblStatus.Location = new Point(card.Width - lblStatus.Width - 40, 16);
            PlaceBadge();
            card.Resize += (s, e) => { PlaceBadge(); lblTitle.MaximumSize = new Size(card.Width - 120, 0); };

            EventHandler click = (s, e) => (FindForm() as MainForm)?.NavigateTo<PageAssignmentDetail>(a.Id);
            card.Click += click;
            foreach (Control c in card.Controls) { c.Click += click; c.Cursor = Cursors.Hand; }

            card.MouseEnter += (s, e) => card.BackColor = Theme.Hover;
            card.MouseLeave += (s, e) => card.BackColor = Theme.BgCard;

            _cardList.Resize += (s, e) => card.Width = _cardList.ClientSize.Width - 40;

            return card;
        }

        private Label MakeChip(string text, Color color) => new Label
        {
            Text = text,
            Font = new Font("맑은 고딕", 8.5f),
            ForeColor = color,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 0)
        };

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new AssignmentAddDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                bool success = _assignmentService.AddAssignment(dialog.Course, dialog.Title, dialog.DueDate, AuthService.CurrentUser.Id);
                if (success)
                {
                    try
                    {
                        var user = AuthService.CurrentUser;
                        var name = user != null && !string.IsNullOrWhiteSpace(user.Name) ? user.Name : user?.Username ?? "시스템";
                        NotificationService.Create(user.Id, "새 프로젝트가 생성되었습니다",
                            $"{name}님이 '{dialog.Title}' 프로젝트를 추가했습니다.", "Assignment", null);
                    }
                    catch { }
                    LoadAssignments();
                }
                else
                {
                    MessageBox.Show("프로젝트 추가 실패!", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Kept for external callers (MainForm/PageHome quick filter shortcuts)
        public void FilterData(string filter)
        {
            switch (filter)
            {
                case "DueToday": case "DUE_SOON": _currentFilter = "DUE_SOON"; break;
                case "DueThisWeek": case "WEEKLY": _currentFilter = "IN_PROGRESS"; break;
                case "COMPLETED": _currentFilter = "COMPLETED"; break;
                default: _currentFilter = "ALL"; break;
            }
            RefreshFilterTabs();
            if (_allAssignments == null) LoadAssignments(); else RenderCards();
        }
    }

    // ── Placeholder TextBox extensions (used across pages) ───────────────────
    static class TextBoxExtensions
    {
        private const string PlaceholderKey = "_placeholder";
        private const string IsPlaceholderKey = "_isPlaceholder";

        public static void SetPlaceholder(this TextBox tb, string placeholder)
        {
            if (tb == null) return;
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
                return "";
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

// ── AssignmentAddDialog ───────────────────────────────────────────────────────
namespace XBit.Dialogs
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

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
            this.Text = "프로젝트 추가";
            this.Size = new Size(460, 290);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;

            var lblCourse = MakeLabel("과목", 20);
            txtCourse = MakeTextBox(45);

            var lblTitle = MakeLabel("제목", 85);
            txtTitle = MakeTextBox(110);

            var lblDueDate = MakeLabel("마감일", 148);
            dtpDueDate = new DateTimePicker
            {
                Location = new Point(20, 170),
                Width = 400,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm",
                ShowUpDown = false,
                Value = DateTime.Now.AddDays(7),
                BackColor = Theme.BgCard,
                ForeColor = Theme.FgDefault
            };

            btnOk = new Button { Text = "확인", Location = new Point(230, 215), Width = 90, Height = 32, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "취소", Location = new Point(330, 215), Width = 90, Height = 32, DialogResult = DialogResult.Cancel };

            Theme.StylePrimaryButton(btnOk);
            Theme.StyleButton(btnCancel);

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCourse.Text)) { Warn("과목을 입력하세요.", txtCourse); return; }
                if (string.IsNullOrWhiteSpace(txtTitle.Text)) { Warn("제목을 입력하세요.", txtTitle); return; }
                if (dtpDueDate.Value <= DateTime.Now) { Warn("마감일은 현재 시간 이후여야 합니다.", dtpDueDate); return; }
            };

            this.Controls.AddRange(new Control[] { lblCourse, txtCourse, lblTitle, txtTitle, lblDueDate, dtpDueDate, btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private Label MakeLabel(string text, int y) => new Label
        {
            Text = text,
            Location = new Point(20, y),
            AutoSize = true,
            Font = new Font("맑은 고딕", 9f),
            ForeColor = Theme.FgMuted,
            BackColor = System.Drawing.Color.Transparent
        };

        private TextBox MakeTextBox(int y) => new TextBox
        {
            Location = new Point(20, y),
            Width = 400,
            Font = new Font("맑은 고딕", 10f),
            BackColor = Theme.BgCard,
            ForeColor = Theme.FgDefault,
            BorderStyle = BorderStyle.FixedSingle
        };

        private void Warn(string msg, Control focus)
        {
            MessageBox.Show(msg, "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            focus.Focus();
            this.DialogResult = DialogResult.None;
        }
    }
}
