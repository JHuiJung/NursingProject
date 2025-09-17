# 🚀 간단 배포 가이드 - Nginx 없이!

## 1. 준비물 체크리스트 ✅

### 로컬 컴퓨터에 필요한 것들:
- [ ] **Git 저장소 URL** (예: https://github.com/username/NursingProject.git)
- [ ] **AWS EC2 키페어 파일** (예: nursing-api-key.pem)
- [ ] **EC2 인스턴스 IP** (예: 3.34.123.45)

### API 키들 (배포 후 설정):
- [ ] OpenAI API Key
- [ ] Pinecone API Key  
- [ ] Naver Clova Speech Secret
- [ ] Google Cloud 인증 파일

## 2. 한 방에 배포하기 🎯

```bash
# 1. 실행 권한 부여 (최초 1회만)
chmod +x deploy_optimized.sh

# 2. 배포 실행 (이것만 치면 끝!)
./deploy_optimized.sh https://github.com/your-username/NursingProject.git 3.34.123.45 nursing-api-key

# 파라미터 설명:
# 1번째: Git 저장소 URL
# 2번째: EC2 IP 주소  
# 3번째: 키페어 파일명 (.pem 제외)
```

## 3. 배포 완료! 🎉

배포가 성공하면 이런 화면이 나타납니다:
```
✅ 운영 서버가 성공적으로 시작되었습니다!
🌐 서버 주소: http://3.34.123.45:8000
📖 API 문서: http://3.34.123.45:8000/docs
🚀 스트리밍 채팅: http://3.34.123.45:8000/chat/stream
🎙️ 스트리밍 음성: http://3.34.123.45:8000/voice_chat/stream
```

## 4. API 키 설정 (필수!) 🔑

```bash
# EC2 접속
ssh -i nursing-api-key.pem ubuntu@3.34.123.45

# 설정 파일 수정
cd ~/nursing-project/NursingProject/nursing_AI
nano .env

# 아래 내용을 실제 키로 수정:
OPENAI_API_KEY=sk-proj-실제키입력
PINECONE_API_KEY=실제키입력
CLOVA_SPEECH_SECRET=실제키입력
# 저장: Ctrl+X → Y → Enter
```

## 5. Google Cloud 파일 업로드 📁

```bash
# 로컬에서 실행 (별도 터미널)
scp -i nursing-api-key.pem appc-sc-jimin-59424c1ed192.json ubuntu@3.34.123.45:~/nursing-project/NursingProject/nursing_AI/credentials/google-credentials.json
```

## 6. 서버 재시작 🔄

```bash
# EC2에서 서버 재시작
cd ~/nursing-project/NursingProject/nursing_AI  
sudo pkill -f gunicorn || true
source venv/bin/activate
nohup gunicorn unified_nursing_api_optimized:app -w 4 -k uvicorn.workers.UvicornWorker -b 0.0.0.0:8000 > server.log 2>&1 &
```

## 7. 테스트 🧪

```bash
# API 작동 확인
curl http://3.34.123.45:8000/health

# 성공하면 이런 응답:
{"status":"healthy","message":"Nursing API is running"}
```

## 🚨 문제 해결

### 연결이 안될 때:
1. **EC2 보안 그룹** 8000번 포트 열렸는지 확인
2. **키페어 권한** `chmod 400 nursing-api-key.pem`
3. **서버 로그 확인** `tail -f server.log`

### API 키 오류:
```bash
# EC2에서 로그 확인
cd ~/nursing-project/NursingProject/nursing_AI
tail -20 server.log

# .env 파일 다시 확인
cat .env
```

## ⚡ 주요 특징

- **Nginx 안 씀**: 복잡한 설정 없이 바로 작동
- **8000번 포트**: 직접 FastAPI 서버 접근
- **자동 배포**: Git에서 최신 코드 자동 받기
- **스트리밍 최적화**: 3-5배 빠른 응답
- **기존 호환**: Unity 클라이언트 그대로 사용

## 🎯 Unity에서 사용

Unity의 APIConfig에서 baseUrl을 변경:
```csharp
// APIConfig.cs에서
private string baseUrl = "http://3.34.123.45:8000";
```

끝! 정말 간단하죠? 😊
