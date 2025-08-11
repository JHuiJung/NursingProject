from fastapi import FastAPI, File, UploadFile, Form
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware  # CORSMiddleware 임포트
from pydantic import BaseModel
from google.cloud import texttospeech
import requests, base64, os
from tempfile import NamedTemporaryFile
from dotenv import load_dotenv
from nursing_llm import get_ai_response, get_followup_question

load_dotenv()
app = FastAPI()

# CORS 미들웨어를 추가합니다.
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],           # 모든 출처(Origin) 허용
    allow_credentials=True,        # 자격 증명(쿠키, 인증 헤더) 허용
    allow_methods=["*"],           # 모든 HTTP 메서드(GET, POST, OPTIONS 등) 허용
    allow_headers=["*"],           # 모든 헤더 허용
)

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
    음성 인식 텍스트 전처리.
    - corrected: 자주 발생하는 오인식만 보정(평가/프롬프트에 사용)
    - keyword_match_versions: 키워드 매칭 보조용 버전들(평가에는 사용하지 않음)
    """
    # 원본 텍스트
    original = text.strip()
    
    # 띄어쓰기 제거(키워드 매칭 보조용)
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
        # 키워드 매칭 전용(평가/AI 입력에는 미사용)
        "keyword_match_versions": [original, corrected, no_spaces]
    }

def check_required_keywords(text: str, required_keywords: list[str]) -> list[str]:
    """필수 키워드 누락 확인. 평가가 아닌 매칭 보조용으로만 공백 제거를 활용."""
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
###
@app.post("/clova_stt")
async def clova_stt(
    audio: UploadFile | None = File(None)
):
    ai_text = clova_speech_to_text(audio);

    return JSONResponse({
        "text": ai_text
    })
###
# # ###
# # @app.post("/clova_stt")
# # async def clova_stt(
# #     question: str = Form(...),
# #     audio: UploadFile | None = File(None),
# #     patient_name: str | None = Form(None),
# #     patient_regno: str | None = Form(None),
# # ):
# #     """STT 기반 평가 또는 텍스트 입력 기반 평가를 제공

# #     - 환아 이름/등록번호 확인(Q17) 같은 경우: patient_name, patient_regno 전달 → 텍스트 기반 평가
# #     - 그 외: audio 업로드 → Clova STT → 평가
# #     """

# #     # 1) 텍스트 입력 기반 분기 (환아 이름/등록번호 확인)
# #     if (patient_name and patient_name.strip()) or (patient_regno and patient_regno.strip()):
# #         name_val = patient_name.strip() if patient_name else ""
# #         reg_val = patient_regno.strip() if patient_regno else ""
# #         transcript = f"환아 이름: {name_val}, 등록번호: {reg_val}"

# #         full_input = (
# #             f"질문: {question}\n"
# #             f"사용자 입력: {transcript}\n\n"
# #         )
# #         ai_response = get_ai_response(full_input)
# #         feedback = ai_response.get("answer", "")

# #         return JSONResponse({
# #             "transcript": transcript,
# #             "processed_versions": {"original": transcript},
# #             "feedback": feedback,
# #             "is_correct": feedback,
# #             "question": question
# #         })

# #     # 2) 음성(STT) 기반 분기 (주사 목적 등)
# #     if audio is None:
# #         return JSONResponse(status_code=400, content={"error": "audio 또는 patient_name/patient_regno 중 하나는 제공되어야 합니다."})

# #     audio_bytes = await audio.read()
# #     headers = {
# #         "X-CLOVASPEECH-API-KEY": CLOVA_API_KEY,
# #         "Content-Type": "application/octet-stream"
# #     }
# #     params = {"lang": "Kor",
# #               "boostings": "환아\t이하트\t239845\t심장\t주사\t팔\t수액줄"}

# #     # Clova STT 요청
# #     resp = requests.post(CLOVA_URL, params=params, headers=headers, data=audio_bytes)
# #     if resp.status_code != 200:
# #         return JSONResponse(status_code=500, content={"error": f"Clova STT 실패: {resp.status_code}"})
# #     result = resp.json()
# #     transcript = result.get("text", "")

# #     # 음성 인식 텍스트 전처리
# #     processed = preprocess_speech_text(transcript)

# #     # LangChain 평가 구성(수정 텍스트 기준)
# #     evaluation_text = processed["corrected"]
# #     full_input = (
# #         f"질문: {question}\n"
# #         f"평가용 사용자 응답: {evaluation_text}\n\n"
# #     )
# #     ai_response = get_ai_response(full_input)
# #     feedback = ai_response.get("answer", "")

# #     return JSONResponse({
# #         "transcript": processed['original'],
# #         "processed_versions": processed,
# #         "feedback": feedback,
# #         "is_correct": feedback,
# #         "question": question
# #     })
# # ###

def _as_text(x) -> str:
    try:
        if x is None:
            return ""
        if isinstance(x, str):
            return x.strip()
        # LangChain AIMessage
        if hasattr(x, "content"):
            return str(getattr(x, "content")).strip()
        # dict 형태 (예: {"answer": "..."} 등)
        if isinstance(x, dict):
            for k in ("answer", "text", "output_text"):
                if k in x and x[k]:
                    return str(x[k]).strip()
        return str(x).strip()
    except Exception:
        return ""

# ========================================
# 🔹 2. 부모 채팅 API (parent_chat_api.py)
# ========================================

@app.post("/parent_response")
async def parent_chat(
    parent_question: str = Form(...),
    user_response: str = Form(...),
    keywords: str = Form(...)
):
    # """부모 음성 → STT → 키워드 누락 확인 → TTS 응답"""
    # transcript = clova_speech_to_text(audio)
    # current_q = next(q for q in QUESTIONS if q["id"] == question_id)
    # missing_keywords = check_required_keywords(transcript, current_q["required_keywords"])

    # 로그로 질문, 응답 확인
    print(f"부모 질문 {parent_question} ")
    print(f"유저 질문 {user_response} ")
    print(f"핵심키워드 {keywords} ")

    try:
        ack_prompt = (
            "역할: 당신은 환아의 보호자(부모)입니다. 아이가 아파 불안하고 다소 예민합니다.\n"
            "목표: 부모의 입장에서 간호사 설명을 검토하여 누락된 핵심 키워드를 중심으로 답변해줘. 정답 오답은 너가 판단하는게 아니야\n"
            f"질문: {parent_question}\n"
            f"간호사 설명 : {user_response}\n"
            f"간호사 설명에 포함되야할 핵심 키워드 : {keywords}\n\n"
            "스타일: 존댓말, 예민한 상황, 아이의 건강을 걱정한다.\n"
            "제한: 이모지는 사용하지 않습니다."
            "제한: 총 글자수는 150자를 넘지 안되, 150글자에 근접하게 작성할것"
            "제한: 너는 아이의 부모라는 것을 절대절대절대절대 역할에서 벗어나면 안되"

        )
        ai_ack = get_ai_response(ack_prompt)
        ack_text = ai_ack.get("answer", "좋습니다. 내용을 잘 이해하셨습니다. 다음 질문으로 넘어갈게요.")
    except Exception:
        ack_text = "알겠습니다..."

    response = {
        "parent_response": ack_text,
    }

    return JSONResponse(content=response)





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

    if missing_keywords:
        followup_result = get_followup_question(transcript, missing_keywords)
        followup_text = _as_text(followup_result) or "말씀하신 내용 중 더 구체적으로 설명해 주실 부분이 있어요. 아래 키워드를 포함해 다시 말씀해 주실 수 있을까요?"

        tts_audio = synthesize_text(followup_text)
        response.update({
            "followup_needed": True,
            "followup_audio_base64": tts_audio,
            "missing_keywords": missing_keywords,
            "followup_text": followup_text
        })
    else:
        # 키워드 모두 만족: 보호자 답변을 바탕으로 간호사 톤의 맞춤형 이해 확인 멘트를 AI로 생성
        try:
            ack_prompt = (
                "역할: 당신은 환아의 보호자(부모)입니다.\n"
                "상황: 간호사가 아래 질문에 대해 충분히 설명했고, 당신(보호자)은 그 내용을 이해했습니다.\n"
                f"질문: {current_q['text']}\n"
                f"간호사 설명 요지(STT): {transcript}\n\n"
                "요청: 간호사의 설명을 이해했다는 뜻을 짧게 인정하고, 다음 질문으로 넘어가자는 자연스러운 보호자 톤의 멘트를 1~2문장으로 작성하세요.\n"
                "스타일: 존댓말, 공감/안도/감사의 뉘앙스, 과도한 의학적 조언 없이 간단한 반응 위주.\n"
                "제한: 12~30자 내외의 짧은 문장 1~2개. 이모지는 사용하지 않습니다."
            )
            ai_ack = get_ai_response(ack_prompt)
            ack_text = ai_ack.get("answer", "좋습니다. 내용을 잘 이해하셨습니다. 다음 질문으로 넘어갈게요.")
        except Exception:
            ack_text = "좋습니다. 내용을 잘 이해하셨습니다. 다음 질문으로 넘어갈게요."

        ack_audio = synthesize_text(ack_text)
        response.update({
            "ack_text": ack_text,
            "ack_audio_base64": ack_audio
        })

    return JSONResponse(content=response)

@app.post("/parent_chat/followup")
async def parent_chat_followup(
    session_id: str = Form(...),
    question_id: str = Form(...),
    audio: UploadFile = File(...)
):
    transcript = clova_speech_to_text(audio)

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

    # 누락 키워드 재확인
    missing_keywords = check_required_keywords(updated, current_q["required_keywords"])

    resp = {
        "transcript": transcript,
        "updated_full_response": updated,
        "followup_registered": True
    }

    if missing_keywords:
        # 여전히 부족 → 추가 꼬리질문 생성
        followup_result = get_followup_question(updated, missing_keywords)
        followup_text = _as_text(followup_result) or "좋아요. 아래 키워드를 포함해서 한 번 더 설명해 주실 수 있을까요?"
        tts_audio = synthesize_text(followup_text)

        resp.update({
            "followup_needed": True,
            "missing_keywords": missing_keywords,
            "followup_text": followup_text,
            "followup_audio_base64": tts_audio
        })
    else:
        # 충분 → 짧은 확인 멘트
        try:
            ack_prompt = (
                "역할: 간호사.\n"
                f"질문: {current_q['text']}\n"
                f"보호자 종합 응답: {updated}\n"
                "요청: 충분히 설명과 이해가 이뤄졌음을 1~2문장으로 부드럽게 확인."
            )
            ai_ack = get_ai_response(ack_prompt)
            ack_text = ai_ack.get("answer", "좋습니다. 충분히 확인되었어요. 다음으로 넘어가겠습니다.")
        except Exception:
            ack_text = "좋습니다. 충분히 확인되었어요. 다음으로 넘어가겠습니다."

        resp.update({
            "followup_needed": False,
            "ack_text": ack_text,
            "ack_audio_base64": synthesize_text(ack_text)
        })

    return JSONResponse(content=resp)


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

    print(ai_response)
    
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