# X BIT — 프로젝트 제출·협업 관리 툴

Visual Studio .NET 환경에서 **Windows Forms**로 개발한 학생용 과제 제출 및 팀 협업 관리 도구입니다.
복잡한 Git 명령어를 몰라도 직관적인 UI로 파일을 정리하고, **원클릭으로 커밋·푸시**까지 자동화할 수 있도록 설계되었습니다.

## 주요 기능

1. **프로젝트·과제 제출 관리**
   - 과제 파일·문서 체계적 관리, 제출 기한·상태 추적, 제출 이력 보관
   - `Submissions/{username}/assignment-{id}/` 폴더 구조로 자동 정리
2. **Git 연동 및 자동화 (LibGit2Sharp + Octokit)**
   - 학생별 브랜치 자동 생성: `submission-{username}-project-{id}`
   - **원클릭 전체 동기화**: 변경 감지 → Stage → Commit → Push
   - GitHub 토큰 기반 인증, 커밋 메시지·README 자동 생성
3. **팀 협업 기능**
   - 팀 생성·멤버 초대·역할(Owner/Member) 관리
   - 태스크 보드(우선순위·상태), 게시판, 댓글·반응(좋아요/싫어요)
   - 알림 센터 (과제 제출, 댓글, 팀 초대 이벤트)
4. **사용자 친화적 인터페이스**
   - 다크/라이트 테마 런타임 전환
   - 9개 페이지 카드 레이아웃 (Home / Assignments / Board / ProjectBoard / Statistics / Notifications / Settings / …)
   - 토스트 알림, 더블 버퍼 패널로 깜빡임 제거

## 기술 스택

| 영역 | 사용 기술 |
| --- | --- |
| 언어 / 런타임 | C#, .NET Framework 4.7.2 |
| UI | Windows Forms (WinForms), TableLayoutPanel 반응형 |
| DB | SQLite (`~/Documents/XBitData/xbit.sqlite`), 9개 테이블 + 13개 인덱스 |
| Git 연동 | LibGit2Sharp 0.31, Octokit 14 |
| 직렬화 | Newtonsoft.Json 13 |
| 보안 | SHA-256 비밀번호 해싱 |

## 프로젝트 구조

```
X BIT/
├─ Models/         User, Assignment, Post, Comment, Notification,
│                  Project, ProjectTask, Settings, Team
├─ Pages/          PageHome, PageAssignments, PageAssignmentDetail,
│                  PageBoard, PagePostDetail, PageProjectBoard,
│                  PageStatistics, PageNotifications, PageSettings
├─ Services/       AuthService, AssignmentService, BoardService,
│                  CommentService, DatabaseManager, FileService,
│                  GitHubService, NotificationService, SettingsService,
│                  StatisticsService, TaskService, TeamService, ...
├─ Dialogs/        TaskAddDialog
├─ UI/             ToastForm, DoubleBufferedFlowLayoutPanel
├─ Resources/      아이콘·이미지 리소스
├─ Submissions/    제출 파일 저장소 (자동 생성)
├─ Theme.cs        다크/라이트 테마 통합 관리
├─ MainForm.cs     메인 셸 (네비게이션 스택)
└─ Program.cs      엔트리 포인트
```

## 빌드 및 실행

### 요구 사항
- Windows + Visual Studio 2019 이상 (.NET Framework 4.7.2 개발자 팩)
- NuGet 패키지 자동 복원 활성화

### 절차
1. `X BIT.csproj` 또는 솔루션 파일을 Visual Studio로 열기
2. **빌드 → NuGet 패키지 복원** (LibGit2Sharp, Octokit, System.Data.SQLite 등)
3. **F5**로 디버그 실행
4. 최초 실행 시 `~/Documents/XBitData/xbit.sqlite`가 자동 생성됨

### GitHub 연동 설정
**설정 페이지 → 통합 → GitHub** 에서 다음을 입력:
- GitHub Personal Access Token (`repo` 권한 필요)
- GitHub 사용자명

이후 과제 제출 시 자동으로 학생별 브랜치 생성·커밋·푸시가 수행됩니다.

## 시연 영상

[YouTube — XBIT 시연](https://www.youtube.com/watch?v=oTahLEX3NXo)

## 라이선스

학습·포트폴리오 목적 비공개 프로젝트.
