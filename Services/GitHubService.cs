// XBit/Services/GitHubService.cs (네임스페이스 충돌 해결)

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using LibGit2Sharp; // ⚠️ using 문 순서 중요
using XBit.Services;
using XBit.Models;

namespace XBit.Services
{
    public class GitHubService
    {
        private readonly string _token;
        private readonly string _username;
        
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
            System.Diagnostics.Debug.WriteLine($"[GitHubService] Repo Exists: {Directory.Exists(_localRepoPath)}");
        }

        public async Task<string> CommitAndPush(int projectId, string localFilePath)
        {
            if (string.IsNullOrEmpty(_token)) 
                throw new InvalidOperationException("GitHub 토큰이 설정되지 않았습니다.");
                
            if (!System.IO.Directory.Exists(_localRepoPath))
                throw new DirectoryNotFoundException($"로컬 Git 저장소 경로를 찾을 수 없습니다: {_localRepoPath}"); 

            string assignmentFileName = Path.GetFileName(localFilePath);
            string targetPath = Path.Combine(_localRepoPath, assignmentFileName);
            string branchName = $"project-{projectId}-submission-{_username}";
            string commitMessage = $"Project #{projectId} submitted by {_username}";
            
            System.IO.File.Copy(localFilePath, targetPath, true);

            return await Task.Run(() => 
            {
                // ⭐️ LibGit2Sharp.Repository로 명시
                using (var repo = new LibGit2Sharp.Repository(_localRepoPath)) 
                {
                    if (repo.Branches[branchName] == null)
                    {
                        repo.CreateBranch(branchName); 
                    }
                    Commands.Checkout(repo, branchName);

                    Commands.Stage(repo, assignmentFileName);

                    var signature = new LibGit2Sharp.Signature(_username, $"{_username}@example.com", DateTimeOffset.Now);
                    repo.Commit(commitMessage, signature, signature);

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

                    return branchName;
                }
            });
        }

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
                    System.Diagnostics.Debug.WriteLine("[SyncAllChanges] Repository 열기 시도...");
                    
                    // ⭐️ LibGit2Sharp.Repository로 명시
                    using (var repo = new LibGit2Sharp.Repository(_localRepoPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] 현재 브랜치: {repo.Head.FriendlyName}");
                        
                        var status = repo.RetrieveStatus();
                        
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] Modified: {status.Modified.Count()}");
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] Added: {status.Added.Count()}");
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] Removed: {status.Removed.Count()}");
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] Untracked: {status.Untracked.Count()}");
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] IsDirty: {status.IsDirty}");

                        if (!status.IsDirty)
                        {
                            System.Diagnostics.Debug.WriteLine("[SyncAllChanges] 변경사항 없음");
                            return false;
                        }

                        foreach (var item in status.Modified)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - Modified: {item.FilePath}");
                        }
                        foreach (var item in status.Added)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - Added: {item.FilePath}");
                        }
                        foreach (var item in status.Untracked)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - Untracked: {item.FilePath}");
                        }

                        System.Diagnostics.Debug.WriteLine("[SyncAllChanges] Staging 시작...");
                        Commands.Stage(repo, "*");

                        System.Diagnostics.Debug.WriteLine("[SyncAllChanges] Commit 시작...");
                        // ⭐️ LibGit2Sharp.Signature로 명시
                        var signature = new LibGit2Sharp.Signature(_username, $"{_username}@example.com", DateTimeOffset.Now);
                        var commit = repo.Commit(
                            $"Auto sync from XBit App - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                            signature,
                            signature
                        );
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] Commit 완료: {commit.Sha}");

                        System.Diagnostics.Debug.WriteLine("[SyncAllChanges] Push 시작...");
                        var options = new PushOptions
                        {
                            CredentialsProvider = (url, user, cred) =>
                            {
                                System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] 인증 요청 - URL: {url}, User: {user}");
                                return new UsernamePasswordCredentials
                                {
                                    Username = _username,
                                    Password = _token
                                };
                            }
                        };

                        var remote = repo.Network.Remotes["origin"];
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] Remote: {remote.Name} - {remote.Url}");
                        
                        var currentBranch = repo.Head;
                        System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] Pushing branch: {currentBranch.CanonicalName}");
                        
                        repo.Network.Push(remote, currentBranch.CanonicalName, options);
                        
                        System.Diagnostics.Debug.WriteLine("[SyncAllChanges] Push 완료!");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncAllChanges] 예외 발생!");
                    System.Diagnostics.Debug.WriteLine($"  Type: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"  Message: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  StackTrace: {ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  InnerException: {ex.InnerException.Message}");
                    }
                    
                    return false;
                }
            });
        }

        public int GetChangedFilesCount()
        {
            try
            {
                if (!Directory.Exists(_localRepoPath))
                {
                    System.Diagnostics.Debug.WriteLine("[GetChangedFilesCount] 저장소 경로 없음");
                    return 0;
                }

                // ⭐️ LibGit2Sharp.Repository로 명시
                using (var repo = new LibGit2Sharp.Repository(_localRepoPath))
                {
                    var status = repo.RetrieveStatus();
                    int count = status.Modified.Count() + status.Added.Count() + status.Removed.Count() + status.Untracked.Count();
                    
                    System.Diagnostics.Debug.WriteLine($"[GetChangedFilesCount] 변경된 파일: {count}개");
                    return count;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetChangedFilesCount] 오류: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> CreatePullRequest(string title, string headBranch)
        {
            var client = new GitHubClient(new ProductHeaderValue("XBit-App"))
            {
                Credentials = new Octokit.Credentials(_token) 
            };

            var newPr = new NewPullRequest(title, headBranch, "main") 
            {
                Body = $"XBit 앱을 통해 제출된 프로젝트: {title}"
            };

            var pr = await client.PullRequest.Create(_repoOwner, _repoName, newPr); 
            return pr.Number; 
        }
    }
}