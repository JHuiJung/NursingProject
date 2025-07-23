from fastapi import FastAPI
from pydantic import BaseModel
from fastapi.responses import FileResponse
from dotenv import load_dotenv
import uuid
import os

from nursing_llm import get_ai_response  # 기존 AI 응답 함수
from google.cloud import texttospeech

load_dotenv()

app = FastAPI()

class ChatTTSRequest(BaseModel):
    session_id: str
    question: str
    voice: str = "ko-KR-Wavenet-A"
    speaking_rate: float = 1.0

# TTS가 사용되는 경우와 아닌 경우 AI 프롬프트를 분기처리해서 답하도록 해야할듯 
@app.post("/chat_tts")
def chat_with_tts(request: ChatTTSRequest):
    # 1. AI 응답 생성
    ai_answer = get_ai_response(request.question)

    # 2. Google Cloud TTS 설정
    os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = os.getenv("GOOGLE_APPLICATION_CREDENTIALS")
    client = texttospeech.TextToSpeechClient()

    synthesis_input = texttospeech.SynthesisInput(text=ai_answer)
    voice = texttospeech.VoiceSelectionParams(
        language_code="ko-KR",
        name=request.voice
    )
    audio_config = texttospeech.AudioConfig(
        audio_encoding=texttospeech.AudioEncoding.MP3,
        speaking_rate=request.speaking_rate
    )

    response = client.synthesize_speech(
        input=synthesis_input, voice=voice, audio_config=audio_config
    )

    # 3. mp3 파일로 저장
    output_path = f"output_{uuid.uuid4()}.mp3"
    with open(output_path, "wb") as out:
        out.write(response.audio_content)

    # 4. mp3 반환
    return FileResponse(output_path, media_type="audio/mpeg", filename="tts.mp3")

# ✅ 테스트 실행용 코드 추가
if __name__ == "__main__":
    import uvicorn
    uvicorn.run("chat_tts_api:app", host="0.0.0.0", port=8000, reload=True)