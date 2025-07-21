using System;
using System.Drawing;

namespace AndroidDeviceVerificationTool.Design
{
    /// <summary>
    /// Material Design 3 기반 디자인 시스템
    /// 모든 색상, 폰트, 사이즈를 중앙에서 관리
    /// </summary>
    public static class MaterialDesign
    {
        // 🎨 Material Design 3 색상 팔레트
        public static class Colors
        {
            // Primary Colors
            public static readonly Color Primary = Color.FromArgb(243, 113, 33);      // #F37121 (주황색)
            public static readonly Color OnPrimary = Color.FromArgb(24, 24, 29);      // #18181D (어두운 텍스트)
            
            // Success Colors
            public static readonly Color Success = Color.FromArgb(0, 255, 166);       // #00FFA6 (성공 녹색)
            public static readonly Color SuccessDark = Color.FromArgb(50, 205, 50);   // 진한 성공 녹색
            public static readonly Color SuccessLight = Color.FromArgb(144, 238, 144); // 연한 성공 녹색
            
            // Info Colors
            public static readonly Color Info = Color.FromArgb(80, 202, 255);         // #50CAFF (정보 파란색)
            public static readonly Color InfoDark = Color.FromArgb(30, 144, 255);     // 진한 정보 파란색
            
            // Error Colors
            public static readonly Color Error = Color.FromArgb(255, 64, 64);         // #FF4040 (오류 빨간색)
            public static readonly Color ErrorDark = Color.FromArgb(220, 20, 60);     // 진한 오류 빨간색
            public static readonly Color ErrorLight = Color.FromArgb(255, 100, 100);  // 연한 오류 빨간색
            
            // Warning Colors
            public static readonly Color Warning = Color.FromArgb(255, 244, 64);      // 경고 노란색
            public static readonly Color WarningDark = Color.FromArgb(255, 140, 0);   // 주황색 경고
            public static readonly Color WarningAmber = Color.FromArgb(210, 181, 141); // #D2B58D
            
            // Surface Colors
            public static readonly Color Surface = Color.FromArgb(30, 30, 30);        // #1E1E1E (메인 배경)
            public static readonly Color SurfaceContainer = Color.FromArgb(47, 47, 55); // #2F2F37 (컨테이너)
            public static readonly Color SurfaceCard = Color.FromArgb(87, 87, 101);   // #575765 (카드)
            public static readonly Color SurfacePanel = Color.FromArgb(70, 70, 82);   // #464652 (패널)
            public static readonly Color SurfaceDark = Color.FromArgb(50, 50, 58);    // 어두운 표면
            
            // Text Colors
            public static readonly Color OnSurface = Color.FromArgb(217, 217, 217);   // #D9D9D9 (기본 텍스트)
            public static readonly Color OnSurfaceVariant = Color.FromArgb(150, 150, 150); // 보조 텍스트
            public static readonly Color OnSurfaceDim = Color.FromArgb(120, 120, 120); // 흐린 텍스트
            public static readonly Color TextWhite = Color.White;                     // 흰색 텍스트
            public static readonly Color TextBlack = Color.Black;                     // 검은색 텍스트
            
            // Status Colors (테스트 상태용)
            public static readonly Color StatusPass = Success;                        // PASS
            public static readonly Color StatusFail = Error;                          // FAIL
            public static readonly Color StatusRunning = Warning;                     // RUNNING
            public static readonly Color StatusReady = SuccessLight;                  // READY
            public static readonly Color StatusCompleted = SuccessDark;               // COMPLETED
            public static readonly Color StatusPending = Color.FromArgb(169, 169, 169); // PENDING
            public static readonly Color StatusTimeout = WarningDark;                 // TIMEOUT
            
            // Border & Outline
            public static readonly Color Outline = Color.FromArgb(121, 116, 126);     // #79747E
            public static readonly Color OutlineVariant = Color.FromArgb(255, 255, 255, 26); // 10% 흰색
            
            // Transparent
            public static readonly Color Transparent = Color.Transparent;
        }
        
        // 📝 폰트 설정
        public static class Fonts
        {
            // 기본 폰트 패밀리 (시스템에 없는 Noto Sans KR 대신 Arial 사용)
            public const string Primary = "Arial";
            public const string Secondary = "맑은 고딕";  // 한글 지원용
            
            // 폰트 크기
            public static class Size
            {
                public const float Headline = 20F;        // 헤드라인 (탭 제목 등)
                public const float Title = 16F;           // 타이틀 (디바이스 이름, 섹션 헤더)
                public const float Body = 14F;            // 본문 (일반 텍스트)
                public const float BodySmall = 12F;       // 작은 본문 (테스트 항목)
                public const float Label = 10F;           // 라벨 (상태 표시)
                public const float Caption = 8F;          // 캡션 (로그)
                
                // 특별한 크기
                public const float Icon = 40F;            // 아이콘
                public const float IconSmall = 24F;       // 작은 아이콘
                public const float Debug = 18F;           // 디버그용
            }
            
            // 폰트 스타일 헬퍼
            public static Font GetFont(float size, FontStyle style = FontStyle.Regular)
            {
                return new Font(Primary, size, style);
            }
            
            public static Font GetKoreanFont(float size, FontStyle style = FontStyle.Regular)
            {
                return new Font(Secondary, size, style);
            }
        }
        
        // 📐 사이즈 및 간격
        public static class Spacing
        {
            // 기본 간격
            public const int ExtraSmall = 4;              // 4px
            public const int Small = 8;                   // 8px
            public const int Medium = 16;                 // 16px
            public const int Large = 24;                  // 24px
            public const int ExtraLarge = 32;             // 32px
            
            // 컴포넌트별 사이즈
            public const int DeviceWidth = 312;           // 디바이스 카드 너비
            public const int DeviceSpacing = 332;         // 디바이스 간 간격 (20px 여백 포함)
            public const int DeviceHeight = 600;          // 디바이스 카드 높이
            public const int HeaderHeight = 50;           // 헤더 높이
            public const int TabHeight = 48;              // 탭 높이
            public const int TopBarHeight = 64;           // 상단 바 높이
            
            // 테스트 항목 사이즈
            public const int TestItemHeight = 28;         // Auto Test 항목 높이
            public const int ManualTestItemHeight = 30;   // Manual Test 항목 높이
            public const int StatusLabelWidth = 110;      // 상태 라벨 너비
            public const int StatusLabelHeight = 22;      // 상태 라벨 높이
            
            // 애니메이션
            public const int AnimationFastMs = 50;        // 빠른 애니메이션 (클릭)
            public const int AnimationNormalMs = 150;     // 일반 애니메이션 (호버)
            public const int AnimationSlowMs = 300;       // 느린 애니메이션
            
            // 라운딩
            public const int CornerSmall = 4;             // 작은 모서리
            public const int CornerMedium = 8;            // 중간 모서리
            public const int CornerLarge = 16;            // 큰 모서리
        }
        
        // 🖱️ 상호작용 색상 (호버, 클릭 등)
        public static class Interactive
        {
            public static readonly Color HoverLight = Color.FromArgb(120, 120, 120);     // 밝은 호버
            public static readonly Color HoverMedium = Color.FromArgb(100, 100, 100);    // 중간 호버
            public static readonly Color HoverDark = Color.FromArgb(80, 80, 80);         // 어두운 호버
            public static readonly Color ClickLight = Color.FromArgb(200, 200, 200);     // 밝은 클릭
            public static readonly Color ClickMedium = Color.FromArgb(150, 150, 150);    // 중간 클릭
            public static readonly Color DisabledBackground = Color.FromArgb(60, 60, 68); // 비활성화 배경
            public static readonly Color DisabledText = Color.FromArgb(100, 100, 100);   // 비활성화 텍스트
        }
        
        // 📱 디바이스 상태별 색상
        public static class Device
        {
            public static readonly Color Connected = Colors.Success;      // 연결됨
            public static readonly Color Disconnected = Colors.OnSurface; // 연결 안됨
            public static readonly Color ConnectedText = Colors.Success;  // 연결된 디바이스 텍스트
            public static readonly Color DisconnectedText = Colors.ErrorLight; // 연결 안된 디바이스 텍스트
        }
        
        // 🔧 유틸리티 메서드들
        public static class Utils
        {
            /// <summary>
            /// 투명도를 적용한 색상 생성
            /// </summary>
            public static Color WithOpacity(Color color, byte alpha)
            {
                return Color.FromArgb(alpha, color.R, color.G, color.B);
            }
            
            /// <summary>
            /// 퍼센트로 투명도 적용
            /// </summary>
            public static Color WithOpacity(Color color, float opacity)
            {
                byte alpha = (byte)(opacity * 255);
                return WithOpacity(color, alpha);
            }
            
            /// <summary>
            /// 안전한 중앙 위치 계산
            /// </summary>
            public static Point SafeCenterPosition(int containerWidth, int itemWidth, int y, int minX = 10)
            {
                int x = Math.Max(minX, (containerWidth - itemWidth) / 2);
                return new Point(x, y);
            }
            
            /// <summary>
            /// 테스트 상태에 따른 색상 반환
            /// </summary>
            public static Color GetStatusColor(string status)
            {
                return status.ToUpper() switch
                {
                    "PASS" => Colors.StatusPass,
                    "FAIL" => Colors.StatusFail,
                    "RUNNING" => Colors.StatusRunning,
                    "READY" => Colors.StatusReady,
                    "COMPLETED" => Colors.StatusCompleted,
                    "PENDING" => Colors.StatusPending,
                    "TIMEOUT" => Colors.StatusTimeout,
                    "WARNING" or "WARN" => Colors.Warning,
                    "ERROR" => Colors.Error,
                    "TEST" => Colors.Info,
                    _ => Colors.SurfaceCard
                };
            }
            
            /// <summary>
            /// 상태에 따른 텍스트 색상 (대비를 위해)
            /// </summary>
            public static Color GetStatusTextColor(string status)
            {
                return status.ToUpper() switch
                {
                    "PASS" or "READY" or "RUNNING" or "WARNING" or "TEST" => Colors.TextBlack,
                    _ => Colors.TextWhite
                };
            }
        }
    }
} 