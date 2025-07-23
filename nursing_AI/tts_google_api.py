from fastapi import FastAPI
from pydantic import BaseModel
from fastapi.responses import FileResponse
from google.cloud import texttospeech
import os
import uuid
from dotenv import load_dotenv

load_dotenv()

app = FastAPI()

class TTSRequest(BaseModel):
    text: str
    voice: str = "ko-KR-Wavenet-A"  # 남자: B, 여자: A
    speaking_rate: float = 1.0

@app.post("/tts")
def generate_google_tts(request: TTSRequest):
    os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = os.getenv("GOOGLE_APPLICATION_CREDENTIALS")

    client = texttospeech.TextToSpeechClient()

    synthesis_input = texttospeech.SynthesisInput(text=request.text)

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

    output_path = f"output_{uuid.uuid4()}.mp3"
    with open(output_path, "wb") as out:
        out.write(response.audio_content)

    return FileResponse(output_path, media_type="audio/mpeg", filename="tts.mp3")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("tts_google_api:app", host="0.0.0.0", port=8000, reload=True)