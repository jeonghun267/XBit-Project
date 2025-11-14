// XBit/Pages/PageBoard.cs (최종 수정본 - 버튼 너비 수정 및 DataGrid 스타일 보정)

using System;
using System.Drawing;
using System.Windows.Forms;
using XBit.Services;
using XBit.Models;
using System.Linq;

namespace XBit.Pages
{
    public class PageBoard : UserControl
    {
        private DataGridView grid;
        private BoardService _boardService = new BoardService();
        private Button btnNewPost;

        public PageBoard()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            // 1. UI 컨트롤 초기화
            btnNewPost = new Button
            {
                Text = "새 글 작성",
                Height = 40,
                Width = 120, // ⭐️ 버튼 너비를 명확하게 지정
                Margin = new Padding(10)
            };
            btnNewPost.Click += BtnNewPost_Click;

            Theme.StylePrimaryButton(btnNewPost);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                BorderStyle = BorderStyle.None,

                // ⭐️ DataGridView 행 높이를 내용에 맞게 자동 조절 설정
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            };

            // DataGridView 컬럼 설정
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "제목", DataPropertyName = "Title", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "작성자", DataPropertyName = "AuthorName", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "작성일", DataPropertyName = "CreatedDate", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });

            grid.CellDoubleClick += Grid_CellDoubleClick;

            this.VisibleChanged += PageBoard_VisibleChanged;

            // 2. TableLayoutPanel을 사용하여 레이아웃 재구성
            var layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var pnlButtonContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Controls = { btnNewPost }
            };

            // 3. 컨트롤 배치
            layoutPanel.Controls.Add(pnlButtonContainer, 0, 0);
            layoutPanel.Controls.Add(grid, 0, 1);

            this.Controls.Add(layoutPanel);

            LoadPosts();
        }

        private void PageBoard_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                LoadPosts();
            }
        }

        private void LoadPosts()
        {
            var posts = _boardService.GetAllPosts();
            grid.DataSource = posts;
        }

        private void BtnNewPost_Click(object sender, EventArgs e)
        {
            var mainForm = FindForm() as MainForm;
            if (mainForm != null)
            {
                mainForm.NavigateTo<PagePostDetail>(-1);
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var selectedPost = grid.Rows[e.RowIndex].DataBoundItem as Post;

                if (selectedPost != null)
                {
                    var mainForm = FindForm() as MainForm;
                    if (mainForm != null)
                    {
                        mainForm.NavigateTo<PagePostDetail>(selectedPost.Id);
                    }
                }
            }
        }
    }
}