# 통합 간호학 API 서버

세 개의 개별 API를 하나로 통합한 FastAPI 서버입니다.

## 🚀 실행 방법

```bash
# 1. 필요한 패키지 설치
pip install fastapi uvicorn python-multipart google-cloud-texttospeech requests python-dotenv

# 2. 환경 변수 설정 (.env 파일)
CLOVA_SPEECH_SECRET=your_clova_api_key_here
GOOGLE_APPLICATION_CREDENTIALS=path/to/your/google-credentials.json

# 3. 서버 실행
python run_unified_api.py
```

## 📋 API 엔드포인트

### 1. STT 평가 API (`/clova_stt`)
**POST** `/clova_stt`

음성 파일을 텍스트로 변환하고 AI 평가를 제공합니다.

**요청:**
- `question` (Form): 평가할 질문
- `audio` (File): 음성 파일

**응답:**
```json
{
  "transcript": "변환된 텍스트",
  "feedback": "AI 평가 피드백",
  "is_correct": "정답 여부",
  "question": "원본 질문"
}
```

### 2. 부모 채팅 API (`/parent_chat`)
**POST** `/parent_chat`

부모의 음성 응답을 처리하고 키워드 누락 여부를 확인합니다.

**요청:**
- `session_id` (Form): 세션 ID
- `question_id` (Form): 질문 ID (Q1~Q5)
- `audio` (File): 음성 파일

**응답:**
```json
{
  "transcript": "변환된 텍스트",
  "followup_needed": false,
  "missing_keywords": [],
  "followup_audio_base64": "후속 질문 음성 (base64)",
  "missing_keywords": ["누락된 키워드들"]
}
```

### 3. 부모 채팅 후속 응답 (`/parent_chat/followup`)
**POST** `/parent_chat/followup`

후속 응답을 기존 응답에 추가합니다.

**요청:**
- `session_id` (Form): 세션 ID
- `question_id` (Form): 질문 ID
- `audio` (File): 후속 음성 파일

**응답:**
```json
{
  "transcript": "후속 응답 텍스트",
  "updated_full_response": "전체 누적 응답",
  "followup_registered": true
}
```

### 4. 부모 채팅 요약 (`/parent_chat/summary`)
**POST** `/parent_chat/summary`

전체 대화를 요약하고 AI 피드백을 제공합니다.

**요청:**
- `session_id` (Form): 세션 ID

**응답:**
```json
{
  "session_id": "세션 ID",
  "feedback": "AI 피드백"
}
```

### 5. 텍스트를 음성으로 변환 (`/tts`)
**POST** `/tts`

텍스트를 음성으로 변환합니다.

**요청:**
- `text` (Form): 변환할 텍스트

**응답:**
```json
{
  "audio_base64": "음성 데이터 (base64)"
}
```

### 6. RAG 기반 채팅 (`/chat`)
**POST** `/chat`

RAG 기반으로 질문에 답변합니다.

**요청:**
```json
{
  "session_id": "세션 ID",
  "question": "질문"
}
```

**응답:**
```json
{
  "answer": "AI 답변",
  "correct_count": 0,
  "incorrect_count": 0,
  "total_questions": 0,
  "score_percentage": 0.0
}
```

## 🔧 설정

### 환경 변수
- `CLOVA_SPEECH_SECRET`: Clova Speech API 키
- `GOOGLE_APPLICATION_CREDENTIALS`: Google Cloud 서비스 계정 키 파일 경로

### 질문 설정
`QUESTIONS` 배열에서 부모 채팅의 질문과 필수 키워드를 수정할 수 있습니다.

## 📖 API 문서

서버 실행 후 다음 URL에서 자동 생성된 API 문서를 확인할 수 있습니다:
- Swagger UI: http://localhost:8000/docs
- ReDoc: http://localhost:8000/redoc

## 🛠️ 개발

### 코드 구조
```
unified_nursing_api.py  # 메인 API 서버
run_unified_api.py      # 실행 스크립트
nursing_llm.py          # AI 응답 처리 (기존 파일)
```

### 주요 기능
1. **공통 유틸리티**: STT, TTS, 키워드 검사 함수
2. **세션 관리**: 부모 채팅 세션별 응답 저장
3. **에러 처리**: 각 API별 적절한 에러 응답
4. **로깅**: 디버깅을 위한 콘솔 로그

## 🔄 기존 API와의 호환성

모든 기존 API의 요청/응답 형식을 그대로 유지하므로 기존 클라이언트 코드를 수정할 필요가 없습니다. 