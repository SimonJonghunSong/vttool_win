# 🎨 FileBrowserControl 사용 가이드

TestConnectSuccessDialog와 동일한 다크 테마 색감을 가진 파일 브라우저 컴포넌트입니다.

## 🎯 주요 특징

- **🎨 다크 테마**: TestConnectSuccessDialog와 완전히 동일한 색상 스킴
- **📁 파일 선택**: Browse 버튼으로 파일 다이얼로그 열기
- **📄 실시간 미리보기**: 텍스트 파일 내용 자동 표시
- **📊 파일 정보**: 크기, 수정일시 자동 업데이트
- **🎯 이벤트 기반**: FileSelected, FileContentChanged 이벤트
- **🔧 커스터마이징**: FileFilter, DialogTitle 속성으로 설정

## 🎨 색상 스킴

```csharp
메인 배경: Color.FromArgb(0x2F, 0x2F, 0x37) // #2F2F37
패널 배경: Color.FromArgb(0x46, 0x46, 0x52) // #464652
텍스트: Color.FromArgb(0xD9, 0xD9, 0xD9)    // 밝은 회색
강조 색상: Color.FromArgb(0xF3, 0x71, 0x21) // #F37121 (오렌지)
```

## 🚀 기본 사용법

### 1. 기본 컴포넌트 추가

```csharp
var fileBrowser = new DarkFileBrowserControl
{
    Location = new Point(10, 10),
    Size = new Size(800, 600),
    FileFilter = "Text files (*.txt)|*.txt|C# files (*.cs)|*.cs|All files (*.*)|*.*",
    DialogTitle = "파일을 선택하세요"
};

// 폼에 추가
this.Controls.Add(fileBrowser);
```

### 2. 이벤트 등록

```csharp
// 파일 선택 이벤트
fileBrowser.FileSelected += (sender, e) => {
    Console.WriteLine($"선택된 파일: {e.FilePath}");
    Console.WriteLine($"파일 크기: {e.FileInfo.Length} bytes");
    Console.WriteLine($"수정일: {e.FileInfo.LastWriteTime}");
};

// 파일 내용 변경 이벤트
fileBrowser.FileContentChanged += (sender, e) => {
    Console.WriteLine($"내용 길이: {e.Content.Length} characters");
    // 실시간 파일 내용 처리
};
```

### 3. 프로그래밍 방식으로 파일 로드

```csharp
// 특정 파일 직접 로드
fileBrowser.LoadFile(@"C:\path\to\your\file.txt");

// 선택된 파일 경로 가져오기
string selectedPath = fileBrowser.SelectedFilePath;

// 파일 내용 가져오기
string content = fileBrowser.FileContent;
```

## 🛠️ 고급 사용법

### 1. 메인 폼에 통합

```csharp
using AndroidDeviceVerificationTool.Examples;

// Form1에 파일 브라우저 추가
var fileBrowser = MainFormIntegrationExample.AddFileBrowserToForm(
    this, 
    new Point(50, 50), 
    new Size(800, 600)
);
```

### 2. 팝업 다이얼로그로 사용

```csharp
// 팝업으로 파일 브라우저 열기
string selectedFile = MainFormIntegrationExample.ShowFileBrowserDialog(
    this, 
    "설정 파일 선택"
);

if (!string.IsNullOrEmpty(selectedFile))
{
    Console.WriteLine($"선택된 파일: {selectedFile}");
}
```

### 3. TabControl에 추가

```csharp
// 탭에 파일 브라우저 추가
var fileBrowser = MainFormIntegrationExample.AddFileBrowserTab(
    tabControl1, 
    "파일 브라우저"
);
```

## 🔧 속성 설정

```csharp
fileBrowser.FileFilter = "JSON files (*.json)|*.json|Config files (*.config)|*.config|All files (*.*)|*.*";
fileBrowser.DialogTitle = "구성 파일을 선택하세요";
```

### 지원하는 파일 필터 예제

```csharp
// 텍스트 파일만
"Text files (*.txt)|*.txt"

// 코드 파일들
"C# files (*.cs)|*.cs|JavaScript files (*.js)|*.js|All files (*.*)|*.*"

// 구성 파일들
"Config files (*.config)|*.config|JSON files (*.json)|*.json|XML files (*.xml)|*.xml"

// 모든 파일
"All files (*.*)|*.*"
```

## 📄 지원하는 파일 형식

### 텍스트 파일 (미리보기 지원)
- `.txt`, `.cs`, `.xml`, `.json`, `.html`, `.css`, `.js`
- `.md`, `.yml`, `.yaml`, `.ini`, `.cfg`, `.config`
- `.log`, `.csv`, `.sql`, `.py`, `.java`, `.cpp`, `.h`

### 바이너리 파일
- 바이너리 파일은 "[바이너리 파일 - 미리보기 불가]" 메시지 표시
- 파일 정보 (크기, 수정일)는 정상적으로 표시

## 🎛️ 레이아웃 구조

```
┌─────────────────────────────────────────────────┐
│ 📁 File Browser                                 │
│ File Path: [텍스트박스]              [Browse]   │
│ Size: 1.25 KB    Modified: 2024-01-01 12:00:00 │
├─────────────────────────────────────────────────┤
│ 📄 File Content Preview                         │
│ ┌─────────────────────────────────────────────┐ │
│ │ 파일 내용이 여기에 표시됩니다...            │ │
│ │                                             │ │
│ │                                             │ │
│ └─────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

## 🎈 예제 실행

### DarkFileBrowserExample 독립 실행
```bash
# 조건부 컴파일 플래그로 독립 실행 가능
dotnet build -p:DefineConstants=STANDALONE_FILEBROWSER
dotnet run
```

### 기존 프로젝트에서 테스트
```csharp
// Form1에서 테스트 버튼 추가
var testButton = new Button
{
    Text = "파일 브라우저 테스트",
    Location = new Point(10, 10),
    Size = new Size(150, 30)
};

testButton.Click += (s, e) => {
    var example = new DarkFileBrowserExample();
    example.Show();
};

this.Controls.Add(testButton);
```

## 💡 활용 아이디어

1. **설정 파일 편집기**: JSON, XML, Config 파일 편집
2. **로그 뷰어**: 로그 파일 실시간 모니터링
3. **코드 뷰어**: 소스 코드 파일 빠른 미리보기
4. **문서 브라우저**: Markdown, 텍스트 문서 조회

## 🔍 문제 해결

### 한글 폰트 문제
```csharp
// Noto Sans KR 폰트가 없는 경우 대체 폰트 사용
Font = new Font("맑은 고딕", 10F, FontStyle.Regular);
```

### 큰 파일 처리
- 1MB 이상 파일은 자동으로 경고 메시지 표시
- 사용자 확인 후 로드 진행

### 메모리 관리
- 컴포넌트 Dispose 시 자동으로 이벤트 정리
- 파일 스트림 자동 해제

---

✨ **TestConnectSuccessDialog와 완벽하게 통일된 다크 테마 디자인으로 일관성 있는 UI를 제공합니다!** 