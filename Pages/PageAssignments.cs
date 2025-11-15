// Pages/PageAssignments.cs (최종 수정본 - 필터링 파라미터 수신 및 적용)

using System;
using System.Drawing;
using System.Windows.Forms;
using XBit.Services;
using XBit.Models;
using System.Collections.Generic;
using System.Linq;

namespace XBit.Pages
{
    public class PageAssignments : UserControl
    {
        private DataGridView grid;
        private AssignmentService _assignmentService = new AssignmentService();
        private List<Assignment> allAssignments;

        // ⭐️ 필터링 파라미터를 받을 수 있도록 생성자 수정 (옵션 1)
        public PageAssignments(string initialFilter = "ALL")
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain; // ⭐️ 배경색 설정

            LoadAllAssignments();
            InitializeLayout();

            // 초기 로드 시 전달받은 필터를 적용
            FilterData(initialFilter);
        }

        // ⭐️ 기존 public FilterData(string filter) 메서드를 유지하고 생성자를 수정하여 Home과 연결합니다.
        // public PageAssignments() : this("ALL") { } 
        // ⚠️ MainForm에서 NavigateTo<PageAssignments>("FILTER") 형태로 호출하므로, 위의 생성자 형태를 유지하거나,
        // 아래처럼 public FilterData(string filter) 메서드를 유지하는 것이 기존 코드와 호환성이 높습니다.
        public PageAssignments() : this("ALL") { } // 매개변수 없는 생성자는 기본 필터로 호출

        private void InitializeLayout()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1, // 필터 버튼을 제거했으므로 행 1개로 단순화
                Padding = new Padding(10)
            };

            // ⭐️ DataGridView 초기화 (기존 코드 유지)
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None
            };
            // Theme.StyleDataGridView(grid); // 스타일링 적용 필요

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "과목", DataPropertyName = "Course", Width = 160 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "제목", DataPropertyName = "Title", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "마감", DataPropertyName = "DueDate", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "상태", DataPropertyName = "Status", Width = 100 });
            grid.CellDoubleClick += Grid_CellDoubleClick;

            mainLayout.Controls.Add(grid, 0, 0); // 그리드를 메인 레이아웃에 추가
            this.Controls.Add(mainLayout);
        }

        private void LoadAllAssignments()
        {
            // ⭐️ 실제 DB에서 데이터 가져오기
            allAssignments = _assignmentService.GetAssignmentsForUser(AuthService.CurrentUser.Id);
        }

        // ⭐️ FilterData 메서드 유지 및 Home 카드 연동에 필요한 로직 반영
        public void FilterData(string filter)
        {
            if (allAssignments == null) LoadAllAssignments();

            IEnumerable<Assignment> filteredList = allAssignments;
            DateTime now = DateTime.Now;

            switch (filter)
            {
                case "DueToday":
                case "DUE_SOON": // Home 카드 연동 (24시간 이내)
                    filteredList = allAssignments.Where(a => (a.DueDate - now).TotalHours <= 24 && (a.DueDate - now).TotalHours > 0 && a.Status != "제출 완료");
                    break;

                case "DueThisWeek":
                case "WEEKLY": // Home 카드 연동 (이번 주)
                    DateTime startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    DateTime endOfWeek = startOfWeek.AddDays(7);
                    filteredList = allAssignments.Where(a => a.DueDate >= startOfWeek && a.DueDate < endOfWeek);
                    break;

                case "ALL":
                default:
                    filteredList = allAssignments;
                    break;
            }

            grid.DataSource = filteredList.ToList();
            grid.Refresh();
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var selectedAssignment = grid.Rows[e.RowIndex].DataBoundItem as Assignment;

                if (selectedAssignment != null)
                {
                    var mainForm = FindForm() as MainForm;
                    if (mainForm != null)
                    {
                        mainForm.NavigateTo<PageAssignmentDetail>(selectedAssignment.Id);
                    }
                }
            }
        }
    }
}