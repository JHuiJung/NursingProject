from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import FileResponse
from google.cloud import speech, texttospeech
from dotenv import load_dotenv
import os
import uuid
from .nursing_llm import get_ai_response  # 현재는 단일 인자 버전만 사용

load_dotenv()
app = FastAPI()

# Google Cloud 인증키 설정
os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = os.getenv("GOOGLE_APPLICATION_CREDENTIALS")

@app.post("/voice_chat")
async def voice_chat(
    audio: UploadFile = File(...),
    session_id: str = Form(...)
):
    # 1️⃣ STT (음성 → 텍스트)
    audio_bytes = await audio.read()

    stt_client = speech.SpeechClient()
    audio_config = speech.RecognitionConfig(
        encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16,
        sample_rate_hertz=16000,
        language_code="ko-KR"
    )
    stt_audio = speech.RecognitionAudio(content=audio_bytes)
    stt_response = stt_client.recognize(config=audio_config, audio=stt_audio)

    transcript = ""
    for result in stt_response.results:
        transcript += result.alternatives[0].transcript

    print("📝 인식된 질문:", transcript)

    # ✅ 비어있을 경우 예외 처리
    if not transcript.strip():
        return {"error": "음성 인식 결과가 비어 있습니다. 다시 말해 주세요."}

    # 2️⃣ AI 응답 생성 (LLM, RAG 등)
    ai_answer = get_ai_response(transcript)
    print("🤖 AI 응답:", ai_answer)

    # 3️⃣ TTS (응답 텍스트 → mp3)
    tts_client = texttospeech.TextToSpeechClient()
    synthesis_input = texttospeech.SynthesisInput(text=ai_answer)

    voice = texttospeech.VoiceSelectionParams(
        language_code="ko-KR",
        name="ko-KR-Wavenet-A"
    )

    audio_config = texttospeech.AudioConfig(
        audio_encoding=texttospeech.AudioEncoding.MP3,
        speaking_rate=1.0
    )

    tts_response = tts_client.synthesize_speech(
        input=synthesis_input, voice=voice, audio_config=audio_config
    )

    # 4️⃣ 파일로 저장 후 응답
    output_path = f"output_{uuid.uuid4()}.mp3"
    with open(output_path, "wb") as f:
        f.write(tts_response.audio_content)

    return FileResponse(output_path, media_type="audio/mpeg", filename="voice_reply.mp3")