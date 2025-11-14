// XBit/Services/FileService.cs

using System;
using System.IO;
using System.Windows.Forms; // MessageBox 사용을 위해 추가

namespace XBit.Services
{
    public class FileService
    {
        // ⚠️ 제출된 파일을 저장할 로컬 디렉터리 정의
        private readonly string SubmissionDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Submissions");

        public FileService()
        {
            // 제출 폴더가 없으면 생성
            if (!Directory.Exists(SubmissionDirectory))
            {
                Directory.CreateDirectory(SubmissionDirectory);
            }
        }

        /// <summary>
        /// 선택된 파일을 지정된 과제 폴더에 복사하고 메타데이터를 DB에 저장합니다.
        /// </summary>
        /// <param name="sourceFilePath">원본 파일 경로</param>
        /// <param name="assignmentId">과제 ID</param>
        /// <returns>성공 여부</returns>
        public bool SubmitFile(string sourceFilePath, int assignmentId)
        {
            if (!File.Exists(sourceFilePath))
            {
                MessageBox.Show("원본 파일을 찾을 수 없습니다.", "제출 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                // 1. 과제별 폴더 생성 (예: Submissions/Assignment_1)
                string assignmentFolder = Path.Combine(SubmissionDirectory, $"Assignment_{assignmentId}");
                if (!Directory.Exists(assignmentFolder))
                {
                    Directory.CreateDirectory(assignmentFolder);
                }

                // 2. 파일 이름 설정 및 복사
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationPath = Path.Combine(assignmentFolder, fileName);

                // ⚠️ 이미 파일이 존재하면 덮어쓰거나 이름을 변경하는 로직 추가 필요
                File.Copy(sourceFilePath, destinationPath, overwrite: true);

                // 3. DB 메타데이터 저장 (현재는 로직 생략. AssignmentService 등에서 상태 업데이트 필요)

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 제출 중 오류 발생: {ex.Message}", "제출 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}