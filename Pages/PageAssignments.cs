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
            // ⚠️ _assignmentService.GetAssignmentsForUser() 메서드가 AssignmentService에 정의되어 있다고 가정합니다.
            // allAssignments = _assignmentService.GetAssignmentsForUser(); 
            // 임시 데이터 (실제 서비스 호출로 대체하세요)
            allAssignments = new List<Assignment>()
            {
                new Assignment { Id = 1, Course = "C# WinForms", Title = "홈페이지 대시보드 UI 구현", DueDate = DateTime.Now.AddHours(12), Status = "미제출" },
                new Assignment { Id = 2, Course = "SQLite DB", Title = "게시물 권한 및 삭제 기능 구현", DueDate = DateTime.Now.AddDays(3), Status = "제출 완료" },
                new Assignment { Id = 3, Course = "Theme.cs", Title = "다크모드 버튼 스타일링", DueDate = DateTime.Now.AddDays(10), Status = "미제출" }
            };
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