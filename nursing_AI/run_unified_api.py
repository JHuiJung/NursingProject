#!/usr/bin/env python3
"""
통합 간호학 API 서버 실행 스크립트
세 개의 API (STT 평가, 부모 채팅, RAG 채팅)를 하나의 서버에서 실행합니다.
"""

import uvicorn
from unified_nursing_api import app

if __name__ == "__main__":
    print("🚀 통합 간호학 API 서버를 시작합니다...")
    print("📋 사용 가능한 엔드포인트:")
    print("   • POST /clova_stt - STT 평가 API")
    print("   • POST /parent_chat - 부모 채팅 API")
    print("   • POST /parent_chat/followup - 부모 채팅 후속 응답")
    print("   • POST /parent_chat/summary - 부모 채팅 요약")
    print("   • POST /tts - 텍스트를 음성으로 변환")
    print("   • POST /chat - RAG 기반 채팅")
    print("   • GET /docs - API 문서 (Swagger UI)")
    print("   • GET /redoc - API 문서 (ReDoc)")
    print("\n🌐 서버 주소: http://localhost:8000")
    print("📖 API 문서: http://localhost:8000/docs")
    
    uvicorn.run(
        "unified_nursing_api:app", 
        host="0.0.0.0", 
        port=8000,
        reload=True,  # 개발 모드에서 코드 변경 시 자동 재시작
        log_level="info"
    )