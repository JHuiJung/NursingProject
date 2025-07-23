# stt_google_api.py
from fastapi import FastAPI, File, UploadFile
from pydantic import BaseModel
from google.cloud import speech
from fastapi.responses import JSONResponse
import os
from dotenv import load_dotenv

load_dotenv()
app = FastAPI()

# 환경 변수 설정
os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = os.getenv("GOOGLE_APPLICATION_CREDENTIALS")

@app.post("/stt")
async def speech_to_text(audio: UploadFile = File(...)):
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

    return JSONResponse(content={"transcript": transcript})