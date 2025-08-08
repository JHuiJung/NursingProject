# 🌐 URL 관리 시스템 사용법

## 📋 개요
이 시스템은 Unity 프로젝트에서 API 서버 URL을 중앙 집중식으로 관리할 수 있게 해주는 솔루션입니다. ngrok URL 변경이나 개발/운영 환경 전환 시 모든 파일을 수정할 필요 없이 한 곳에서 관리할 수 있습니다.

## 🗂️ 파일 구조
```
Assets/
├── Config/
│   └── APIConfig.cs          # URL 설정을 관리하는 ScriptableObject
├── Resources/
│   └── APIConfig.asset       # 실제 URL 설정 파일 (Unity에서 생성)
├── Scripts/
│   ├── URLManager.cs         # URL 관리 유틸리티 클래스
│   └── URLManagerExample.cs  # 사용 예제 스크립트
└── README_URL_Management.md  # 이 파일
```

## 🚀 초기 설정

### 1. APIConfig 에셋 생성
1. Unity 에디터에서 `Assets` → `Create` → `Nursing Project` → `API Config` 선택
2. 생성된 `APIConfig.asset` 파일을 `Assets/Resources/` 폴더로 이동
3. Inspector에서 기본 URL 설정

### 2. 기존 스크립트 수정 완료
다음 파일들이 이미 수정되어 있습니다:
- ✅ `NursingChatClient.cs`
- ✅ `ParentChatManager.cs` 
- ✅ `SttChat.cs`
- ✅ `VoiceChatClient.cs`

## 💻 사용법

### 기본 사용법
```csharp
// 현재 설정된 URL 가져오기
string chatUrl = APIConfig.Instance.ChatUrl;
string parentChatUrl = APIConfig.Instance.ParentChatUrl;

// 런타임에 URL 변경
URLManager.ChangeBaseUrl("https://your-new-domain.com");
```

### ngrok URL 변경
```csharp
// ngrok URL로 변경
URLManager.SetNgrokUrl("https://abc123.ngrok-free.app");
```

### 로컬 개발 서버로 변경
```csharp
// 로컬 서버로 변경
URLManager.SetLocalUrl();
```

### 현재 설정 확인
```csharp
// 모든 URL 로그 출력
URLManager.LogAllUrls();
```

## 🎮 런타임 URL 변경

### UI를 통한 변경
1. `URLManagerExample` 스크립트를 빈 GameObject에 추가
2. Inspector에서 UI 요소들을 연결:
   - URL 입력 필드
   - 변경 버튼
   - 로그 버튼
   - 현재 URL 표시 텍스트

### 코드를 통한 변경
```csharp
// 게임 시작 시 ngrok URL 설정
void Start()
{
    URLManager.SetNgrokUrl("https://your-ngrok-url.ngrok-free.app");
}

// 사용자 입력으로 URL 변경
public void OnUrlChangeButtonClick()
{
    string newUrl = urlInputField.text;
    URLManager.ChangeBaseUrl(newUrl);
}
```

## ⌨️ 키보드 단축키 (URLManagerExample 사용 시)
- `Ctrl + L`: 로컬 서버로 변경
- `Ctrl + G`: 현재 URL 설정 로그 출력

## 🔧 APIConfig 설정

### Inspector에서 설정 가능한 항목
- **Base URL**: 기본 서버 주소
- **Chat Endpoint**: 채팅 API 엔드포인트
- **Parent Chat Endpoint**: 부모 채팅 API 엔드포인트
- **TTS Endpoint**: TTS API 엔드포인트
- **STT Endpoint**: STT API 엔드포인트
- **Voice Chat Endpoint**: 음성 채팅 API 엔드포인트

### 기본 설정값
```
Base URL: https://2ea0c617449c.ngrok-free.app
Chat: /chat
Parent Chat: /parent_chat
TTS: /tts
STT: /clova_stt
Voice Chat: /voice_chat
```

## 🎯 장점

### ✅ 중앙 집중식 관리
- 모든 API URL을 한 곳에서 관리
- URL 변경 시 한 번만 수정하면 모든 스크립트에 적용

### ✅ 런타임 변경 가능
- 게임 실행 중에도 URL 변경 가능
- ngrok URL이 바뀔 때마다 코드 수정 불필요

### ✅ 개발/운영 환경 전환 용이
- 로컬 개발 서버와 운영 서버 간 쉬운 전환
- 환경별 설정 관리 가능

### ✅ 유효성 검사
- URL 형식 검증 기능
- 잘못된 URL 입력 방지

## 🚨 주의사항

1. **APIConfig.asset 파일 위치**: 반드시 `Assets/Resources/` 폴더에 있어야 합니다.
2. **URL 형식**: `http://` 또는 `https://`로 시작해야 합니다.
3. **에러 처리**: APIConfig를 찾을 수 없을 때 콘솔에 에러가 출력됩니다.

## 🔄 업데이트 방법

### 새로운 API 엔드포인트 추가
1. `APIConfig.cs`에 새로운 엔드포인트 변수 추가
2. 해당 프로퍼티 추가
3. `APIConfig.asset`에서 값 설정

### 기존 스크립트에 적용
```csharp
// 기존
string url = "https://server.com/api/endpoint";

// 변경 후
string url = APIConfig.Instance.NewEndpointUrl;
```

## 📞 지원
문제가 발생하거나 추가 기능이 필요하면 개발팀에 문의해주세요.

