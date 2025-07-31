from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import JSONResponse
from google.cloud import texttospeech
import requests, base64, os
from tempfile import NamedTemporaryFile
from nursing_llm import get_ai_response, get_followup_question


app = FastAPI()

# -------------------------------
# 🔹 Clova Speech-to-Text (STT)
# -------------------------------
def clova_speech_to_text(audio_file: UploadFile) -> str:
    CLOVA_URL = "https://clovaspeech-gw.ncloud.com/recog/v1/stt"
    SECRET_KEY = os.getenv("CLOVA_SPEECH_SECRET")  # 정확한 이름으로 통일

    # 오디오 파일을 임시 저장
    with NamedTemporaryFile(delete=False, suffix=".wav") as tmp:
        audio_bytes = audio_file.file.read()
        tmp.write(audio_bytes)
        tmp_path = tmp.name

    # Clova short-sentence 방식은 octet-stream
    headers = {
        "X-CLOVASPEECH-API-KEY": SECRET_KEY,
        "Content-Type": "application/octet-stream"
    }

    params = {
        "lang": "Kor"
        # "boostings": "환아\t심장\t주사\t정맥\t수액\t응급\t열\t항생제"
    }

    with open(tmp_path, "rb") as f:
        response = requests.post(CLOVA_URL, headers=headers, params=params, data=f)

    if response.status_code != 200:
        raise Exception(f"Clova STT 요청 실패: {response.status_code} - {response.text}")

    return response.json().get("text", "")
# -------------------------------
# 🔹 Google Text-to-Speech (TTS)
# -------------------------------
def synthesize_text(text: str) -> str:
    client = texttospeech.TextToSpeechClient()

    # 감정을 표현하는 SSML 태그 적용
    ssml_text = f"""
    <speak>
      <prosody rate="slow" pitch="-1st" volume="loud">
        {text}
      </prosody>
    </speak>
    """

    synthesis_input = texttospeech.SynthesisInput(ssml=ssml_text)

    voice = texttospeech.VoiceSelectionParams(
        language_code="ko-KR",
        name="ko-KR-Wavenet-A",  # 감정 표현이 더 자연스러운 WaveNet 사용
        ssml_gender=texttospeech.SsmlVoiceGender.FEMALE,
    )

    audio_config = texttospeech.AudioConfig(
        audio_encoding=texttospeech.AudioEncoding.MP3,
        sample_rate_hertz=24000
    )

    response = client.synthesize_speech(
        input=synthesis_input, voice=voice, audio_config=audio_config
    )

    return base64.b64encode(response.audio_content).decode("utf-8")
# -------------------------------
# 🔹 키워드 누락 확인 유틸
# -------------------------------
def check_required_keywords(text: str, required_keywords: list[str]) -> list[str]:
    return [kw for kw in required_keywords if kw not in text]

# -------------------------------
# 🔹 질문 목록
# -------------------------------
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
        # "required_keywords": ["정맥", "수액", "약물 주입"]
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


# -------------------------------
# 🔸 부모 음성 → STT → 키워드 누락 확인 → TTS 응답
# -------------------------------
@app.post("/parent_chat")
async def parent_chat(
    session_id: str = Form(...),
    question_id: str = Form(...),
    audio: UploadFile = File(...)
):
    transcript = clova_speech_to_text(audio)
    current_q = next(q for q in QUESTIONS if q["id"] == question_id)
    missing_keywords = check_required_keywords(transcript, current_q["required_keywords"])

    # 🔸 로그로 질문, 응답 확인
    print(f"[{session_id}] 질문 {question_id} → '{current_q['text']}'")
    print(f"[{session_id}] 응답: {transcript}")
    if missing_keywords:
        print(f"[{session_id}] 누락 키워드: {missing_keywords}")
    else:
        print(f"[{session_id}] 모든 키워드 포함")

    # 🔸 세션 응답 저장
    if session_id not in user_sessions:
        user_sessions[session_id] = {}
    user_sessions[session_id][question_id] = transcript

    # ✅ 무조건 nextBtn 활성화
    response = {
        "transcript": transcript,
        "followup_needed": False,
        "missing_keywords": []
    }

    # 🔸 누락 키워드 있을 때 맞장구식 후속 질문 제공 (단, next는 막지 않음)
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
    transcript = clova_speech_to_text(audio)

    # 🔸 이전 응답이 있는 경우 이어붙이기
    if session_id not in user_sessions:
        return JSONResponse(content={"error": "세션이 없습니다."}, status_code=404)
    
    if question_id not in user_sessions[session_id]:
        return JSONResponse(content={"error": f"{question_id}에 대한 기존 응답이 없습니다."}, status_code=404)

    # 🔸 기존 응답에 후속 응답을 이어붙이기
    prev = user_sessions[session_id][question_id]
    updated = prev.strip() + " " + transcript.strip()
    user_sessions[session_id][question_id] = updated

    print(f"[{session_id}] 후속 응답 누적 → '{updated}'")

    return JSONResponse(content={
        "transcript": transcript,
        "updated_full_response": updated,
        "followup_registered": True
    })


# -------------------------------
# 🔸 전체 요약 및 AI 피드백 요청
# -------------------------------
@app.post("/parent_chat/summary")
async def parent_chat_summary(session_id: str = Form(...)):
    from nursing_llm import get_ai_response  # 외부 평가 시스템만 분리

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
    """
    질문 또는 일반 텍스트를 음성으로 변환하여 base64로 반환합니다.
    """
    audio_base64 = synthesize_text(text)
    return JSONResponse(content={"audio_base64": audio_base64})