# 🚀 Nursing API 배포 가이드

## 📋 개요
이 가이드는 `deploy_git.sh` 스크립트 실행 후 필요한 설정들을 안내합니다.

## 🎯 deploy_git.sh 실행 방법

```bash
./deploy_git.sh [git-url] [instance-ip] [keypair-name] [user]
```

### 예시
```bash
./deploy_git.sh https://github.com/username/nursing-ai.git 3.34.123.45 nursing-api-key ubuntu
```

## 🔧 배포 후 필수 설정

### 1️⃣ .env 파일 설정

SSH로 인스턴스에 접속하여 .env 파일을 수정합니다:

```bash
ssh -i nursing-api-key.pem ubuntu@3.34.123.45
cd ~/nursing-project/nursing_AI
nano .env
```

#### 필요한 환경 변수들:

```bash
# OpenAI API 키 (필수)
OPENAI_API_KEY=sk-your-openai-api-key-here

# Pinecone 벡터 데이터베이스 설정 (필수)
PINECONE_API_KEY=your-pinecone-api-key-here
PINECONE_INDEX=your-pinecone-index-name

# Naver Clova Speech API (필수)
CLOVA_SPEECH_SECRET=your-clova-secret-key

# Google Cloud 인증 파일 경로 (필수)
GOOGLE_APPLICATION_CREDENTIALS=/app/credentials/google-credentials.json
```

### 2️⃣ Google Cloud 인증 파일 업로드

로컬에서 Google Cloud 인증 파일을 서버로 업로드:

```bash
scp -i nursing-api-key.pem appc-sc-jimin-59424c1ed192.json ubuntu@3.34.123.45:~/nursing-project/nursing_AI/credentials/google-credentials.json
```

### 3️⃣ 컨테이너 재시작

설정 완료 후 컨테이너를 재시작합니다:

```bash
ssh -i nursing-api-key.pem ubuntu@3.34.123.45
cd ~/nursing-project/nursing_AI
docker-compose restart
```

### 4️⃣ 서비스 상태 확인

```bash
# 로그 확인
docker-compose logs -f

# 헬스체크
curl http://localhost:8000/health

# API 문서 확인
curl http://localhost:8000/docs
```

## 🔑 API 키 획득 방법

### OpenAI API 키
1. https://platform.openai.com/ 접속
2. API Keys 메뉴에서 새 키 생성
3. `sk-`로 시작하는 키 복사

### Pinecone API 키
1. https://www.pinecone.io/ 접속
2. API Keys 메뉴에서 키 확인
3. 인덱스 이름도 함께 확인

### Naver Clova Speech Secret
1. https://clova.ai/ 접속
2. Speech API 서비스 신청
3. Secret Key 확인

### Google Cloud 인증 파일
1. Google Cloud Console 접속
2. IAM & Admin > Service Accounts
3. JSON 키 파일 다운로드
4. 파일명을 `google-credentials.json`으로 변경

## 🌐 서비스 접속

배포 완료 후 다음 URL로 접속 가능:

- **API 서버**: http://[인스턴스-IP]:8000
- **API 문서**: http://[인스턴스-IP]:8000/docs
- **헬스체크**: http://[인스턴스-IP]:8000/health

## 🔄 업데이트 방법

코드가 업데이트된 경우:

```bash
# 1. Git에서 최신 코드 가져오기
ssh -i nursing-api-key.pem ubuntu@3.34.123.45
cd ~/nursing-project/NursingProject
git pull origin Jimin

# 2. 컨테이너 재빌드 및 재시작
cd nursing_AI
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

## 🚨 문제 해결

### 서비스가 시작되지 않는 경우
```bash
# 로그 확인
docker-compose logs

# 컨테이너 상태 확인
docker-compose ps

# 환경 변수 확인
docker-compose exec nursing-api env | grep -E "(OPENAI|PINECONE|CLOVA|GOOGLE)"
```

### API 키 오류
- .env 파일의 API 키가 올바른지 확인
- API 키에 충분한 권한이 있는지 확인
- API 사용량 한도를 초과하지 않았는지 확인

### Google Cloud 인증 오류
- `credentials/google-credentials.json` 파일이 올바른 위치에 있는지 확인
- 파일 권한이 올바른지 확인: `chmod 600 credentials/google-credentials.json`

## 📞 지원

문제가 발생하면 다음을 확인해주세요:
1. 모든 API 키가 올바르게 설정되었는지
2. Google Cloud 인증 파일이 올바른 위치에 있는지
3. Docker 컨테이너가 정상적으로 실행되고 있는지
4. 방화벽에서 8000번 포트가 열려있는지
