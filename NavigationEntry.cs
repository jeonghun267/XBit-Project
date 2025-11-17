// NavigationEntry.cs

using System;

namespace XBit
{
    /// <summary>
    /// 페이지 네비게이션 정보를 저장하는 클래스
    /// </summary>
    public class NavigationEntry
    {
        /// <summary>
        /// 네비게이션할 페이지의 타입
        /// </summary>
        public Type PageType { get; set; }
        
        /// <summary>
        /// 페이지에 전달할 매개변수
        /// </summary>
        public object Parameter { get; set; }

        /// <summary>
        /// NavigationEntry 생성자
        /// </summary>
        /// <param name="pageType">페이지 타입</param>
        /// <param name="parameter">전달할 매개변수 (선택)</param>
        public NavigationEntry(Type pageType, object parameter = null)
        {
            PageType = pageType;
            Parameter = parameter;
        }
    }
}