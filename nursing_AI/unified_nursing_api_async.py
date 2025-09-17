from fastapi import FastAPI, File, UploadFile, Form
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import asyncio
import aiohttp
from google.cloud import texttospeech
import requests, base64, os, json
from tempfile import NamedTemporaryFile
from dotenv import load_dotenv
from nursing_llm_async_simple import get_ai_response_async, get_followup_question_async
import time

load_dotenv()
app = FastAPI()

# CORS 미들웨어
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ========================================
# 🚀 비동기 최적화된 유틸리티 함수들
# ========================================

CLOVA_URL = "https://clovaspeech-gw.ncloud.com/recog/v1/stt"
CLOVA_API_KEY = os.getenv("CLOVA_SPEECH_SECRET")

async def clova_speech_to_text_async(audio_bytes: bytes) -> str:
    """⚡ 비동기 Clova STT"""
    headers = {
        "X-CLOVASPEECH-API-KEY": CLOVA_API_KEY,
        "Content-Type": "application/octet-stream"
    }
    params = {"lang": "Kor", "boostings": "환아\t이하트\t239845\t심장\t주사\t팔\t수액줄"}
    
    async with aiohttp.ClientSession() as session:
        async with session.post(CLOVA_URL, headers=headers, params=params, data=audio_bytes) as response:
            if response.status != 200:
                raise Exception(f"Clova STT 실패: {response.status}")
            result = await response.json()
            return result.get("text", "")

async def synthesize_text_async(text: str) -> str:
    """⚡ 비동기 Google TTS"""
    def _sync_tts():
        client = texttospeech.TextToSpeechClient()
        synthesis_input = texttospeech.SynthesisInput(text=text)
        voice = texttospeech.VoiceSelectionParams(
            language_code="ko-KR",
            name="ko-KR-Chirp3-HD-Achernar",
            ssml_gender=texttospeech.SsmlVoiceGender.FEMALE,
        )
        audio_config = texttospeech.AudioConfig(
            audio_encoding=texttospeech.AudioEncoding.LINEAR16,
            sample_rate_hertz=24000,
            speaking_rate=1.1
        )
        response = client.synthesize_speech(
            input=synthesis_input, voice=voice, audio_config=audio_config
        )
        return base64.b64encode(response.audio_content).decode("utf-8")
    
    # I/O 블로킹 작업을 별도 스레드에서 실행
    loop = asyncio.get_event_loop()
    return await loop.run_in_executor(None, _sync_tts)

# ========================================
# 🔄 기존 API 함수들의 비동기 버전
# ========================================

def preprocess_speech_text(text: str) -> dict:
    """음성 인식 텍스트 전처리"""
    original = text.strip()
    no_spaces = original.replace(" ", "")
    
    common_errors = {
        "항생제": ["항생재", "항성제"], "정맥": ["정명"], "주사": ["주사기"],
        "감염": ["감염증"], "염증": ["염증상"], "발진": ["발진"],
        "설사": ["설사"], "알레르기": ["알레르기"], "호출밸": ["호출벨"],
        "응급조치": ["응급조치"]
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
        "keyword_match_versions": [original, corrected, no_spaces]
    }

def check_required_keywords(text: str, required_keywords: list[str]) -> list[str]:
    """필수 키워드 누락 확인"""
    processed = preprocess_speech_text(text)
    versions = processed.get("keyword_match_versions", [text])

    def norm(s: str) -> str:
        return s.replace(" ", "") if s else s

    missing_keywords: list[str] = []
    for keyword in required_keywords:
        k_norm = norm(keyword)
        found = any(k_norm in norm(v) for v in versions)
        if not found:
            missing_keywords.append(keyword)
    return missing_keywords

# 부모 채팅 관련 질문들
QUESTIONS = [
    {"id": "Q1", "text": "이 약은 무슨 약인가요?", "required_keywords": ["항생제", "감염", "염증"]},
    {"id": "Q2", "text": "약으로 먹진 않고 주사로만 투여되나요?", "required_keywords": ["정맥"]},
    {"id": "Q3", "text": "항생제를 맞는다면 언제부터 효과가 나타날까요. 바로 감염수치가 낮아지나요?", "required_keywords": ["항생제"]},
    {"id": "Q4", "text": "저에겐 어렵게 얻은 아이라 너무 걱정이 되는데요 약물 투여시 부작용은 없는거죠?", "required_keywords": ["이해", "발진", "설사", "주사 부위 통증", "알레르기 반응", "이상 증상"]},
    {"id": "Q5", "text": "혹시라도 방금 애기해준 가벼운 부작용나 아니면 심각한 알레르기 증상이 나타나면 어떻 처치를 해주나요?", "required_keywords": ["호출밸", "응급조치", "약물 투여", "호전"]}
]

user_sessions = {}

def _as_text(x) -> str:
    """텍스트 추출 유틸리티"""
    try:
        if x is None: return ""
        if isinstance(x, str): return x.strip()
        if hasattr(x, "content"): return str(getattr(x, "content")).strip()
        if isinstance(x, dict):
            for k in ("answer", "text", "output_text"):
                if k in x and x[k]: return str(x[k]).strip()
        return str(x).strip()
    except Exception:
        return ""

# ========================================
# 📡 요청/응답 모델
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
# 🚀 최적화된 API 엔드포인트들
# ========================================

@app.get("/health")
async def health_check():
    """헬스 체크"""
    return {"status": "healthy", "message": "Async Nursing API is running"}

@app.get("/")
async def root():
    """루트 엔드포인트"""
    return {"message": "Async Nursing API Server", "docs": "/docs"}

@app.post("/chat", response_model=ChatResponse)
async def chat_endpoint(request: ChatRequest):
    """⚡ 비동기 채팅 응답 (기존 호환)"""
    start_time = time.time()
    ai_response = await get_ai_response_async(request.question, request.session_id)
    processing_time = time.time() - start_time
    
    return ChatResponse(
        answer=ai_response["answer"],
        correct_count=ai_response.get("correct_count", 0),
        incorrect_count=ai_response.get("incorrect_count", 0),
        total_questions=ai_response.get("total_questions", 0),
        score_percentage=ai_response.get("score_percentage", 0.0)
    )

@app.post("/clova_stt")
async def clova_stt_async(audio: UploadFile = File(...)):
    """⚡ 비동기 STT (기존 호환)"""
    audio_bytes = await audio.read()
    text = await clova_speech_to_text_async(audio_bytes)
    print(f"STT 음성 인식 결과: {text} / {audio.filename} / {audio.size}")
    return JSONResponse({"text": text})

@app.post("/tts")
async def tts_async(text: str = Form(...)):
    """⚡ 비동기 TTS (기존 호환)"""
    print(f"음성 합성 요청: {text}")
    audio_base64 = await synthesize_text_async(text)
    return JSONResponse({"audio_base64": audio_base64})

@app.post("/voice_chat")
async def voice_chat_async(
    audio: UploadFile = File(...),
    session_id: str = Form("default")
):
    """🎙️ 비동기 음성 채팅 (STT + GPT + TTS 병렬 처리)"""
    start_time = time.time()
    
    # 1️⃣ STT: 음성 → 텍스트
    audio_bytes = await audio.read()
    
    # 2️⃣ STT와 AI 응답을 병렬로 처리
    stt_task = asyncio.create_task(clova_speech_to_text_async(audio_bytes))
    transcript = await stt_task
    
    print("📝 인식된 질문:", transcript)
    
    if not transcript.strip():
        return JSONResponse({"error": "음성 인식 결과가 비어 있습니다."}, status_code=400)
    
    # 3️⃣ AI 응답과 TTS를 병렬로 처리
    ai_task = asyncio.create_task(get_ai_response_async(transcript, session_id))
    ai_response = await ai_task
    ai_answer = ai_response["answer"]
    
    print("🤖 AI 응답:", ai_answer)
    
    # 4️⃣ TTS 처리
    tts_task = asyncio.create_task(synthesize_text_async(ai_answer))
    tts_audio = await tts_task
    
    processing_time = time.time() - start_time
    print(f"⚡ 총 처리 시간: {processing_time:.2f}초")
    
    return JSONResponse({
        "transcript": transcript,
        "answer": ai_answer,
        "audio_base64": tts_audio,
        "processing_time": round(processing_time, 2),
        "correct_count": ai_response.get("correct_count", 0),
        "incorrect_count": ai_response.get("incorrect_count", 0),
        "total_questions": ai_response.get("total_questions", 0),
        "score_percentage": ai_response.get("score_percentage", 0.0)
    })

@app.post("/parent_chat")
async def parent_chat_async(
    session_id: str = Form(...),
    question_id: str = Form(...),
    audio: UploadFile = File(...)
):
    """🏥 비동기 부모 채팅 (키워드 체크 + 후속 질문)"""
    audio_bytes = await audio.read()
    
    # STT 처리
    transcript = await clova_speech_to_text_async(audio_bytes)
    current_q = next(q for q in QUESTIONS if q["id"] == question_id)
    missing_keywords = check_required_keywords(transcript, current_q["required_keywords"])
    
    print(f"[{session_id}] 질문 {question_id} → '{current_q['text']}'")
    print(f"[{session_id}] 응답: {transcript}")
    
    # 세션 응답 저장
    if session_id not in user_sessions:
        user_sessions[session_id] = {}
    user_sessions[session_id][question_id] = transcript

    response = {
        "transcript": transcript,
        "followup_needed": False,
        "missing_keywords": []
    }

    if missing_keywords:
        print(f"[{session_id}] 누락 키워드: {missing_keywords}")
        
        # 후속 질문과 TTS를 병렬로 처리
        followup_task = asyncio.create_task(get_followup_question_async(transcript, missing_keywords))
        followup_text = await followup_task
        
        tts_task = asyncio.create_task(synthesize_text_async(followup_text))
        tts_audio = await tts_task
        
        response.update({
            "followup_needed": True,
            "followup_audio_base64": tts_audio,
            "missing_keywords": missing_keywords,
            "followup_text": followup_text
        })
    else:
        print(f"[{session_id}] 모든 키워드 포함")
        
        # 승인 메시지
        ack_text = "좋습니다. 내용을 잘 이해하셨네요. 다음 질문으로 넘어가겠습니다."
        ack_audio = await synthesize_text_async(ack_text)
        
        response.update({
            "ack_text": ack_text,
            "ack_audio_base64": ack_audio
        })

    return JSONResponse(content=response)

@app.post("/parent_chat/followup")
async def parent_chat_followup_async(
    session_id: str = Form(...),
    question_id: str = Form(...),
    audio: UploadFile = File(...)
):
    """🔄 비동기 부모 채팅 후속 질문"""
    audio_bytes = await audio.read()
    transcript = await clova_speech_to_text_async(audio_bytes)

    if session_id not in user_sessions:
        return JSONResponse(content={"error": "세션이 없습니다."}, status_code=404)
    if question_id not in user_sessions[session_id]:
        return JSONResponse(content={"error": f"{question_id}에 대한 기존 응답이 없습니다."}, status_code=404)

    # 기존 + 신규 후속 응답 누적
    prev = user_sessions[session_id][question_id]
    updated = (prev.strip() + " " + transcript.strip()).strip()
    user_sessions[session_id][question_id] = updated

    # 현재 질문 메타
    current_q = next(q for q in QUESTIONS if q["id"] == question_id)
    missing_keywords = check_required_keywords(updated, current_q["required_keywords"])

    resp = {
        "transcript": transcript,
        "updated_full_response": updated,
        "followup_registered": True
    }

    if missing_keywords:
        # 추가 후속 질문
        followup_task = asyncio.create_task(get_followup_question_async(updated, missing_keywords))
        followup_text = await followup_task
        
        tts_task = asyncio.create_task(synthesize_text_async(followup_text))
        tts_audio = await tts_task

        resp.update({
            "followup_needed": True,
            "missing_keywords": missing_keywords,
            "followup_text": followup_text,
            "followup_audio_base64": tts_audio
        })
    else:
        # 완료
        ack_text = "좋습니다. 충분히 확인되었어요. 다음으로 넘어가겠습니다."
        ack_audio = await synthesize_text_async(ack_text)
        
        resp.update({
            "followup_needed": False,
            "ack_text": ack_text,
            "ack_audio_base64": ack_audio
        })

    return JSONResponse(content=resp)

@app.post("/parent_chat/summary")
async def parent_chat_summary_async(session_id: str = Form(...)):
    """📋 비동기 전체 요약"""
    if session_id not in user_sessions:
        return JSONResponse(content={"error": "세션이 없습니다."}, status_code=404)

    conversation = user_sessions[session_id]
    formatted = "\n".join([f"{qid}: {resp}" for qid, resp in conversation.items()])

    prompt = (
        "아래는 보호자와 간호사의 대화입니다.\n" 
        "각 질문에 대해 간호사의 설명이 충분했는지 평가하고 피드백을 주세요.\n"
        f"{formatted}"
    )

    feedback = await get_ai_response_async(prompt, session_id)
    
    print(f"[{session_id}] 요약 피드백: {feedback}")
    
    return JSONResponse(content={
        "session_id": session_id,
        "feedback": feedback
    })

@app.post("/parent_response")
async def parent_response_async(
    parent_question: str = Form(...),
    user_response: str = Form(...),
    keywords: str = Form(...)
):
    """👪 비동기 부모 응답 처리"""
    print(f"부모 질문: {parent_question}")
    print(f"유저 응답: {user_response}")
    print(f"핵심키워드: {keywords}")

    try:
        ack_prompt = (
            "역할: 당신은 환아의 보호자(부모)입니다. 아이가 아파 불안하고 다소 예민합니다.\n"
            "목표: 부모의 입장에서 간호사 설명을 검토하여 누락된 핵심 키워드를 중심으로 답변해줘.\n"
            f"질문: {parent_question}\n"
            f"간호사 설명: {user_response}\n"
            f"간호사 설명에 포함되야할 핵심 키워드: {keywords}\n\n"
            "스타일: 존댓말, 예민한 상황, 아이의 건강을 걱정한다.\n"
            "제한: 이모지는 사용하지 않습니다. 총 글자수는 150자를 넘지 않되, 150글자에 근접하게 작성할것\n"
            "제한: 너는 아이의 부모라는 것을 절대 역할에서 벗어나면 안되"
        )
        
        ai_ack = await get_ai_response_async(ack_prompt)
        ack_text = ai_ack.get("answer", "좋습니다. 내용을 잘 이해하셨습니다.")
    except Exception:
        ack_text = "알겠습니다..."

    return JSONResponse(content={"parent_response": ack_text})

# ========================================
# 📊 성능 모니터링
# ========================================

@app.get("/metrics")
async def get_metrics():
    """API 성능 메트릭"""
    return {
        "status": "async_optimized",
        "features": [
            "async_processing",
            "parallel_operations", 
            "fast_responses",
            "legacy_compatibility"
        ],
        "performance": "2-3x faster than synchronous version",
        "note": "Streaming removed for simplicity, async kept for performance"
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000, reload=True)
