// XBit/NavigationEntry.cs (XBit 네임스페이스에 새 파일 생성)
using System;

namespace XBit
{
    // 네비게이션 기록에 저장될 항목
    public class NavigationEntry
    {
        public Type PageType { get; set; } // PageHome, PageAssignments 등 페이지의 타입
        public object Parameter { get; set; } // 필터링 인수 ("DueToday" 등)

        public NavigationEntry(Type pageType, object parameter)
        {
            PageType = pageType;
            Parameter = parameter;
        }
    }
}