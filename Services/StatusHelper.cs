using System;
using System.Linq;

namespace XBit.Services
{
    // 상태 비교를 중앙화하여 인코딩/지역화 문제를 완화합니다.
    public static class StatusHelper
    {
        private static readonly string[] CompletedKeywords = new[] { "완료", "완료됨", "완료된", "complete", "completed", "done" };
        private static readonly string[] InProgressKeywords = new[] { "진행", "진행중", "진행중임", "inprogress", "in progress", "progress" };

        public static bool IsCompleted(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            var s = status.Trim().ToLowerInvariant();
            // 키워드 포함 검사(단순, 안전)
            return CompletedKeywords.Any(k => s.Contains(k));
        }

        public static bool IsInProgress(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            var s = status.Trim().ToLowerInvariant();
            return InProgressKeywords.Any(k => s.Contains(k));
        }
    }
}