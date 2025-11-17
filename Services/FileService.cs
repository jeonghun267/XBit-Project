using System;
using System.IO;
using System.Windows.Forms;

namespace XBit.Services
{
    public class FileService
    {
        private readonly string SubmissionDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "Submissions"
        );

        public FileService()
        {
            if (!Directory.Exists(SubmissionDirectory))
            {
                Directory.CreateDirectory(SubmissionDirectory);
            }
        }

        public bool SubmitFile(string sourceFilePath, int assignmentId)
        {
            if (!File.Exists(sourceFilePath))
            {
                MessageBox.Show("원본 파일을 찾을 수 없습니다.", "제출 실패", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                string assignmentFolder = Path.Combine(
                    SubmissionDirectory, 
                    $"Assignment_{assignmentId}"
                );
                
                if (!Directory.Exists(assignmentFolder))
                {
                    Directory.CreateDirectory(assignmentFolder);
                }

                string fileName = Path.GetFileName(sourceFilePath);
                string destinationPath = Path.Combine(assignmentFolder, fileName);

                File.Copy(sourceFilePath, destinationPath, overwrite: true);

                System.Diagnostics.Debug.WriteLine($"[FileService] 파일 복사 완료: {destinationPath}");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 제출 중 오류 발생: {ex.Message}", "제출 실패", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public string[] GetSubmittedFiles(int assignmentId)
        {
            string assignmentFolder = Path.Combine(
                SubmissionDirectory, 
                $"Assignment_{assignmentId}"
            );

            if (Directory.Exists(assignmentFolder))
            {
                return Directory.GetFiles(assignmentFolder);
            }

            return new string[0];
        }
    }
}