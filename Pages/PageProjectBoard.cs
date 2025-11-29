// Pages/PageProjectBoard.cs (수정: 작업 추가/상태 이동 시 알림 생성, Tag 타입 안전화, 성공 MessageBox 제거, 대기 커서 적용)

using System;
using System.Windows.Forms;
using System.Drawing;
using XBit.Models;
using XBit.Services;
using System.Collections.Generic;
using System.Linq;

namespace XBit.Pages
{
    public class PageProjectBoard : UserControl
    {
        private const int ColumnWidth = 320;
        private const int CardHeight = 100;
        private const int StatusCount = 3;

        // 팀 관련 컨트롤
        private System.Windows.Forms.Label lblTeamName;
        private System.Windows.Forms.Label lblTeamMembers;
        private Button btnInviteTeam;

        // 칸반 보드 관련
        private Panel[] pnlColumns = new Panel[StatusCount];
        private FlowLayoutPanel[] pnlTasks = new FlowLayoutPanel[StatusCount];
        private System.Windows.Forms.Label[] lblStatusTitle = new System.Windows.Forms.Label[StatusCount];

        private Button btnAddTask;
        private ComboBox cmbTeams;

        // Drag & Drop 관련 필드
        private Panel draggedCard = null;
        private FlowLayoutPanel sourceContainer = null;

        private TaskService _taskService = new TaskService();
        private TeamService _team_service = new TeamService(); // 필드명 통일: _team_service
        private int currentTeamId = 1;  // 현재 선택된 팀 ID

        public PageProjectBoard()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgMain;

            InitializeUIControls();
            LoadTeams();

            Theme.Apply(this);
        }

        private void InitializeUIControls()
        {
            this.Padding = new Padding(0);
            
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var pnlHeader = CreateTeamHeader();
            var pnlKanban = CreateKanbanBoard();

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlKanban, 0, 1);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateTeamHeader()
        {
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(15)
            };

            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            var lblSelectTeam = new Label
            {
                Text = "팀 선택",
                AutoSize = true,
                ForeColor = Theme.FgDefault,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left
            };

            cmbTeams = new ComboBox
            {
                Width = 180,
                BackColor = Theme.BgMain,
                ForeColor = Theme.FgDefault,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left
            };
            cmbTeams.SelectedIndexChanged += CmbTeams_SelectedIndexChanged;

            var pnlTeamSelect = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            pnlTeamSelect.Controls.Add(lblSelectTeam);
            pnlTeamSelect.Controls.Add(cmbTeams);

            lblTeamName = new System.Windows.Forms.Label
            {
                Text = "팀 이름",
                Font = new Font("맑은 고딕", 12f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left
            };

            lblTeamMembers = new System.Windows.Forms.Label
            {
                Text = "멤버: 0",
                ForeColor = Theme.FgMuted,
                AutoSize = true,
                Font = new Font("맑은 고딕", 9f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left
            };

            btnInviteTeam = new Button
            {
                Text = "멤버 초대",
                Width = 120,
                Height = 35,
                Anchor = AnchorStyles.None
            };
            Theme.StylePrimaryButton(btnInviteTeam);
            btnInviteTeam.Click += BtnInviteTeam_Click;

            btnAddTask = new Button
            {
                Text = "작업 추가",
                Width = 100,
                Height = 35,
                Margin = new Padding(5, 0, 0, 0),
                Anchor = AnchorStyles.None
            };
            Theme.StylePrimaryButton(btnAddTask);
            btnAddTask.Click += BtnAddTask_Click;

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                WrapContents = false
            };
            pnlButtons.Controls.Add(btnAddTask);
            pnlButtons.Controls.Add(btnInviteTeam);

            headerLayout.Controls.Add(pnlTeamSelect, 0, 0);
            headerLayout.Controls.Add(lblTeamName, 1, 0);
            headerLayout.Controls.Add(lblTeamMembers, 2, 0);
            headerLayout.Controls.Add(pnlButtons, 3, 0);

            pnlHeader.Controls.Add(headerLayout);
            return pnlHeader;
        }

        private Panel CreateKanbanBoard()
        {
            var pnlKanban = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgMain,
                AutoScroll = true
            };

            var boardLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            string[] statuses = { "할 일", "진행 중", "완료" };
            Color[] statusColors = 
            {
                Color.FromArgb(200, 200, 200),
                Color.FromArgb(66, 133, 244),
                Color.FromArgb(76, 175, 80)
            };

            for (int i = 0; i < StatusCount; i++)
            {
                pnlColumns[i] = new Panel
                {
                    Width = ColumnWidth,
                    Height = 450,
                    Margin = new Padding(10),
                    BackColor = Theme.BgCard,
                    BorderStyle = BorderStyle.FixedSingle
                };

                var columnLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    Padding = new Padding(0)
                };

                columnLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                columnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                columnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var pnlTitle = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = statusColors[i],
                    Padding = new Padding(0),
                    Margin = new Padding(0)
                };

                lblStatusTitle[i] = new System.Windows.Forms.Label
                {
                    Text = statuses[i],
                    Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
                    ForeColor = Color.White,
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding = new Padding(0),
                    Margin = new Padding(0)
                };
                pnlTitle.Controls.Add(lblStatusTitle[i]);

                pnlTasks[i] = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    AutoScroll = true,
                    Padding = new Padding(8),
                    BackColor = Theme.BgMain,
                    WrapContents = false,
                    Margin = new Padding(0),
                    AllowDrop = true
                };

                // 드래그 이벤트 연결 (DragLeave 추가)
                pnlTasks[i].DragEnter += TaskContainer_DragEnter;
                pnlTasks[i].DragLeave += TaskContainer_DragLeave;
                pnlTasks[i].DragDrop += TaskContainer_DragDrop;

                columnLayout.Controls.Add(pnlTitle, 0, 0);
                columnLayout.Controls.Add(pnlTasks[i], 0, 1);

                pnlColumns[i].Controls.Add(columnLayout);
                boardLayout.Controls.Add(pnlColumns[i]);
            }

            pnlKanban.Controls.Add(boardLayout);
            return pnlKanban;
        }

        // TaskCard의 Tag에 사용할 명시적 타입
        private class TaskCardInfo
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Assignee { get; set; }
            public string Priority { get; set; } // "긴급", "높음" 등
            public string Status { get; set; }   // "할 일", "진행 중", "완료" 등
        }

        private Panel CreateTaskCard(string title, string assignee, int priority, int taskId = -1)
        {
            var card = new Panel
            {
                Width = ColumnWidth - 30,
                Height = CardHeight,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 8),
                Cursor = Cursors.Hand,
                AllowDrop = false
            };

            // Tag에 TaskCardInfo 사용 (익명 객체 제거)
            card.Tag = new TaskCardInfo
            {
                Id = taskId,
                Title = title,
                Assignee = assignee,
                Priority = GetPriorityLabel(priority),
                Status = string.Empty
            };

            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                ForeColor = Theme.FgDefault,
                AutoSize = true,
                MaximumSize = new Size(ColumnWidth - 50, 0)
            };

            var lblAssignee = new Label
            {
                Text = "담당자: " + assignee,
                Font = new Font("맑은 고딕", 9f),
                ForeColor = Theme.FgMuted,
                AutoSize = true
            };

            var lblPriority = new Label
            {
                Text = GetPriorityLabel(priority),
                Font = new Font("맑은 고딕", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = GetPriorityColor(priority),
                Padding = new Padding(4, 3, 4, 3)
            };

            cardLayout.Controls.Add(lblTitle, 0, 0);
            cardLayout.Controls.Add(lblAssignee, 0, 1);
            cardLayout.Controls.Add(lblPriority, 0, 2);

            card.Controls.Add(cardLayout);

            // 카드와 모든 자식 컨트롤에 이벤트 연결
            card.MouseDown += Card_MouseDown;
            card.DoubleClick += (s, e) => ShowTaskDetails(title);
            
            // TableLayoutPanel에도 연결
            cardLayout.MouseDown += Card_MouseDown;
            cardLayout.DoubleClick += (s, e) => ShowTaskDetails(title);
            
            // 모든 Label에도 연결
            lblTitle.MouseDown += Card_MouseDown;
            lblTitle.DoubleClick += (s, e) => ShowTaskDetails(title);
            lblAssignee.MouseDown += Card_MouseDown;
            lblAssignee.DoubleClick += (s, e) => ShowTaskDetails(title);
            lblPriority.MouseDown += Card_MouseDown;
            lblPriority.DoubleClick += (s, e) => ShowTaskDetails(title);

            return card;
        }

        // Drag & Drop 이벤트 핸들러

        private void Card_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            Control control = sender as Control;
            Panel card = null;

            // 카드(Panel)를 찾을 때까지 부모를 올라감
            while (control != null)
            {
                if (control is Panel p && p.Tag is TaskCardInfo)
                {
                    card = p;
                    break;
                }
                control = control.Parent;
            }

            if (card != null)
            {
                draggedCard = card;
                sourceContainer = card.Parent as FlowLayoutPanel;
                card.DoDragDrop(card, DragDropEffects.Move);
            }
        }

        private void TaskContainer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Panel)))
            {
                e.Effect = DragDropEffects.Move;

                var container = sender as FlowLayoutPanel;
                if (container != null)
                {
                    container.BackColor = Color.FromArgb(220, 230, 255);
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // 드래그가 컨테이너를 벗어나면 배경색 복원
        private void TaskContainer_DragLeave(object sender, EventArgs e)
        {
            var container = sender as FlowLayoutPanel;
            if (container != null)
            {
                container.BackColor = Theme.BgMain;
            }
        }

        private void TaskContainer_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var targetContainer = sender as FlowLayoutPanel;
                if (targetContainer == null || draggedCard == null)
                    return;

                targetContainer.BackColor = Theme.BgMain;

                if (sourceContainer == targetContainer)
                    return;

                // UI 즉시 이동(시각적 피드백)
                if (sourceContainer != null)
                    sourceContainer.Controls.Remove(draggedCard);

                targetContainer.Controls.Add(draggedCard);

                string sourceStatus = GetContainerStatus(sourceContainer);
                string targetStatus = GetContainerStatus(targetContainer);

                // Tag를 안전하게 캐스팅
                var cardInfo = draggedCard.Tag as TaskCardInfo;
                if (cardInfo != null && cardInfo.Id > 0)
                {
                    bool success = false;
                    try
                    {
                        this.UseWaitCursor = true;
                        success = _task_service_safe_update(cardInfo.Id, targetStatus);
                    }
                    finally
                    {
                        this.UseWaitCursor = false;
                    }

                    if (success)
                    {
                        // 성공: MessageBox 제거 (시각적 이동으로 충분)
                        // 상태 변경 알림 전송 (변경한 사람 제외)
                        try
                        {
                            var members = _team_service.GetTeamMembers(currentTeamId);
                            if (members != null)
                            {
                                foreach (var m in members)
                                {
                                    if (AuthService.CurrentUser != null && m.UserId == AuthService.CurrentUser.Id) continue;

                                    var moverName = AuthService.CurrentUser != null
                                        ? (!string.IsNullOrWhiteSpace(AuthService.CurrentUser.Name) ? AuthService.CurrentUser.Name : AuthService.CurrentUser.Username)
                                        : "시스템";

                                    NotificationService.Create(
                                        m.UserId,
                                        "작업 상태가 변경되었습니다",
                                        $"{moverName}님이 '{cardInfo.Title}' 작업을 '{targetStatus}'로 이동했습니다.",
                                        "Task",
                                        cardInfo.Id
                                    );
                                }
                            }
                        }
                        catch
                        {
                            // 알림 실패는 치명적이지 않으므로 무시
                        }
                    }
                    else
                    {
                        // 실패 시: 사용자에게 명확히 알리고 UI 롤백
                        MessageBox.Show("데이터베이스 저장 실패!", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        // 롤백 UI
                        try
                        {
                            targetContainer.Controls.Remove(draggedCard);
                            sourceContainer.Controls.Add(draggedCard);
                        }
                        catch { /* 무시 */ }
                    }
                }

                draggedCard = null;
                sourceContainer = null;
            }
            catch (Exception ex)
            {
                // 예외 발생 시 UI 롤백 및 에러 표시
                try
                {
                    if (draggedCard != null && sourceContainer != null)
                    {
                        var parent = draggedCard.Parent as FlowLayoutPanel;
                        if (parent != null)
                            parent.Controls.Remove(draggedCard);
                        sourceContainer.Controls.Add(draggedCard);
                    }
                }
                catch { /* 추가 실패 무시 */ }

                MessageBox.Show("작업 이동 중 오류가 발생했습니다:\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                draggedCard = null;
                sourceContainer = null;
            }
        }

        private void CmbTeams_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = cmbTeams.SelectedItem as Team;
            if (selected != null)
            {
                currentTeamId = selected.Id;
                RefreshBoard();
            }
        }

        private void BtnInviteTeam_Click(object sender, EventArgs e)
        {
            string memberEmail = PromptDialog("멤버 이메일", "멤버 초대");
            if (!string.IsNullOrEmpty(memberEmail))
            {
                // ?? 실제로 멤버 추가
                var user = AuthService.GetUserByEmail(memberEmail);
                
                if (user != null)
                {
                    bool success = _team_service_safe_addmember(currentTeamId, user.Id);
                    
                    if (success)
                    {
                        MessageBox.Show($"{user.Name}({memberEmail})님이 팀에 초대되었습니다!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshBoard();
                    }
                    else
                    {
                        MessageBox.Show("이미 팀 멤버이거나 초대할 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("해당 이메일의 사용자를 찾을 수 없습니다.\n사용자가 먼저 가입해야 합니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnAddTask_Click(object sender, EventArgs e)
        {
            using (var dialog = new TaskAddDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;

                try
                {
                    bool success = false;
                    try
                    {
                        this.UseWaitCursor = true;
                        success = _taskService.AddTask(
                            dialog.TaskTitle,
                            dialog.Assignee,
                            dialog.Priority,
                            "할 일",
                            currentTeamId
                        );
                    }
                    finally
                    {
                        this.UseWaitCursor = false;
                    }

                    if (success)
                    {
                        // 성공 시 MessageBox 제거 ? UI 갱신으로 충분
                        RefreshBoard();

                        try
                        {
                            var members = _team_service.GetTeamMembers(currentTeamId);
                            if (members != null)
                            {
                                foreach (var m in members)
                                {
                                    if (AuthService.CurrentUser != null && m.UserId == AuthService.CurrentUser.Id) continue;

                                    var senderName = AuthService.CurrentUser != null
                                        ? (!string.IsNullOrWhiteSpace(AuthService.CurrentUser.Name) ? AuthService.CurrentUser.Name : AuthService.CurrentUser.Username)
                                        : "시스템";

                                    NotificationService.Create(
                                        m.UserId,
                                        "새 작업이 추가되었습니다",
                                        $"{senderName}님이 '{dialog.TaskTitle}' 작업을 추가했습니다.",
                                        "Task",
                                        currentTeamId // RelatedId로 팀/보드 ID 전달
                                    );
                                }
                            }
                        }
                        catch
                        {
                            // 알림 실패는 치명적이지 않으므로 무시
                        }

                        // 즉시 Home이 열려있으면 진행중 카운트 갱신
                        try
                        {
                            var mainForm = FindForm() as MainForm;
                            if (mainForm != null)
                            {
                                var home = mainForm.pnlContent.Controls.OfType<PageHome>().FirstOrDefault();
                                if (home != null)
                                {
                                    home.LoadData();
                                }

                                mainForm.UpdateNotificationBadge();
                            }
                        }
                        catch
                        {
                            // 무시
                        }
                    }
                    else
                    {
                        MessageBox.Show("작업 추가 실패!", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("작업 추가 중 오류가 발생했습니다:\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowTaskDetails(string taskTitle)
        {
            MessageBox.Show("작업: " + taskTitle + "\n\n팀원들이 협업할 수 있습니다.", "작업 상세");
        }

        private void StartDragTask(Panel card)
        {
            // Card_MouseDown에서 처리
        }

        private void LoadTeams()
        {
            var teams = GetTeamsSafe(); // 안전 래퍼 호출

            // 콤보박스 바인딩: DisplayMember/ValueMember 이용
            cmbTeams.DisplayMember = "Name";
            cmbTeams.ValueMember = "Id";
            cmbTeams.DataSource = teams;

            if (teams.Count > 0)
            {
                currentTeamId = teams[0].Id;
                cmbTeams.SelectedIndex = 0;
            }
            else
            {
                // 기본 팀 생성 및 다시 로드
                int teamId = _team_service_safe_create("기본 팀", AuthService.CurrentUser.Id);
                var newTeams = GetTeamsSafe();
                if (newTeams.Count > 0)
                {
                    currentTeamId = newTeams[0].Id;
                    cmbTeams.DataSource = newTeams;
                    cmbTeams.SelectedIndex = 0;
                }
            }

            AddSampleTasks();
        }

        private void AddSampleTasks()
        {
            // DB에서 작업 목록 가져오기
            var tasks = _taskService.GetTasksByTeam(currentTeamId);

            if (tasks.Count == 0)
            {
                // 샘플 데이터 추가
                _taskService.AddTask("UI 디자인 완료", "김철수", 1, "할 일", currentTeamId);
                _task_service_safe_add("API 문서화", "이영희", 2, "할 일", currentTeamId);
                _taskService.AddTask("데이터베이스 설계", "박민준", 2, "진행 중", currentTeamId);
                _taskService.AddTask("테스트 케이스 작성", "최지은", 3, "진행 중", currentTeamId);
                _taskService.AddTask("프로젝트 초기화", "김철수", 3, "완료", currentTeamId);
                _task_service_safe_add("팀 미팅", "이영희", 4, "완료", currentTeamId);
                
                // 다시 로드
                tasks = _taskService.GetTasksByTeam(currentTeamId);
            }

            // UI에 표시
            foreach (var task in tasks)
            {
                int columnIndex = GetColumnIndexByStatus(task.Status);
                if (columnIndex >= 0)
                {
                    pnlTasks[columnIndex].Controls.Add(
                        CreateTaskCard(task.Title, task.Assignee, task.Priority, task.Id)
                    );
                }
            }

            lblTeamMembers.Text = "멤버: 4";
        }

        // 상태에 따른 컬럼 인덱스 반환
        private int GetColumnIndexByStatus(string status)
        {
            if (status == "할 일") return 0;
            if (status == "진행 중") return 1;
            if (status == "완료") return 2;
            return -1;
        }

        private string PromptDialog(string message, string title)
        {
            var form = new Form
            {
                Text = title,
                Width = 400,
                Height = 150,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.BgMain
            };

            var label = new Label { Text = message, Left = 20, Top = 20, AutoSize = true, Font = new Font("맑은 고딕", 9f) };
            var textBox = new TextBox { Left = 20, Top = 50, Width = 340, Height = 30 };
            var okButton = new Button { Text = "확인", Left = 220, Top = 90, Width = 80, Height = 35 };
            var cancelButton = new Button { Text = "취소", Left = 310, Top = 90, Width = 80, Height = 35 };

            okButton.Click += (s, e) => form.DialogResult = DialogResult.OK;
            cancelButton.Click += (s, e) => form.DialogResult = DialogResult.Cancel;

            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(okButton);
            form.Controls.Add(cancelButton);

            Theme.StylePrimaryButton(okButton);
            Theme.StyleButton(cancelButton);

            return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }

        // RefreshBoard 메서드 추가 (파일 하단에)
        private void RefreshBoard()
        {
            // 모든 컬럼 초기화
            for (int i = 0; i < StatusCount; i++)
            {
                pnlTasks[i].Controls.Clear();
            }

            // DB에서 다시 로드
            var tasks = _taskService.GetTasksByTeam(currentTeamId);

            // UI에 표시
            foreach (var task in tasks)
            {
                int columnIndex = GetColumnIndexByStatus(task.Status);
                if (columnIndex >= 0)
                {
                    pnlTasks[columnIndex].Controls.Add(
                        CreateTaskCard(task.Title, task.Assignee, task.Priority, task.Id)
                    );
                }
            }

            lblTeamMembers.Text = "멤버: 4";
        }

        // 안전한 TeamService 호출 래퍼 (일관된 이름 사용)
        private List<Team> GetTeamsSafe()
        {
            try
            {
                if (_team_service == null)
                    _team_service = new TeamService();

                var teams = _team_service.GetTeamsByUser(AuthService.CurrentUser.Id);
                return teams ?? new List<Team>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PageProjectBoard] GetTeamsSafe 예외: {ex.Message}");
                return new List<Team>();
            }
        }

        // 안전한 TeamService - 팀 생성
        private int _team_service_safe_create(string name, int ownerId)
        {
            try
            {
                if (_team_service == null)
                    _team_service = new TeamService();

                return _team_service.CreateTeam(name, ownerId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PageProjectBoard] CreateTeamSafe 예외: {ex.Message}");
                return -1;
            }
        }

        // 안전한 TeamService - AddMember 래퍼
        private bool _team_service_safe_addmember(int teamId, int userId)
        {
            try
            {
                if (_team_service == null)
                    _team_service = new TeamService();

                return _team_service.AddMember(teamId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PageProjectBoard] AddMemberSafe 예외: {ex.Message}");
                return false;
            }
        }

        // 안전한 TaskService 추가(호출 일관성 유지)
        private bool _task_service_safe_update(int taskId, string status)
        {
            try
            {
                if (_taskService == null)
                    _taskService = new TaskService();

                return _taskService.UpdateTaskStatus(taskId, status);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PageProjectBoard] UpdateTaskStatusSafe 예외: {ex.Message}");
                return false;
            }
        }

        // 안전한 TaskService Add 래퍼 (샘플 추가에서 사용)
        private bool _task_service_safe_add(string title, string assignee, int priority, string status, int teamId)
        {
            try
            {
                if (_taskService == null)
                    _taskService = new TaskService();

                return _taskService.AddTask(title, assignee, priority, status, teamId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PageProjectBoard] AddTaskSafe 예외: {ex.Message}");
                return false;
            }
        }

        // 헬퍼 메서드: GetPriorityLabel, GetPriorityColor, GetContainerStatus
        // PageProjectBoard 클래스 내부에 위치해야 합니다.

        private string GetPriorityLabel(int priority)
        {
            if (priority == 1)
                return "긴급";
            else if (priority == 2)
                return "높음";
            else if (priority == 3)
                return "보통";
            else
                return "낮음";
        }

        private Color GetPriorityColor(int priority)
        {
            if (priority == 1)
                return Color.FromArgb(244, 67, 54);
            else if (priority == 2)
                return Color.FromArgb(255, 152, 0);
            else if (priority == 3)
                return Color.FromArgb(76, 175, 80);
            else
                return Color.FromArgb(189, 189, 189);
        }

        private string GetContainerStatus(FlowLayoutPanel container)
        {
            if (container == null) return "알 수 없음";

            for (int i = 0; i < StatusCount; i++)
            {
                if (pnlTasks[i] == container)
                    return lblStatusTitle[i].Text;
            }

            return "알 수 없음";
        }
    }
}