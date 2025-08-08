from fastapi import FastAPI, File, UploadFile, Form
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from google.cloud import texttospeech
import requests, base64, os
from tempfile import NamedTemporaryFile
from dotenv import load_dotenv
from nursing_llm import get_ai_response, get_followup_question

load_dotenv()
app = FastAPI()

# ========================================
# 🔹 공통 설정 및 유틸리티 함수
# ========================================

CLOVA_URL = "https://clovaspeech-gw.ncloud.com/recog/v1/stt"
CLOVA_API_KEY = os.getenv("CLOVA_SPEECH_SECRET")

@app.get("/health")
async def health_check():
    """헬스 체크 엔드포인트"""
    return {"status": "healthy", "message": "Nursing API is running"}

@app.get("/")
async def root():
    """루트 엔드포인트"""
    return {"message": "Nursing API Server", "docs": "/docs"}

def clova_speech_to_text(audio_file: UploadFile) -> str:
    """Clova Speech-to-Text API를 사용하여 음성을 텍스트로 변환"""
    # 오디오 파일을 임시 저장
    with NamedTemporaryFile(delete=False, suffix=".wav") as tmp:
        audio_bytes = audio_file.file.read()
        tmp.write(audio_bytes)
        tmp_path = tmp.name

    headers = {
        "X-CLOVASPEECH-API-KEY": CLOVA_API_KEY,
        "Content-Type": "application/octet-stream"
    }

    params = {
        "lang": "Kor"
    }

    with open(tmp_path, "rb") as f:
        response = requests.post(CLOVA_URL, headers=headers, params=params, data=f)

    if response.status_code != 200:
        raise Exception(f"Clova STT 요청 실패: {response.status_code} - {response.text}")

    return response.json().get("text", "")

def synthesize_text(text: str) -> str:
    """Google Text-to-Speech API를 사용하여 텍스트를 음성으로 변환"""
    client = texttospeech.TextToSpeechClient()

    synthesis_input = texttospeech.SynthesisInput(text=text)

    voice = texttospeech.VoiceSelectionParams(
        language_code="ko-KR",
        name="ko-KR-Wavenet-A",
        ssml_gender=texttospeech.SsmlVoiceGender.FEMALE,
    )

    audio_config = texttospeech.AudioConfig(
        audio_encoding=texttospeech.AudioEncoding.MP3,
        sample_rate_hertz=24000,
        speaking_rate=1.3,  # 더 빠른 속도로 날카로운 느낌
        pitch=5.0           # 높은 톤으로 날카로운 목소리
    )

    response = client.synthesize_speech(
        input=synthesis_input, voice=voice, audio_config=audio_config
    )

    return base64.b64encode(response.audio_content).decode("utf-8")

def preprocess_speech_text(text: str) -> dict:
    """
    음성 인식 텍스트를 전처리하여 띄어쓰기 문제를 해결합니다.
    """
    # 원본 텍스트
    original = text.strip()
    
    # 띄어쓰기 제거
    no_spaces = original.replace(" ", "")
    
    # 일반적인 음성 인식 오류 패턴 수정
    common_errors = {
        "항생제": ["항생재", "항성제", "항생제"],
        "정맥": ["정맥", "정명", "정맥"],
        "주사": ["주사", "주사기", "주사"],
        "감염": ["감염", "감염증", "감염"],
        "염증": ["염증", "염증상", "염증"],
        "이해": ["이해", "이해", "이해"],
        "발진": ["발진", "발진", "발진"],
        "설사": ["설사", "설사", "설사"],
        "알레르기": ["알레르기", "알레르기", "알레르기"],
        "호출밸": ["호출밸", "호출벨", "호출밸"],
        "응급조치": ["응급조치", "응급조치", "응급조치"]
    }
    
    corrected = original
    for correct, variations in common_errors.items():
        for variation in variations:
            if variation in corrected:
                corrected = corrected.replace(variation, correct)
    
    return {
        "original": original,
        "no_spaces": no_spaces,
        "corrected": corrected,
        "all_versions": [original, no_spaces, corrected]
    }

def check_required_keywords(text: str, required_keywords: list[str]) -> list[str]:
    """필수 키워드 누락 확인 (전처리된 텍스트 사용)"""
    processed = preprocess_speech_text(text)
    
    # 모든 버전에서 키워드 확인
    missing_keywords = []
    for keyword in required_keywords:
        found = False
        for version in processed["all_versions"]:
            if keyword in version:
                found = True
                break
        if not found:
            missing_keywords.append(keyword)
    
    return missing_keywords

# ========================================
# 🔹 부모 채팅 관련 설정
# ========================================

QUESTIONS = [
    {
        "id": "Q1",
        "text": "이 약은 무슨 약인가요?",
        "required_keywords": ["항생제", "감염", "염증"]
    },
    {
        "id": "Q2",
        "text": "약으로 먹진 않고 주사로만 투여되나요?",
        "required_keywords": ["정맥"]
    },
    {
        "id": "Q3",
        "text": "항생제를 맞는다면 언제부터 효과가 나타날까요. 바로 감염수치가 낮아지나요?",
        "required_keywords": ["항생제"]
    },
    {
        "id": "Q4",
        "text": "저에겐 어렵게 얻은 아이라 너무 걱정이 되는데요 약물 투여시 부작용은 없는거죠?",
        "required_keywords": ["이해", "발진", "설사", "주사 부위 통증", "알레르기 반응", "이상 증상"]
    },
    {
        "id": "Q5",
        "text": "혹시라도 방금 애기해준 가벼운 부작용나 아니면 심각한 알레르기 증상이 나타나면 어떻 처치를 해주나요?",
        "required_keywords": ["호출밸", "응급조치", "약물 투여", "호전"]
    }
]

# 세션별 응답 저장
user_sessions = {}

# ========================================
# 🔹 요청/응답 모델 정의
# ========================================

class ChatRequest(BaseModel):
    session_id: str
    question: str

class ChatResponse(BaseModel):
    answer: str
    correct_count: int
    incorrect_count: int
    total_questions: int
    score_percentage: float

# ========================================
# 🔹 1. STT 평가 API (stt_naver_api.py)
# ========================================

@app.post("/clova_stt")
async def clova_stt(
    question: str = Form(...),
    audio: UploadFile = File(...)
):
    """음성 파일을 텍스트로 변환하고 AI 평가를 제공"""
    audio_bytes = await audio.read()
    headers = {
        "X-CLOVASPEECH-API-KEY": CLOVA_API_KEY,
        "Content-Type": "application/octet-stream"
    }
    params = {"lang": "Kor",
              "boostings": "환아\t이하트\t239845\t심장\t주사\t팔\t수액줄"}

    # Clova STT 요청
    resp = requests.post(CLOVA_URL, params=params, headers=headers, data=audio_bytes)
    if resp.status_code != 200:
        return JSONResponse(status_code=500, content={"error": f"Clova STT 실패: {resp.status_code}"})
    result = resp.json()
    transcript = result.get("text", "")

    # 음성 인식 텍스트 전처리
    processed = preprocess_speech_text(transcript)
    
    # LangChain 평가 구성
    full_input = (
        f"질문: {question}\n\n"
        f"사용자 음성 응답 텍스트 (원본): {processed['original']}\n"
        f"사용자 음성 응답 텍스트 (띄어쓰기 제거): {processed['no_spaces']}\n"
        f"사용자 음성 응답 텍스트 (오류 수정): {processed['corrected']}\n\n"
        "음성 인식 시 띄어쓰기나 발음 오류가 발생할 수 있으므로, 모든 버전을 고려하여 평가해주세요.\n"
        "핵심 키워드가 포함되어 있다면 정답으로 처리하세요. 정답일 경우 ✅로 시작하고, 오답일 경우 ❌로 시작해주세요."
    )

    ai_response = get_ai_response(full_input)
    feedback = ai_response.get("answer", "")

    return JSONResponse({
        "transcript": processed['original'],
        "processed_versions": processed,
        "feedback": feedback,
        "is_correct": feedback,
        "question": question
    })

# ========================================
# 🔹 2. 부모 채팅 API (parent_chat_api.py)
# ========================================

@app.post("/parent_chat")
async def parent_chat(
    session_id: str = Form(...),
    question_id: str = Form(...),
    audio: UploadFile = File(...)
):
    """부모 음성 → STT → 키워드 누락 확인 → TTS 응답"""
    transcript = clova_speech_to_text(audio)
    current_q = next(q for q in QUESTIONS if q["id"] == question_id)
    missing_keywords = check_required_keywords(transcript, current_q["required_keywords"])

    # 로그로 질문, 응답 확인
    print(f"[{session_id}] 질문 {question_id} → '{current_q['text']}'")
    print(f"[{session_id}] 응답: {transcript}")
    if missing_keywords:
        print(f"[{session_id}] 누락 키워드: {missing_keywords}")
    else:
        print(f"[{session_id}] 모든 키워드 포함")

    # 세션 응답 저장
    if session_id not in user_sessions:
        user_sessions[session_id] = {}
    user_sessions[session_id][question_id] = transcript

    response = {
        "transcript": transcript,
        "followup_needed": False,
        "missing_keywords": []
    }

    # 누락 키워드 있을 때 맞장구식 후속 질문 제공
    if missing_keywords:
        followup_text = get_followup_question(transcript, missing_keywords)
        tts_audio = synthesize_text(followup_text)
        response.update({
            "followup_needed": True,
            "followup_audio_base64": tts_audio,
            "missing_keywords": missing_keywords
        })

    return JSONResponse(content=response)

@app.post("/parent_chat/followup")
async def parent_chat_followup(
    session_id: str = Form(...),
    question_id: str = Form(...),
    audio: UploadFile = File(...)
):
    """후속 응답 처리"""
    transcript = clova_speech_to_text(audio)

    if session_id not in user_sessions:
        return JSONResponse(content={"error": "세션이 없습니다."}, status_code=404)
    
    if question_id not in user_sessions[session_id]:
        return JSONResponse(content={"error": f"{question_id}에 대한 기존 응답이 없습니다."}, status_code=404)

    # 기존 응답에 후속 응답을 이어붙이기
    prev = user_sessions[session_id][question_id]
    updated = prev.strip() + " " + transcript.strip()
    user_sessions[session_id][question_id] = updated

    print(f"[{session_id}] 후속 응답 누적 → '{updated}'")

    return JSONResponse(content={
        "transcript": transcript,
        "updated_full_response": updated,
        "followup_registered": True
    })

@app.post("/parent_chat/summary")
async def parent_chat_summary(session_id: str = Form(...)):
    """전체 요약 및 AI 피드백 요청"""
    if session_id not in user_sessions:
        return JSONResponse(content={"error": "세션이 없습니다."}, status_code=404)

    conversation = user_sessions[session_id]
    formatted = "\n".join([f"{qid}: {resp}" for qid, resp in conversation.items()])

    prompt = (
        "아래는 보호자와 간호사의 대화입니다.\n" 
        "각 질문에 대해 간호사의 설명이 충분했는지 평가하고 피드백을 주세요.\n"
        f"{formatted}"
    )

    feedback = get_ai_response(prompt)
    print(f"[{session_id}] 요약 프롬프트:\n{prompt}")
    print(f"[{session_id}] AI 피드백 결과:\n{feedback}")
    return JSONResponse(content={
        "session_id": session_id,
        "feedback": feedback
    })

@app.post("/tts")
async def tts(text: str = Form(...)):
    """질문 또는 일반 텍스트를 음성으로 변환하여 base64로 반환"""
    audio_base64 = synthesize_text(text)
    return JSONResponse(content={"audio_base64": audio_base64})

# ========================================
# 🔹 3. RAG 채팅 API (chat_rag_api.py)
# ========================================

@app.post("/chat", response_model=ChatResponse)
def chat_endpoint(request: ChatRequest):
    """RAG 기반 채팅 응답"""
    ai_response = get_ai_response(request.question)
    
    return {
        "answer": ai_response["answer"],
        "correct_count": ai_response["correct_count"],
        "incorrect_count": ai_response["incorrect_count"],
        "total_questions": ai_response["total_questions"],
        "score_percentage": round((ai_response["correct_count"] / ai_response["total_questions"] * 100) if ai_response["total_questions"] > 0 else 0, 1)
    }

# ========================================
# 🔹 서버 실행을 위한 메인 함수
# ========================================

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000) 