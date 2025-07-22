# ?? 스케줄 프로그램 관리자

Windows Forms 기반의 프로그램 관리 시스템입니다. 다양한 프로그램들을 폴더별로 체계적으로 관리하고 즉시 실행할 수 있습니다.

## ?? 주요 기능

### ??? 프로그램 관리
- **폴더별 프로그램 구분**: 각 프로그램을 독립적인 폴더로 관리
- **자동 스캔**: Programs 폴더의 모든 CS 파일을 자동으로 검색
- **메모 시스템**: 각 CS 파일의 `memo` 변수에서 설명 자동 추출
- **즉시 실행**: 그리드에서 바로 프로그램 실행 가능

### ?? 사용자 인터페이스
- **현대적인 디자인**: 깔끔하고 직관적인 UI
- **그리드 기반**: 프로그램 정보를 표 형태로 명확하게 표시
- **색상 구분**: 프로그램별로 다른 배경색으로 시각적 구분
- **실시간 상태**: 하단 상태바로 현재 상태 표시

## ?? 프로젝트 구조

```
scheduleProgram/
├── ?? Models/                 # 데이터 모델
│   └── ProgramInfo.cs         # 프로그램 정보 클래스
├── ?? Services/               # 비즈니스 로직
│   └── ProgramManager.cs      # 프로그램 관리 서비스
├── ?? Programs/               # 실행할 프로그램들
│   └── ?? lottoApp/           # 로또 관련 프로그램
│       ├── LottoNumberGenerator.cs
│       ├── LottoStatistics.cs
│       ├── LottoChecker.cs
│       └── getHistory.cs
├── UITheme.cs                 # UI 테마 관리
├── UIComponents.cs            # UI 컴포넌트 팩토리
├── MainForm.cs                # 메인 폼 로직
├── MainForm.Designer.cs       # 메인 폼 디자인
└── Program.cs                 # 진입점

```

## ?? UI 컴포넌트

### ?? 메인 그리드
| 컬럼명 | 설명 |
|--------|------|
| 프로그램명 | CS 파일이 속한 폴더 이름 |
| CS파일명 | 실행할 CS 파일의 이름 |
| 메모 | CS 파일 내 memo 변수의 값 |
| 즉시실행 | 프로그램을 바로 실행하는 버튼 |

### ?? 주요 기능 버튼
- **?? 새로고침**: 프로그램 목록을 다시 스캔
- **?? 폴더 추가**: 새로운 프로그램 폴더 생성

## ?? 시작하기

### 필요 조건
- .NET 8.0 이상
- Windows Forms 지원 환경

### 설치 및 실행
1. 저장소 클론
```bash
git clone https://github.com/minux0409/scheduleProgram.git
cd scheduleProgram
```

2. 빌드 및 실행
```bash
dotnet build
dotnet run
```

### 새 프로그램 추가 방법
1. "?? 폴더 추가" 버튼을 클릭하여 새 프로그램 폴더 생성
2. 생성된 폴더에 CS 파일 추가
3. CS 파일에 다음 형식으로 memo 변수 포함:
```csharp
private string memo = "프로그램 설명";
```
4. "?? 새로고침" 버튼으로 목록 업데이트

## ?? CS 파일 작성 예시

```csharp
using System;
using System.Windows.Forms;

namespace scheduleProgram.lottoApp
{
    public class SampleProgram
    {
        private string memo = "샘플 프로그램 설명";
        
        public void Execute()
        {
            MessageBox.Show("프로그램이 실행되었습니다!", "샘플 프로그램");
        }
    }
}
```

## ?? UI 테마

### 색상 팔레트
- **Primary**: `#2980B9` (블루)
- **Secondary**: `#3498DB` (라이트 블루)  
- **Accent**: `#E67E22` (오렌지)
- **Background**: `#ECF0F1` (라이트 그레이)
- **Error**: `#E74C3C` (레드)

### 폰트
- **기본**: 맑은 고딕 9pt
- **제목**: 맑은 고딕 10pt Bold
- **대제목**: 맑은 고딕 12pt Bold

## ??? 기술 스택

- **Framework**: .NET 8.0
- **UI**: Windows Forms
- **Language**: C# 12.0
- **Architecture**: Model-Service-View 패턴

## ?? 향후 계획

- [ ] 프로그램 스케줄링 기능
- [ ] 로그 시스템 추가
- [ ] 설정 파일 지원
- [ ] 프로그램 실행 히스토리
- [ ] 다크 모드 지원

## ?? 기여하기

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## ?? 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다. 자세한 내용은 `LICENSE` 파일을 참조하세요.

## ????? 개발자

- **minux0409** - *Initial work* - [GitHub](https://github.com/minux0409)

---

? 이 프로젝트가 도움이 되었다면 Star를 눌러주세요!