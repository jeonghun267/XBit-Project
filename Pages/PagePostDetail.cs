// Pages/PagePostDetail.cs (모든 오류 수정 완료)

using System;
using System.Windows.Forms;
using System.Drawing;
using XBit.Models;
using XBit.Services;
using System.Collections.Generic;
using System.Linq;

namespace XBit.Pages
{
    public class PagePostDetail : UserControl
    {
        private BoardService _boardService = new BoardService();
        private CommentService _commentService = new CommentService();
        private Post currentPost;

        private FlowLayoutPanel pnlComments ;
        private TextBox txtCommentInput;
        private Button btnAddComment;

        private TextBox txtTitle;
        private RichTextBox txtContent;
        private Button btnSave;
        private Button btnCancel;
        private Button btnDelete;

        // ✅ 추가: UI 요소 선언
        private System.Windows.Forms.Label lblModeIndicator;
        private System.Windows.Forms.Label lblTitleCount;
        private System.Windows.Forms.Label lblContentCount;
        private System.Windows.Forms.Label lblSaveStatus;

        // ✅ 추가: 상수 선언
        private const int ContentWidth = 600;
        private const int MAX_TITLE_LENGTH = 100;
        private const int MAX_CONTENT_LENGTH = 5000;

        public PagePostDetail(int postId = -1)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            InitializeUIControls();

            if (postId != -1)
            {
                LoadPost(postId);
            }
            else
            {
                InitializeNewPostMode();
            }

            Theme.Apply(this);
        }

        public PagePostDetail() : this(-1) { }

        private void InitializeNewPostMode()
        {
            currentPost = new Post 
            { 
                AuthorId = AuthService.CurrentUser.Id,
                CreatedDate = DateTime.Now
            };
            
            txtTitle.ReadOnly = false;
            txtTitle.Text = "";
            
            txtContent.ReadOnly = false;
            txtContent.Text = "";
            
            btnSave.Text = "작성 완료";
            btnDelete.Visible = false;
            
            // ✅ 모드 표시
            lblModeIndicator.Text = "📝 새 글 작성";
            lblModeIndicator.ForeColor = Color.FromArgb(66, 133, 244);
            
            // ✅ 글자 수 초기화
            lblTitleCount.Text = "0 / 100";
            lblContentCount.Text = "0 / 5000";
            
            txtTitle.Focus();
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

            // ✅ 모드 표시 라벨
            lblModeIndicator = new System.Windows.Forms.Label
            {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                ForeColor = Theme.FgDefault
            };

            // ✅ 제목 영역 (라벨 + 글자 수)
            var pnlTitleHeader = new FlowLayoutPanel
            {
                Width = ContentWidth,
                Height = 30,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false,
                Margin = new Padding(0, 0, 0, 5)
            };

            var lblTitleLabel = new Label
            {
                Text = "제목",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Theme.FgDefault
            };

            lblTitleCount = new System.Windows.Forms.Label
            {
                Text = "0 / 100",
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                ForeColor = Theme.FgMuted,
                Margin = new Padding(10, 0, 0, 0)
            };

            pnlTitleHeader.Controls.Add(lblTitleLabel);
            pnlTitleHeader.Controls.Add(lblTitleCount);

            // ✅ 제목 입력 필드
            txtTitle = new TextBox 
            { 
                Font = new Font("Segoe UI", 14f, FontStyle.Bold), 
                Height = 35, 
                ForeColor = Theme.FgDefault, 
                BackColor = Theme.BgCard, 
                ReadOnly = false,
                Width = ContentWidth,
                Multiline = false,
                AcceptsReturn = false
            };
            txtTitle.TextChanged += TxtTitle_TextChanged;
            txtTitle.MaxLength = MAX_TITLE_LENGTH;

            // ✅ 내용 영역 (라벨 + 글자 수)
            var pnlContentHeader = new FlowLayoutPanel
            {
                Width = ContentWidth,
                Height = 30,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false,
                Margin = new Padding(0, 15, 0, 5)
            };

            var lblContentLabel = new Label
            {
                Text = "내용",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Theme.FgDefault
            };

            lblContentCount = new System.Windows.Forms.Label
            {
                Text = "0 / 5000",
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                ForeColor = Theme.FgMuted,
                Margin = new Padding(10, 0, 0, 0)
            };

            pnlContentHeader.Controls.Add(lblContentLabel);
            pnlContentHeader.Controls.Add(lblContentCount);

            // ✅ 내용 입력 필드
            txtContent = new RichTextBox 
            { 
                Height = 250, 
                Width = ContentWidth, 
                BackColor = Theme.BgCard, 
                ForeColor = Theme.FgDefault, 
                ReadOnly = false
            };
            txtContent.TextChanged += TxtContent_TextChanged;

            // 버튼 영역
            btnSave = new Button { Text = "작성 완료", Width = 120, Height = 35, Margin = new Padding(5) };
            btnCancel = new Button { Text = "취소", Width = 120, Height = 35, Margin = new Padding(5) };
            btnDelete = new Button { Text = "삭제", Width = 120, Height = 35, Margin = new Padding(5) };

            Theme.StylePrimaryButton(btnSave);
            Theme.StyleButton(btnCancel);
            Theme.StyleDanger(btnDelete);

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            btnDelete.Click += BtnDelete_Click;

            var pnlButtons = new FlowLayoutPanel
            {
                Width = ContentWidth,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Controls = { btnCancel, btnDelete, btnSave },
                Margin = new Padding(0, 10, 0, 0)
            };

            // ✅ 저장 상태 표시
            lblSaveStatus = new System.Windows.Forms.Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 0),
                ForeColor = Theme.FgMuted
            };

            // 댓글 영역
            var lblCommentTitle = new Label 
            { 
                Text = "댓글", 
                AutoSize = true, 
                Margin = new Padding(0, 20, 0, 5), 
                ForeColor = Theme.FgDefault, 
                Font = new Font("Segoe UI", 14f, FontStyle.Bold)
            };

            pnlComments = new FlowLayoutPanel
            {
                Width = ContentWidth,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Margin = new Padding(0, 0, 0, 10)
            };

            layoutPanel.Controls.AddRange(new Control[] {
                lblModeIndicator,
                pnlTitleHeader,
                txtTitle,
                pnlContentHeader,
                txtContent,
                pnlButtons,
                lblSaveStatus,
                lblCommentTitle,
                CreateCommentInputPanel(),
                pnlComments
            });
            this.Controls.Add(layoutPanel);
        }

        // ✅ 제목 글자 수 업데이트
        private void TxtTitle_TextChanged(object sender, EventArgs e)
        {
            int count = txtTitle.Text.Length;
            lblTitleCount.Text = $"{count} / {MAX_TITLE_LENGTH}";

            // 글자 수 초과 시 경고
            if (count > MAX_TITLE_LENGTH)
            {
                lblTitleCount.ForeColor = Color.FromArgb(244, 67, 54); // 빨강
                txtTitle.Text = txtTitle.Text.Substring(0, MAX_TITLE_LENGTH);
                lblTitleCount.Text = $"{MAX_TITLE_LENGTH} / {MAX_TITLE_LENGTH} ⚠️ 초과됨";
            }
            else if (count > MAX_TITLE_LENGTH * 0.8)
            {
                lblTitleCount.ForeColor = Color.FromArgb(255, 152, 0); // 주황
            }
            else
            {
                lblTitleCount.ForeColor = Theme.FgMuted; // 기본색
            }

            UpdateSaveStatus();
        }

        // ✅ 내용 글자 수 업데이트
        private void TxtContent_TextChanged(object sender, EventArgs e)
        {
            int count = txtContent.Text.Length;
            lblContentCount.Text = $"{count} / {MAX_CONTENT_LENGTH}";

            // 글자 수 초과 시 경고
            if (count > MAX_CONTENT_LENGTH)
            {
                lblContentCount.ForeColor = Color.FromArgb(244, 67, 54); // 빨강
                lblContentCount.Text = $"{MAX_CONTENT_LENGTH} / {MAX_CONTENT_LENGTH} ⚠️ 초과됨";
            }
            else if (count > MAX_CONTENT_LENGTH * 0.8)
            {
                lblContentCount.ForeColor = Color.FromArgb(255, 152, 0); // 주황
            }
            else
            {
                lblContentCount.ForeColor = Theme.FgMuted; // 기본색
            }

            UpdateSaveStatus();
        }

        // ✅ 저장 상태 표시
        private void UpdateSaveStatus()
        {
            if (!string.IsNullOrWhiteSpace(txtTitle.Text) && !string.IsNullOrWhiteSpace(txtContent.Text))
            {
                lblSaveStatus.Text = "✓ 저장 가능 상태";
                lblSaveStatus.ForeColor = Color.FromArgb(76, 175, 80);
            }
            else
            {
                lblSaveStatus.Text = "○ 제목과 내용을 입력해주세요";
                lblSaveStatus.ForeColor = Theme.FgMuted;
            }
        }

        private Control CreateCommentInputPanel()
        {
            var pnlInput = new Panel
            {
                Width = ContentWidth,
                Height = 60,
                Margin = new Padding(0, 5, 0, 10)
            };

            txtCommentInput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 60,
                BackColor = Theme.BgCard,
                ForeColor = Theme.FgDefault,
                TabStop = true
            };

            btnAddComment = new Button
            {
                Text = "등록",
                Dock = DockStyle.Right,
                Width = 80,
                Height = 60
            };
            Theme.StylePrimaryButton(btnAddComment);

            btnAddComment.Click += BtnAddComment_Click;

            pnlInput.Controls.Add(btnAddComment);
            pnlInput.Controls.Add(txtCommentInput);
            pnlInput.Controls.SetChildIndex(txtCommentInput, 0);

            return pnlInput;
        }

        private void LoadPost(int postId)
        {
            currentPost = _boardService.GetPostById(postId);

            if (currentPost == null)
            {
                currentPost = new Post
                {
                    Id = postId,
                    Title = $"게시글 상세 ID:{postId}",
                    AuthorId = AuthService.CurrentUser.Id,
                    AuthorName = AuthService.CurrentUser.Name,
                    Content = $"이 게시글은 {postId}번 게시글입니다.",
                    CreatedDate = DateTime.Now
                };
            }

            if (currentPost != null)
            {
                // ✅ 모드 표시 - 기존 글 읽기/수정
                lblModeIndicator.Text = "📖 게시글 보기";
                lblModeIndicator.ForeColor = Theme.FgDefault;

                txtTitle.Text = currentPost.Title;
                txtContent.Text = currentPost.Content;

                // ✅ 글자 수 업데이트
                lblTitleCount.Text = $"{currentPost.Title.Length} / {MAX_TITLE_LENGTH}";
                lblContentCount.Text = $"{currentPost.Content.Length} / {MAX_CONTENT_LENGTH}";

                bool isAuthor = currentPost.AuthorId == AuthService.CurrentUser.Id;

                if (!isAuthor)
                {
                    txtTitle.ReadOnly = true;
                    txtContent.ReadOnly = true;
                    btnSave.Visible = false;
                    btnDelete.Visible = false;
                    lblModeIndicator.Text = "🔒 읽기 전용";
                    lblModeIndicator.ForeColor = Color.FromArgb(244, 67, 54);
                }
                else
                {
                    txtTitle.ReadOnly = false;
                    txtContent.ReadOnly = false;
                    btnSave.Text = "수정 완료";
                    btnDelete.Visible = true;
                    lblModeIndicator.Text = "✏️ 편집 모드";
                    lblModeIndicator.ForeColor = Color.FromArgb(255, 152, 0);
                }

                LoadComments();
            }
        }

        private void LoadComments()
        {
            if (currentPost == null) return;
            pnlComments.Controls.Clear();

            List<Comment> comments = _commentService.GetCommentsByPostId(currentPost.Id);

            if (comments.Count == 0)
            {
                pnlComments.Controls.Add(new Label 
                { 
                    Text = "아직 댓글이 없습니다.", 
                    ForeColor = Theme.FgMuted, 
                    AutoSize = true, 
                    Padding = new Padding(0, 5, 0, 5) 
                });
                return;
            }

            foreach (var comment in comments)
            {
                pnlComments.Controls.Add(CreateCommentItem(comment));
            }
        }

        private Control CreateCommentItem(Comment comment)
        {
            var pnlComment = new Panel
            {
                Width = ContentWidth,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5),
                BackColor = Theme.BgCard,
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Theme.Border
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 100),
                    new ColumnStyle(SizeType.Absolute, 150)
                }
            };

            var lblContent = new Label
            {
                Text = comment.Content,
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                MaximumSize = new Size(ContentWidth - 30, 0),
                Margin = new Padding(0, 0, 0, 5)
            };

            var lblAuthorInfo = new Label
            {
                Text = $"{comment.AuthorName} | {comment.CreatedDate:MM-dd HH:mm}",
                ForeColor = Theme.FgMuted,
                AutoSize = true
            };

            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0)
            };

            int likes = comment.Likes;
            var lblLikes = new Label 
            { 
                Text = $"{likes}개", 
                ForeColor = Theme.FgMuted, 
                AutoSize = true, 
                Margin = new Padding(5, 5, 5, 0) 
            };

            var btnLike = new Button 
            { 
                Text = $"👍 공감", 
                Width = 70, 
                Height = 25, 
                Tag = comment.Id,
                Margin = new Padding(0, 0, 8, 0)
            };
            Theme.StyleButton(btnLike);
            btnLike.Click += BtnLikeComment_Click;

            if (comment.AuthorId == AuthService.CurrentUser.Id)
            {
                var btnDel = new Button 
                { 
                    Text = "삭제", 
                    Width = 50, 
                    Height = 25, 
                    Tag = comment.Id,
                    Margin = new Padding(0, 0, 8, 0)
                };
                Theme.StyleDanger(btnDel);
                btnDel.Font = new Font("Segoe UI", 9f);
                btnDel.Click += BtnDeleteComment_Click;

                pnlActions.Controls.Add(btnDel);
                pnlActions.Controls.SetChildIndex(btnDel, 0);
            }

            pnlActions.Controls.Add(btnLike);
            pnlActions.Controls.SetChildIndex(btnLike, comment.AuthorId == AuthService.CurrentUser.Id ? 1 : 0);
            pnlActions.Controls.Add(lblLikes);

            layout.Controls.Add(lblContent, 0, 0);
            layout.Controls.Add(lblAuthorInfo, 0, 1);
            layout.Controls.Add(pnlActions, 1, 1);
            layout.SetRowSpan(pnlActions, 1);

            pnlComment.Controls.Add(layout);

            return pnlComment;
        }

        private void BtnAddComment_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCommentInput.Text)) return;
            if (currentPost == null) return;

            var newComment = new Comment
            {
                PostId = currentPost.Id,
                Content = txtCommentInput.Text.Trim()
            };

            if (_commentService.AddComment(newComment))
            {
                txtCommentInput.Clear();
                LoadComments();
            }
            else
            {
                MessageBox.Show("댓글 등록에 실패했습니다. (DB 오류)", "오류");
            }
        }

        private void BtnDeleteComment_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || !(btn.Tag is int commentId)) return;

            if (MessageBox.Show("정말로 이 댓글을 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (_commentService.DeleteComment(commentId))
                {
                    LoadComments();
                }
                else
                {
                    MessageBox.Show("댓글 삭제에 실패했습니다. (권한 오류 또는 DB 오류)", "오류");
                }
            }
        }

        private void BtnLikeComment_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || !(btn.Tag is int commentId)) return;

            if (_commentService.IncrementLikes(commentId))
            {
                MessageBox.Show($"댓글 ID {commentId}에 공감했습니다.", "공감 완료");
                LoadComments();
            }
            else
            {
                MessageBox.Show("공감 처리에 실패했습니다. (DB 오류)", "오류");
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtContent.Text))
            {
                MessageBox.Show("제목과 내용을 모두 입력해야 합니다.", "경고");
                return;
            }

            currentPost.Title = txtTitle.Text.Trim();
            currentPost.Content = txtContent.Text.Trim();

            if (currentPost.Id == 0)
            {
                currentPost.AuthorId = AuthService.CurrentUser.Id;
                currentPost.CreatedDate = DateTime.Now;
                
                _boardService.CreatePost(currentPost);
                MessageBox.Show("새 게시글이 작성되었습니다.", "완료");
            }
            else
            {
                if (currentPost.AuthorId != AuthService.CurrentUser.Id)
                {
                    MessageBox.Show("본인이 작성한 게시글만 수정할 수 있습니다.", "권한 오류");
                    return;
                }
                
                _boardService.UpdatePost(currentPost);
                MessageBox.Show("게시글이 수정되었습니다.", "완료");
            }

            ReturnToBoardList();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            ReturnToBoardList();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("정말로 게시글을 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                bool success = _boardService.DeletePost(currentPost.Id);

                if (success)
                {
                    MessageBox.Show("게시글이 삭제되었습니다.", "완료");
                    ReturnToBoardList();
                }
                else
                {
                    MessageBox.Show("게시글 삭제에 실패했습니다. (권한 오류 또는 DB 오류)", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ReturnToBoardList()
        {
            var mainForm = FindForm() as MainForm;
            if (mainForm != null)
            {
                mainForm.GoBack();
            }
        }
    }
}