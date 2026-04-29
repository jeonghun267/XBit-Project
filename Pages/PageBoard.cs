using System;
using System.Drawing;
using System.Windows.Forms;
using XBit.Services;
using XBit.Models;
using System.Linq;
using System.Collections.Generic;

namespace XBit.Pages
{
    public class PageBoard : UserControl
    {
        private readonly BoardService _boardService = new BoardService();
        private List<Post> _allPosts = new List<Post>();
        private FlowLayoutPanel _postList;
        private TextBox _txtSearch;
        private string _searchQuery = "";

        public PageBoard()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            BuildLayout();
            LoadPosts();

            this.VisibleChanged += (s, e) => { if (this.Visible) LoadPosts(); };
            Theme.ThemeChanged += () => { BackColor = Theme.BgMain; Theme.Apply(this); RenderPosts(); };
        }

        // ───────────────────────────────────────
        // 레이아웃
        // ───────────────────────────────────────
        private void BuildLayout()
        {
            // ── 상단 바
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.BgMain,
                Padding = new Padding(20, 12, 20, 0)
            };

            var lblTitle = new Label
            {
                Text = "게시판",
                Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(20, 16)
            };

            // 검색창
            _txtSearch = new TextBox
            {
                Width = 220,
                Height = 30,
                Font = new Font("맑은 고딕", 10f),
                BackColor = Theme.BgCard,
                ForeColor = Theme.FgDefault,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.SetPlaceholder("검색...");
            _txtSearch.TextChanged += (s, e) =>
            {
                _searchQuery = _txtSearch.GetActualText();
                RenderPosts();
            };

            var btnNew = new Button { Text = "새 글 작성", Width = 110, Height = 34 };
            Theme.StylePrimaryButton(btnNew);
            btnNew.Click += (s, e) => (FindForm() as MainForm)?.NavigateTo<PagePostDetail>(-1);

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(_txtSearch);
            pnlTop.Controls.Add(btnNew);

            pnlTop.Resize += (s, e) =>
            {
                btnNew.Location = new Point(pnlTop.Width - btnNew.Width - 20, 15);
                _txtSearch.Location = new Point(btnNew.Left - _txtSearch.Width - 10, 17);
            };

            // ── 구분선
            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };

            // ── 포스트 목록 (스크롤)
            _postList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(20, 12, 20, 20)
            };
            Theme.EnableDoubleBuffer(_postList);

            Controls.Add(_postList);
            Controls.Add(divider);
            Controls.Add(pnlTop);
        }

        // ───────────────────────────────────────
        // 데이터 로드 & 렌더링
        // ───────────────────────────────────────
        private void LoadPosts()
        {
            try
            {
                _allPosts = _boardService.GetAllPosts() ?? new List<Post>();
                RenderPosts();
            }
            catch { }
        }

        private void RenderPosts()
        {
            _postList.SuspendLayout();
            _postList.Controls.Clear();

            var filtered = string.IsNullOrEmpty(_searchQuery)
                ? _allPosts
                : _allPosts.Where(p =>
                    (p.Title ?? "").IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (p.AuthorName ?? "").IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0
                  ).ToList();

            if (filtered.Count == 0)
            {
                _postList.Controls.Add(new Label
                {
                    Text = "게시글이 없습니다.",
                    Font = new Font("맑은 고딕", 11f),
                    ForeColor = Theme.FgMuted,
                    AutoSize = true,
                    Padding = new Padding(0, 20, 0, 0)
                });
            }
            else
            {
                foreach (var post in filtered.OrderByDescending(p => p.CreatedDate))
                    _postList.Controls.Add(MakePostCard(post));
            }

            _postList.ResumeLayout();
        }

        // ───────────────────────────────────────
        // 포스트 카드
        // ───────────────────────────────────────
        private Panel MakePostCard(Post post)
        {
            var card = new Panel
            {
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(20, 14, 20, 14),
                Cursor = Cursors.Hand,
                Tag = post
            };
            card.Width = _postList.ClientSize.Width - 40;
            Theme.StyleCard(card);

            // 제목
            var lblTitle = new Label
            {
                Text = post.Title,
                Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Location = new Point(0, 0),
                MaximumSize = new Size(card.Width - 160, 0)
            };

            // 내용 미리보기
            string preview = (post.Content ?? "").Replace("\r", "").Replace("\n", " ");
            if (preview.Length > 120) preview = preview.Substring(0, 120) + "...";
            var lblPreview = new Label
            {
                Text = preview,
                Font = new Font("맑은 고딕", 9.5f),
                ForeColor = Theme.FgMuted,
                AutoSize = false,
                Width = card.Width - 40,
                Height = 38,
                Location = new Point(0, 28),
                Tag = "muted"
            };

            // 하단 메타
            var pnlMeta = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Location = new Point(0, 72),
                Padding = new Padding(0)
            };

            pnlMeta.Controls.Add(MakeMetaChip("✍ " + (post.AuthorName ?? "익명"), Theme.FgMuted));
            pnlMeta.Controls.Add(MakeMetaChip("🕐 " + post.CreatedDate.ToString("MM/dd HH:mm"), Theme.FgMuted));
            if (post.CommentCount > 0)
                pnlMeta.Controls.Add(MakeMetaChip($"💬 {post.CommentCount}", Theme.Primary));

            card.Height = 108;
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblPreview);
            card.Controls.Add(pnlMeta);

            // 호버 효과 + 클릭
            EventHandler click = (s, e) => (FindForm() as MainForm)?.NavigateTo<PagePostDetail>(post.Id);
            card.Click += click;
            foreach (Control c in card.Controls) { c.Click += click; c.Cursor = Cursors.Hand; }

            card.MouseEnter += (s, e) => card.BackColor = Theme.Hover;
            card.MouseLeave += (s, e) => card.BackColor = Theme.BgCard;

            // 카드 너비 반응형
            _postList.Resize += (s, e) =>
            {
                int w = _postList.ClientSize.Width - 40;
                card.Width = w;
                lblPreview.Width = w - 40;
                lblTitle.MaximumSize = new Size(w - 160, 0);
            };

            return card;
        }

        private Label MakeMetaChip(string text, Color color)
        {
            return new Label
            {
                Text = text,
                Font = new Font("맑은 고딕", 8.5f),
                ForeColor = color,
                AutoSize = true,
                Margin = new Padding(0, 0, 14, 0),
                Tag = "no-theme"
            };
        }
    }
}
