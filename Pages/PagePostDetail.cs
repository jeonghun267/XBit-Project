// Pages/PagePostDetail.cs (최종 완성본 - UI/기능 통합 및 버튼 여백 수정)

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

        private FlowLayoutPanel pnlComments;
        private TextBox txtCommentInput;
        private Button btnAddComment;

        private TextBox txtTitle;
        private RichTextBox txtContent;
        private Button btnSave;
        private Button btnCancel;
        private Button btnDelete;
        
        // ⭐️ 게시물 공감 관련 컨트롤 (사용하지 않을 경우 주석 처리 또는 제거)
        // private Button btnPostLike;
        // private Label lblPostLikes;

        private const int ContentWidth = 600;

        public PagePostDetail(int postId = -1)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            InitializeUIControls();

            if (postId != -1)
            {
                // LoadPost 호출
                LoadPost(postId);
            }
            else
            {
                // 새 글 작성 모드
                currentPost = new Post { AuthorId = AuthService.CurrentUser.Id };
                txtTitle.Text = "새 글 제목";
                btnSave.Text = "작성 완료";
                btnDelete.Visible = false;
            }

            Theme.Apply(this);
        }

        public PagePostDetail() : this(-1) { }


        private void InitializeUIControls()
        {
            var layoutPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            // 1. 게시물 상세 정보 UI
            txtTitle = new TextBox { Font = new Font("Segoe UI", 16f, FontStyle.Bold), Height = 30, ForeColor = Theme.FgDefault, BackColor = Theme.BgMain, ReadOnly = true, Width = ContentWidth };
            txtContent = new RichTextBox { Text = "로딩 중...", Height = 250, Width = ContentWidth, BackColor = Theme.BgCard, ForeColor = Theme.FgDefault, ReadOnly = true };

            // 2. 버튼 영역 (저장/취소/삭제)
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

            // 3. 댓글 입력 및 목록 영역 추가
            var lblCommentTitle = new Label { Text = "댓글", AutoSize = true, Margin = new Padding(0, 20, 0, 5), ForeColor = Theme.FgDefault, Font = new Font("Segoe UI", 14f, FontStyle.Bold), Width = ContentWidth };

            pnlComments = new FlowLayoutPanel
            {
                Width = ContentWidth, // ⭐️ 너비 통일
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Margin = new Padding(0, 0, 0, 10)
            };

            // 4. 컨트롤 배치
            layoutPanel.Controls.AddRange(new Control[] {
                txtTitle,
                txtContent,
                pnlButtons,

                lblCommentTitle,
                CreateCommentInputPanel(),
                pnlComments
            });
            this.Controls.Add(layoutPanel);
        }

        private Control CreateCommentInputPanel()
        {
            var pnlInput = new Panel
            {
                Width = ContentWidth, // ⭐️ 너비 통일
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
            // ⚠️ 임시 데이터 사용 (실제 DB 연결 로직으로 교체 필요)
            currentPost = _boardService.GetPostById(postId);

            if (currentPost == null)
            {
                // 임시 포스트 생성 (DB 연결 전 테스트용)
                 currentPost = new Post
                {
                    Id = postId,
                    Title = $"게시글 상세 ID:{postId}",
                    AuthorId = AuthService.CurrentUser.Id, 
                    AuthorName = AuthService.CurrentUser.Name,
                    Content = $"이 게시글은 {postId}번 게시글입니다. 상세 설명...",
                    CreatedDate = DateTime.Now
                };
            }

            if (currentPost != null)
            {
                txtTitle.Text = currentPost.Title;
                txtContent.Text = currentPost.Content;

                bool isAuthor = currentPost.AuthorId == AuthService.CurrentUser.Id;
                if (!isAuthor)
                {
                    btnSave.Visible = false;
                    btnDelete.Visible = false;
                    txtTitle.ReadOnly = true;
                }

                LoadComments();
            }
        }

        private void LoadComments()
        {
            if (currentPost == null) return;
            pnlComments.Controls.Clear();

            // ⚠️ CommentService.GetCommentsByPostId(currentPost.Id)는 DB에 접속합니다.
            List<Comment> comments = _commentService.GetCommentsByPostId(currentPost.Id);

            if (comments.Count == 0)
            {
                pnlComments.Controls.Add(new Label { Text = "아직 댓글이 없습니다.", ForeColor = Theme.FgMuted, AutoSize = true, Padding = new Padding(0, 5, 0, 5) });
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
                Width = ContentWidth, // ⭐️ 너비 통일
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
                RowCount = 2, // ⭐️ 행을 2개로 줄여서 작성자 정보와 댓글 내용, 액션을 한 줄에 담습니다.
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 100),
                    new ColumnStyle(SizeType.Absolute, 150)
                }
            };

            // ⭐️ 댓글 내용 (큰 영역)
            var lblContent = new Label
            {
                Text = comment.Content,
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                MaximumSize = new Size(ContentWidth - 30, 0),
                Margin = new Padding(0, 0, 0, 5) // 내용 아래 여백
            };

            // ⭐️ 작성자 정보 (댓글 내용 아래)
            var lblAuthorInfo = new Label
            {
                Text = $"{comment.AuthorName} | {comment.CreatedDate:MM-dd HH:mm}",
                ForeColor = Theme.FgMuted,
                AutoSize = true
            };
            
            // ⭐️ 액션 패널: 공감 카운터, 공감 버튼, 삭제 버튼을 담습니다.
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0)
            };

            // 공감 카운터
            int likes = comment.Likes;
            var lblLikes = new Label { Text = $"{likes}개", ForeColor = Theme.FgMuted, AutoSize = true, Margin = new Padding(5, 5, 5, 0) };

            // 공감 버튼
            var btnLike = new Button { 
                Text = $"👍 공감", 
                Width = 70, 
                Height = 25, 
                Tag = comment.Id,
                Margin = new Padding(0, 0, 8, 0) // ⭐️ 버튼 간 여백 추가
            };
            Theme.StyleButton(btnLike);
            btnLike.Click += BtnLikeComment_Click;

            // ⭐️ 삭제 버튼 추가 (작성자에게만)
            if (comment.AuthorId == AuthService.CurrentUser.Id)
            {
                var btnDel = new Button { 
                    Text = "삭제", 
                    Width = 50, 
                    Height = 25, 
                    Tag = comment.Id,
                    Margin = new Padding(0, 0, 8, 0) // ⭐️ 버튼 간 여백 추가
                }; 
                Theme.StyleDanger(btnDel);
                btnDel.Font = new Font("Segoe UI", 9f);
                btnDel.Click += BtnDeleteComment_Click;
                
                pnlActions.Controls.Add(btnDel);
                pnlActions.Controls.SetChildIndex(btnDel, 0); // 가장 오른쪽
            }
            
            pnlActions.Controls.Add(btnLike); // 버튼 추가
            pnlActions.Controls.SetChildIndex(btnLike, comment.AuthorId == AuthService.CurrentUser.Id ? 1 : 0); // 삭제 버튼 다음/가장 오른쪽
            pnlActions.Controls.Add(lblLikes); // 카운터 추가


            // TableLayoutPanel에 컨트롤 배치
            layout.Controls.Add(lblContent, 0, 0); // 내용 (좌측 상단)
            layout.Controls.Add(lblAuthorInfo, 0, 1); // 작성자 정보 (좌측 하단)
            
            layout.Controls.Add(pnlActions, 1, 1); // 액션 (우측 하단)
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


        // 기존 메서드 (수정 및 삭제, 취소)
        // ------------------------------------

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
                _boardService.CreatePost(currentPost);
                MessageBox.Show("새 게시글이 작성되었습니다.", "완료");
            }
            else
            {
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