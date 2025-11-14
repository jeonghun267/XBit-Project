using System.Windows.Forms;
using System.Drawing;

namespace XBit
{
    public class TermsAndConditionsForm : Form
    {
        public TermsAndConditionsForm()
        {
            this.Text = "서비스 이용 약관";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            var rtbTerms = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = "제 1조 (목적)\n본 약관은 XBit 서비스 이용에 관한 사항을 규정합니다.\n\n제 2조 (개인정보 보호)\n사용자의 개인정보는 서비스 제공 목적으로만 사용됩니다.\n\n제 3조 (권한)\n관리자는 사용자 계정 및 게시글에 대한 관리 권한을 갖습니다. 사용자는 본인의 게시글에 대한 수정 및 삭제 권한을 갖습니다.\n\n[이하 생략]",
                Font = new Font("Segoe UI", 10f),
                Padding = new Padding(10)
            };

            // ⭐️ Controls.Add 호출 (생성자 내부)
            this.Controls.Add(rtbTerms);
        }
    }
}