#!/bin/bash

# Git 기반 AWS 인스턴스 배포 스크립트
# 사용법: ./deploy_git.sh [git-url] [instance-ip] [keypair-name]

set -e

GIT_URL=${1}
INSTANCE_IP=${2}
KEYPAIR_NAME=${3:-nursing-api-key}
INSTANCE_USER=${4:-ubuntu}

if [ -z "$GIT_URL" ] || [ -z "$INSTANCE_IP" ]; then
    echo "❌ Git URL과 인스턴스 IP를 입력해주세요."
    echo "사용법: ./deploy_git.sh [git-url] [instance-ip] [keypair-name] [user]"
    echo "예시: ./deploy_git.sh https://github.com/username/nursing-ai.git 3.34.123.45 nursing-api-key ubuntu"
    exit 1
fi

KEYPAIR_FILE="${KEYPAIR_NAME}.pem"

echo "🚀 Git 기반 Nursing API 배포를 시작합니다..."
echo "📋 Git URL: $GIT_URL"
echo "🌐 인스턴스: $INSTANCE_USER@$INSTANCE_IP"
echo "🔑 키페어: $KEYPAIR_NAME"

# 1. 키페어 파일 확인
if [ ! -f "$KEYPAIR_FILE" ]; then
    echo "❌ 키페어 파일이 없습니다: $KEYPAIR_FILE"
    exit 1
fi

# 2. 키페어 파일 권한 설정
chmod 400 "$KEYPAIR_FILE"

# 3. 인스턴스 연결 테스트
echo "🔍 인스턴스 연결을 테스트합니다..."
if ! ssh -i "$KEYPAIR_FILE" -o ConnectTimeout=10 -o StrictHostKeyChecking=no "$INSTANCE_USER@$INSTANCE_IP" "echo '연결 성공'" 2>/dev/null; then
    echo "❌ 인스턴스에 연결할 수 없습니다."
    exit 1
fi

# 4. 원격 서버에서 배포 실행
echo "🚀 원격 서버에서 Git 배포를 실행합니다..."
ssh -i "$KEYPAIR_FILE" "$INSTANCE_USER@$INSTANCE_IP" << EOF
    echo "🔧 시스템 패키지를 업데이트합니다..."
    sudo apt-get update -y
    
    echo "🐳 Docker를 설치합니다..."
    if ! command -v docker &> /dev/null; then
        curl -fsSL https://get.docker.com -o get-docker.sh
        sudo sh get-docker.sh
        sudo usermod -aG docker \$USER
        echo "✅ Docker 설치 완료"
    fi
    
    echo "🔧 Docker Compose를 설치합니다..."
    if ! command -v docker-compose &> /dev/null; then
        sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-\$(uname -s)-\$(uname -m)" -o /usr/local/bin/docker-compose
        sudo chmod +x /usr/local/bin/docker-compose
        echo "✅ Docker Compose 설치 완료"
    fi
    
    echo "📁 프로젝트 디렉토리를 생성합니다..."
    mkdir -p ~/nursing-project
    cd ~/nursing-project
    
    echo "📥 Git 저장소를 클론합니다..."
    if [ -d "NursingProject" ]; then
        echo "🔄 기존 저장소를 업데이트합니다..."
        cd NursingProject
        git pull origin Jimin
    else
        echo "📥 Jimin 브랜치를 클론합니다..."
        git clone -b Jimin $GIT_URL
        cd NursingProject
    fi
    
    echo "📁 nursing_AI 폴더로 이동합니다..."
    cd nursing_AI
    
    echo "🔧 환경 변수 파일을 설정합니다..."
    if [ -f .env ]; then
        echo "✅ 기존 .env 파일이 존재합니다. 덮어쓰지 않습니다."
        echo "📝 현재 .env 파일 내용:"
        cat .env
    else
        echo "🆕 .env 파일이 없어 기본 템플릿을 생성합니다."
        cat > .env << 'ENVFILE'
# ========================================
# 🔑 Nursing API 환경 변수 설정
# ========================================
# OpenAI API 키 (필수)
OPENAI_API_KEY=your_openai_api_key_here

# Pinecone 벡터 데이터베이스 설정 (필수)
PINECONE_API_KEY=your_pinecone_api_key_here
PINECONE_INDEX=your_pinecone_index_name

# Naver Clova Speech API (필수)
CLOVA_SPEECH_SECRET=your_clova_secret_key

# Google Cloud 인증 파일 경로 (필수)
GOOGLE_APPLICATION_CREDENTIALS=/app/credentials/google-credentials.json
ENVFILE
        echo "⚠️  .env 파일이 생성되었습니다. 반드시 실제 값으로 수정해주세요!"
    fi    
    
    echo "📁 credentials 디렉토리를 생성합니다..."
    mkdir -p credentials
    
    echo "🔍 credentials 디렉토리 확인..."
    if [ -f "credentials/google-credentials.json" ]; then
        echo "✅ Google Cloud 인증 파일이 이미 존재합니다."
    else
        echo "⚠️  Google Cloud 인증 파일이 없습니다."
        echo "   credentials/google-credentials.json 파일을 업로드해주세요."
    fi
    
    echo "🔄 기존 컨테이너를 정리합니다..."
    docker-compose down --remove-orphans || true
    
    echo "🔨 Docker 이미지를 빌드합니다..."
    docker-compose build --no-cache
    
    echo "🚀 컨테이너를 실행합니다..."
    docker-compose up -d
    
    echo "🏥 서비스 상태를 확인합니다..."
    sleep 10
    
    if curl -f http://localhost:8000/health > /dev/null 2>&1; then
        echo "✅ 서비스가 정상적으로 실행되었습니다!"
        echo "🌐 서버 주소: http://$INSTANCE_IP:8000"
        echo "📖 API 문서: http://$INSTANCE_IP:8000/docs"
    else
        echo "❌ 서비스 실행에 실패했습니다."
        echo "📋 로그를 확인합니다..."
        docker-compose logs
        echo "⚠️  다음을 확인해주세요:"
        echo "  1. .env 파일의 API 키가 올바른지"
        echo "  2. credentials/google-credentials.json 파일이 있는지"
        echo "  3. Docker 이미지 빌드가 성공했는지"
    fi
    
    echo "📋 최근 로그:"
    docker-compose logs --tail=10
EOF

echo "🎉 Git 배포가 완료되었습니다!"
echo "🌐 서버 주소: http://$INSTANCE_IP:8000"
echo "📖 API 문서: http://$INSTANCE_IP:8000/docs"
echo ""
echo "📋 ========================================"
echo "🔧 필수 설정 파일 및 환경 변수"
echo "=========================================="
echo ""
echo "1️⃣ .env 파일 설정 (SSH 접속 후 수정):"
echo "   ssh -i $KEYPAIR_FILE $INSTANCE_USER@$INSTANCE_IP"
echo "   cd ~/nursing-project/nursing_AI"
echo "   nano .env"
echo ""
echo "   📝 필요한 환경 변수들:"
echo "   - OPENAI_API_KEY: OpenAI API 키"
echo "   - PINECONE_API_KEY: Pinecone API 키"
echo "   - PINECONE_INDEX: Pinecone 인덱스 이름"
echo "   - CLOVA_SPEECH_SECRET: Naver Clova Speech Secret"
echo "   - GOOGLE_APPLICATION_CREDENTIALS: Google Cloud 인증 파일 경로"
echo ""
echo "2️⃣ Google Cloud 인증 파일 업로드:"
echo "   scp -i $KEYPAIR_FILE appc-sc-jimin-59424c1ed192.json $INSTANCE_USER@$INSTANCE_IP:~/nursing-project/nursing_AI/credentials/google-credentials.json"
echo ""
echo "3️⃣ 설정 완료 후 컨테이너 재시작:"
echo "   ssh -i $KEYPAIR_FILE $INSTANCE_USER@$INSTANCE_IP"
echo "   cd ~/nursing-project/nursing_AI"
echo "   docker-compose restart"
echo ""
echo "4️⃣ 서비스 상태 확인:"
echo "   docker-compose logs -f"
echo ""
echo "⚠️  주의사항:"
echo "- .env 파일과 credentials 파일은 덮어씌워지지 않습니다"
echo "- API 키들은 반드시 실제 값으로 수정해야 합니다"
echo "- Google Cloud 인증 파일은 credentials/ 폴더에 google-credentials.json 이름으로 저장해야 합니다" 