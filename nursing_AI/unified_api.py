from fastapi import FastAPI, File, UploadFile, Form
from fastapi.responses import JSONResponse, FileResponse
from pydantic import BaseModel
from google.cloud import speech, texttospeech
from dotenv import load_dotenv
from nursing_llm import get_ai_response
import os
import uuid

load_dotenv()
app = FastAPI(title="Nursing AI API", description="간호학 시뮬레이션 AI 통합 API")

# Google Cloud 인증키 설정
os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = os.getenv("GOOGLE_APPLICATION_CREDENTIALS")

# 요청 모델 정의
class ChatRequest(BaseModel):
    session_id: str
    question: str

# 응답 모델 정의
class ChatResponse(BaseModel):
    answer: str
    correct_count: int
    incorrect_count: int
    total_questions: int
    score_percentage: float

@app.get("/")
async def root():
    """API 루트 엔드포인트"""
    return {"message": "Nursing AI API 서버가 실행 중입니다", "endpoints": ["/chat", "/stt", "/voice_chat"]}

@app.post("/chat", response_model=ChatResponse)
def chat_endpoint(request: ChatRequest):
    """텍스트 기반 채팅 API"""
    ai_response = get_ai_response(request.question)
    
    return {
        "answer": ai_response["answer"],
        "correct_count": ai_response["correct_count"],
        "incorrect_count": ai_response["incorrect_count"],
        "total_questions": ai_response["total_questions"],
        "score_percentage": ai_response["score_percentage"]
    }

@app.post("/stt")
async def stt_single_question(
    question: str = Form(...),
    audio: UploadFile = File(...)
):
    """음성 인식 및 피드백 API"""
    # 1. STT: 음성 → 텍스트
    client = speech.SpeechClient()
    audio_content = await audio.read()

    audio_config = speech.RecognitionConfig(
        encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16,
        sample_rate_hertz=16000,
        language_code="ko-KR"
    )
    audio_data = speech.RecognitionAudio(content=audio_content)

    response = client.recognize(config=audio_config, audio=audio_data)

    transcript = ""
    for result in response.results:
        transcript += result.alternatives[0].transcript

    # 2. LangChain 프롬프트 구성
    full_input = (
        "다음은 간호 시뮬레이션 질문에 대한 사용자의 음성 응답입니다.\n\n"
        f"질문: {question}\n\n"
        f"사용자 음성 응답 텍스트: {transcript}\n\n"
        "이 응답이 질문에 적절한지 평가하고, 피드백을 제공하세요. 정답일 경우 ✅로 시작하고, 오답일 경우 ❌로 시작해주세요."
    )

    # 3. LangChain AI 응답
    ai_response = get_ai_response(full_input)

    # 4. 응답 반환
    return JSONResponse(content={
        "transcript": transcript,
        "feedback": ai_response["answer"],
        "is_correct": "✅" in ai_response["answer"],
        "question": question
    })

@app.post("/voice_chat")
async def voice_chat(
    audio: UploadFile = File(...),
    session_id: str = Form(...)
):
    """음성 채팅 API (STT → AI → TTS)"""
    # 1️⃣ STT: 음성 → 텍스트
    audio_bytes = await audio.read()

    stt_client = speech.SpeechClient()
    stt_config = speech.RecognitionConfig(
        encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16,
        sample_rate_hertz=16000,
        language_code="ko-KR"
    )
    stt_audio = speech.RecognitionAudio(content=audio_bytes)
    stt_response = stt_client.recognize(config=stt_config, audio=stt_audio)

    transcript = ""
    for result in stt_response.results:
        transcript += result.alternatives[0].transcript

    print("📝 인식된 질문:", transcript)

    if not transcript.strip():
        return {"error": "음성 인식 결과가 비어 있습니다. 다시 말씀해 주세요."}

    # 2️⃣ AI 응답 생성
    ai_response = get_ai_response(transcript)
    ai_answer = ai_response["answer"]
    correct_count = ai_response["correct_count"]
    incorrect_count = ai_response["incorrect_count"]
    total_questions = ai_response["total_questions"]
    
    print("🤖 AI 응답:", ai_answer)
    print(f"📊 정답: {correct_count}개, 오답: {incorrect_count}개, 총 문제: {total_questions}개")

    # 3️⃣ TTS: 텍스트 → 음성(mp3)
    tts_client = texttospeech.TextToSpeechClient()
    synthesis_input = texttospeech.SynthesisInput(text=ai_answer)

    voice = texttospeech.VoiceSelectionParams(
        language_code="ko-KR",
        name="ko-KR-Wavenet-A"
    )

    tts_config = texttospeech.AudioConfig(
        audio_encoding=texttospeech.AudioEncoding.MP3,
        speaking_rate=1.0
    )

    tts_response = tts_client.synthesize_speech(
        input=synthesis_input, voice=voice, audio_config=tts_config
    )

    output_path = f"output_{uuid.uuid4()}.mp3"
    with open(output_path, "wb") as f:
        f.write(tts_response.audio_content)

    # 4️⃣ JSON 응답으로 음성 파일 경로와 점수 정보 반환
    return JSONResponse({
        "audio_file": f"/download_audio/{output_path}",
        "answer": ai_answer,
        "correct_count": correct_count,
        "incorrect_count": incorrect_count,
        "total_questions": total_questions,
        "score_percentage": round((correct_count / total_questions * 100) if total_questions > 0 else 0, 1)
    })

@app.get("/download_audio/{filename}")
async def download_audio(filename: str):
    """음성 파일 다운로드 엔드포인트"""
    return FileResponse(filename, media_type="audio/mpeg", filename="voice_reply.mp3")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000) 