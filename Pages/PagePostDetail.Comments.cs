using System;
using System.Windows.Forms;
using System.Collections.Generic;
using XBit.Models;
using XBit.Services;

namespace XBit.Pages
{
    public partial class PagePostDetail : UserControl
    {
        // 댓글 영역 초기화(필요하면 호출)
        public void InitializeCommentsPanel()
        {
            if (pnlComments != null) return;

            pnlComments = new FlowLayoutPanel
            {
                Width = ContentWidth,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Margin = new Padding(0, 0, 0, 10)
            };

            // main 레이아웃에 추가(필요하면 위치 조정)
            this.Controls.Add(pnlComments);
            pnlComments.BringToFront();
        }

        // 댓글 새로고침 (LoadComments와 동일한 역할)
        public void RefreshComments()
        {
            if (currentPost == null) return;

            if (pnlComments == null) InitializeCommentsPanel();
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
    }
}