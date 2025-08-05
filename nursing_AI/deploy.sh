#!/bin/bash

# AWS 인스턴스 배포 스크립트
# 사용법: ./deploy.sh [dev|prod]

set -e  # 에러 발생 시 스크립트 중단

ENVIRONMENT=${1:-prod}
IMAGE_NAME="nursing-api"
CONTAINER_NAME="nursing-api-container"

echo "🚀 Nursing API 배포를 시작합니다..."
echo "📋 환경: $ENVIRONMENT"

# 1. Docker 설치 확인
if ! command -v docker &> /dev/null; then
    echo "❌ Docker가 설치되지 않았습니다. 설치를 진행합니다..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker $USER
    echo "✅ Docker 설치 완료. 시스템을 재시작하거나 새 터미널을 열어주세요."
    exit 1
fi

# 2. Docker Compose 설치 확인
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose가 설치되지 않았습니다. 설치를 진행합니다..."
    sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
    echo "✅ Docker Compose 설치 완료"
fi

# 3. 환경 변수 파일 확인
if [ ! -f .env ]; then
    echo "❌ .env 파일이 없습니다. 환경 변수를 설정해주세요."
    echo "필요한 환경 변수:"
    echo "  - OPENAI_API_KEY"
    echo "  - PINECONE_API_KEY"
    echo "  - PINECONE_INDEX"
    echo "  - CLOVA_SPEECH_SECRET"
    exit 1
fi

# 4. Google Cloud 인증 파일 확인
if [ ! -f credentials/google-credentials.json ]; then
    echo "❌ Google Cloud 인증 파일이 없습니다."
    echo "credentials/google-credentials.json 파일을 생성해주세요."
    exit 1
fi

# 5. 기존 컨테이너 중지 및 제거
echo "🔄 기존 컨테이너를 정리합니다..."
docker-compose down --remove-orphans || true
docker stop $CONTAINER_NAME || true
docker rm $CONTAINER_NAME || true

# 6. 이미지 빌드
echo "🔨 Docker 이미지를 빌드합니다..."
docker-compose build --no-cache

# 7. 컨테이너 실행
if [ "$ENVIRONMENT" = "dev" ]; then
    echo "🛠️ 개발 환경으로 실행합니다..."
    docker-compose --profile dev up -d nursing-api-dev
else
    echo "🚀 프로덕션 환경으로 실행합니다..."
    docker-compose up -d nursing-api
fi

# 8. 헬스체크
echo "🏥 서비스 상태를 확인합니다..."
sleep 10

if curl -f http://localhost:8000/health > /dev/null 2>&1; then
    echo "✅ 서비스가 정상적으로 실행되었습니다!"
    echo "🌐 서버 주소: http://$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4):8000"
    echo "📖 API 문서: http://$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4):8000/docs"
else
    echo "❌ 서비스 실행에 실패했습니다. 로그를 확인해주세요."
    docker-compose logs
    exit 1
fi

# 9. 로그 확인
echo "📋 최근 로그:"
docker-compose logs --tail=20

echo "🎉 배포가 완료되었습니다!" 