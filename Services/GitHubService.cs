// GitHubService.cs (간단하고 즉시 작동하는 버전)

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using LibGit2Sharp;
using XBit.Services;
using XBit.Models;

namespace XBit.Services
{
    public class GitHubService
    {
        private readonly string _token;
        private readonly string _username;
        
        // ⭐️ 공용 저장소 (현재 프로젝트)
        private readonly string _repoOwner = "jeonghun267";
        private readonly string _repoName = "XBit-Project";
        private readonly string _localRepoPath = @"C:\Users\1\source\repos\X BIT\X BIT";

        public GitHubService()
        {
            var settings = SettingsService.Current; 
            
            _token = settings.Integrations.GitHubToken;
            _username = settings.Integrations.GitHubUser;

            System.Diagnostics.Debug.WriteLine($"[GitHubService] Token: {(_token != null ? "설정됨" : "없음")}");
            System.Diagnostics.Debug.WriteLine($"[GitHubService] Username: {_username ?? "없음"}");
            System.Diagnostics.Debug.WriteLine($"[GitHubService] LocalRepoPath: {_localRepoPath}");
        }

        // ⭐️ 학생별 폴더에 제출
        public async Task<string> CommitAndPushToClassroom(int projectId, string localFilePath)
        {
            if (string.IsNullOrEmpty(_token)) 
                throw new InvalidOperationException("GitHub 토큰이 설정되지 않았습니다.");
                
            if (!Directory.Exists(_localRepoPath))
                throw new DirectoryNotFoundException($"로컬 Git 저장소 경로를 찾을 수 없습니다: {_localRepoPath}");

            // ⭐️ 학생별 제출 폴더: Submissions/{username}/assignment-{id}/
            string submissionFolder = Path.Combine(
                _localRepoPath, 
                "Submissions", 
                _username, 
                $"assignment-{projectId}"
            );

            // 폴더 생성
            if (!Directory.Exists(submissionFolder))
            {
                Directory.CreateDirectory(submissionFolder);
                System.Diagnostics.Debug.WriteLine($"[CommitAndPushToClassroom] 폴더 생성: {submissionFolder}");
            }

            // 파일 복사
            string fileName = Path.GetFileName(localFilePath);
            string targetPath = Path.Combine(submissionFolder, fileName);
            File.Copy(localFilePath, targetPath, true);

            System.Diagnostics.Debug.WriteLine($"[CommitAndPushToClassroom] 파일 복사됨: {targetPath}");

            // ⭐️ 제출 정보 파일 생성 (README.md)
            string readmePath = Path.Combine(submissionFolder, "README.md");
            string readmeContent = $@"# Project {projectId} - {_username}

**제출자:** {_username}  
**제출 시각:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}  
**파일:** {fileName}  

---
*XBit 앱을 통해 자동 제출되었습니다.*
";
            File.WriteAllText(readmePath, readmeContent);

            // Git 작업
            return await Task.Run(() => 
            {
                using (var repo = new LibGit2Sharp.Repository(_localRepoPath)) 
                {
                    // ⭐️ 브랜치 생성 (학생별)
                    string branchName = $"submission-{_username}-project-{projectId}";
                    
                    // main 브랜치로 체크아웃
                    Commands.Checkout(repo, "main");
                    
                    // 브랜치가 없으면 생성
                    if (repo.Branches[branchName] == null)
                    {
                        repo.CreateBranch(branchName);
                        System.Diagnostics.Debug.WriteLine($"[CommitAndPushToClassroom] 브랜치 생성: {branchName}");
                    }
                    
                    Commands.Checkout(repo, branchName);

                    // ⭐️ Submissions/{username}/assignment-{id}/ 전체 추가
                    string gitPath = $"Submissions/{_username}/assignment-{projectId}/*";
                    Commands.Stage(repo, gitPath);

                    System.Diagnostics.Debug.WriteLine($"[CommitAndPushToClassroom] Staged: {gitPath}");

                    // Commit
                    var signature = new LibGit2Sharp.Signature(
                        _username, 
                        $"{_username}@student.edu", 
                        DateTimeOffset.Now
                    );
                    
                    string commitMessage = $@"Project #{projectId} submitted by {_username}

제출 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
파일: {fileName}
";
                    
                    var commit = repo.Commit(commitMessage, signature, signature);
                    System.Diagnostics.Debug.WriteLine($"[CommitAndPushToClassroom] Commit: {commit.Sha}");

                    // Push
                    var options = new PushOptions
                    {
                        CredentialsProvider = (url, user, cred) => new LibGit2Sharp.UsernamePasswordCredentials
                        {
                            Username = _username,
                            Password = _token
                        }
                    };
                    
                    var remote = repo.Network.Remotes["origin"];
                    repo.Network.Push(remote, repo.Head.CanonicalName, options);
                    
                    System.Diagnostics.Debug.WriteLine($"[CommitAndPushToClassroom] Push 완료: {branchName}");

                    return commit.Sha;
                }
            });
        }

        // ⭐️ 제출 URL 생성
        public async Task<string> GetSubmissionUrl(int projectId)
        {
            string branchName = $"submission-{_username}-project-{projectId}";
            string url = $"https://github.com/{_repoOwner}/{_repoName}/tree/{branchName}/Submissions/{_username}/assignment-{projectId}";
            return await Task.FromResult(url);
        }

        // 기존 메서드들 유지
        public async Task<bool> SyncAllChanges()
        {
            System.Diagnostics.Debug.WriteLine("[SyncAllChanges] 시작");

            if (string.IsNullOrEmpty(_token))
            {
                System.Diagnostics.Debug.WriteLine("[SyncAllChanges] 오류: 토큰이 없음");
                return false;
            }

            if (!Directory.Exists(_localRepoPath))
            {
                System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] 오류: 저장소 경로 없음 - {_localRepoPath}");
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    using (var repo = new LibGit2Sharp.Repository(_localRepoPath))
                    {
                        var status = repo.RetrieveStatus();
                        
                        if (!status.IsDirty)
                        {
                            System.Diagnostics.Debug.WriteLine("[SyncAllChanges] 변경사항 없음");
                            return false;
                        }

                        Commands.Stage(repo, "*");

                        var signature = new LibGit2Sharp.Signature(_username, $"{_username}@example.com", DateTimeOffset.Now);
                        var commit = repo.Commit(
                            $"Auto sync from XBit App - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                            signature,
                            signature
                        );

                        var options = new PushOptions
                        {
                            CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                            {
                                Username = _username,
                                Password = _token
                            }
                        };

                        var remote = repo.Network.Remotes["origin"];
                        repo.Network.Push(remote, repo.Head.CanonicalName, options);
                        
                        System.Diagnostics.Debug.WriteLine("[SyncAllChanges] Push 완료!");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] 예외: {ex.Message}");
                    return false;
                }
            });
        }

        public int GetChangedFilesCount()
        {
            try
            {
                if (!Directory.Exists(_localRepoPath))
                    return 0;

                using (var repo = new LibGit2Sharp.Repository(_localRepoPath))
                {
                    var status = repo.RetrieveStatus();
                    return status.Modified.Count() + status.Added.Count() + status.Removed.Count() + status.Untracked.Count();
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}